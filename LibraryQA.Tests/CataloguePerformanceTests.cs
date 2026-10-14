using System;
using System.Diagnostics;
using System.IO;
using LibraryQA.Core.Database;
using LibraryQA.Core.Services;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibraryQA.Tests
{
    [TestClass]
    // Performance against REQ-15, measured on a generated 5,000-book catalogue: catalogue search,
    // the staff dashboard refresh, and staff catalogue management.
    // The timings printed here are the evidence for Task 7.
    public class CataloguePerformanceTests
    {
        private const int GeneratedBookCount = 5000;

        private const int SearchBudgetMilliseconds = 2000;
        private const int DashboardBudgetMilliseconds = 2000;
        private const int CatalogueWriteBudgetMilliseconds = 2000;

        private string _dbPath = string.Empty;
        private string _connectionString = string.Empty;

        [TestInitialize]
        public void Setup()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"perftest_{Guid.NewGuid()}.db");

            Assert.IsTrue(new DatabaseInitializer(_dbPath).InitializeDatabase(),
                "Database schema could not be created - check DatabaseSchema.sql is in the test output folder.");
            Assert.IsTrue(new DatabaseSeeder(_dbPath).SeedSampleData(),
                "Sample data could not be loaded - check SampleData.sql is in the test output folder.");

            _connectionString = $"Data Source={_dbPath}";
        }

        [TestCleanup]
        public void Cleanup()
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }

        // TC-37: catalogue search stays responsive on a 5,000-book catalogue (REQ-15)
        [TestMethod]
        public void SearchCatalogue_FiveThousandBooks_CompletesWithinBudget()
        {
            var generator = new PerformanceDataGenerator(_connectionString);

            var generationTimer = Stopwatch.StartNew();
            int inserted = generator.GenerateBooks(GeneratedBookCount);
            generationTimer.Stop();

            Assert.AreEqual(GeneratedBookCount, inserted, "Every generated book should have been inserted.");
            Console.WriteLine($"Generated {inserted} books in {generationTimer.ElapsedMilliseconds} ms.");

            using (var db = new DatabaseHelper(_connectionString))
            {
                // Warm up first, so the measured run is not paying for opening the connection.
                db.SearchCatalogue("zzzz-no-match");

                // A broad term, matching a large share of the catalogue - the slowest realistic case.
                var broadTimer = Stopwatch.StartNew();
                var broadResults = db.SearchCatalogue("the");
                broadTimer.Stop();

                // A narrow term, matching a handful of rows - the common case.
                var narrowTimer = Stopwatch.StartNew();
                var narrowResults = db.SearchCatalogue("Mockingbird");
                narrowTimer.Stop();

                Console.WriteLine($"Broad search  \"the\"        : {broadResults.Count} results in {broadTimer.ElapsedMilliseconds} ms.");
                Console.WriteLine($"Narrow search \"Mockingbird\": {narrowResults.Count} results in {narrowTimer.ElapsedMilliseconds} ms.");

                Assert.IsNotEmpty(broadResults, "The broad search should match generated titles.");
                Assert.HasCount(1, narrowResults, "The narrow search should match only the seeded title.");

                Assert.IsLessThan(SearchBudgetMilliseconds,
broadTimer.ElapsedMilliseconds, $"Broad catalogue search took {broadTimer.ElapsedMilliseconds} ms, over the {SearchBudgetMilliseconds} ms budget.");
                Assert.IsLessThan(SearchBudgetMilliseconds,
narrowTimer.ElapsedMilliseconds, $"Narrow catalogue search took {narrowTimer.ElapsedMilliseconds} ms, over the {SearchBudgetMilliseconds} ms budget.");
            }
        }

        // TC-38: generated books can be removed again, leaving the seeded catalogue intact
        [TestMethod]
        public void RemoveGeneratedBooks_LeavesTheSeededCatalogueUntouched()
        {
            var generator = new PerformanceDataGenerator(_connectionString);

            using (var db = new DatabaseHelper(_connectionString))
            {
                int seededCount = db.SearchCatalogue(string.Empty).Count;

                generator.GenerateBooks(100);
                Assert.HasCount(seededCount + 100, db.SearchCatalogue(string.Empty));

                int removed = generator.RemoveGeneratedBooks();

                Assert.AreEqual(100, removed);
                Assert.HasCount(seededCount, db.SearchCatalogue(string.Empty),
                    "Removing generated books must not remove any seeded book.");
            }
        }

        // TC-98: The staff dashboard stays responsive on a 5,000-book catalogue (REQ-15, REQ-6)
        // Every staff action calls RefreshAllData(), so these six queries run together far more often
        // than a catalogue search does. The Books-wide counts and the Books-to-Loans join are what
        // scale with the catalogue.
        [TestMethod]
        public void StaffDashboard_FiveThousandBooks_CompletesWithinBudget()
        {
            var generator = new PerformanceDataGenerator(_connectionString);
            Assert.AreEqual(GeneratedBookCount, generator.GenerateBooks(GeneratedBookCount));

            using (var db = new DatabaseHelper(_connectionString))
            {
                db.GetStaffStatistics(DateTime.Today); // warm up the connection

                var timer = Stopwatch.StartNew();
                var stats = db.GetStaffStatistics(DateTime.Today);
                var overdue = db.GetAllOverdueLoans(DateTime.Today);
                var popular = db.GetMostBorrowedBooks();
                var activeLoans = db.GetAllActiveLoans();
                var reservations = db.GetAllActiveReservations();
                var members = db.GetAllMembers();
                timer.Stop();

                Console.WriteLine($"Staff dashboard refresh over {stats["TotalBooks"]} books: {timer.ElapsedMilliseconds} ms.");
                Console.WriteLine($"  on loan {stats["TotalOnLoan"]}, overdue {overdue.Count}, reservations {reservations.Count}, " +
                                  $"popular titles {popular.Count}, active loans {activeLoans.Count}, members {members.Count}.");

                // The generated books must be counted, or the dashboard is not actually under load.
                Assert.IsGreaterThan(GeneratedBookCount, stats["TotalBooks"],
                    "The generated catalogue should be included in the dashboard figures.");

                Assert.IsLessThan(DashboardBudgetMilliseconds, timer.ElapsedMilliseconds,
                    $"The staff dashboard took {timer.ElapsedMilliseconds} ms, over the {DashboardBudgetMilliseconds} ms budget.");
            }
        }

        // TC-99: Staff catalogue management stays responsive on a 5,000-book catalogue (REQ-15, REQ-1)
        // The duplicate-ISBN check is the slowest part by design: it compares
        // UPPER(REPLACE(REPLACE(ISBN, '-', ''), ' ', '')) against every row, which cannot use
        // idx_books_isbn, so it is a full scan that grows with the catalogue. Worth measuring rather
        // than assuming.
        [TestMethod]
        public void CatalogueManagement_FiveThousandBooks_CompletesWithinBudget()
        {
            var generator = new PerformanceDataGenerator(_connectionString);
            Assert.AreEqual(GeneratedBookCount, generator.GenerateBooks(GeneratedBookCount));

            var catalogue = new CatalogueService(_connectionString);

            using (var db = new DatabaseHelper(_connectionString))
            {
                db.GetBookById(1); // warm up the connection

                var lookupTimer = Stopwatch.StartNew();
                var book = db.GetBookById(1);
                lookupTimer.Stop();

                var isbnTimer = Stopwatch.StartNew();
                string? inUseBy = db.GetTitleOfBookWithIsbn("9780000000017");
                isbnTimer.Stop();

                var addTimer = Stopwatch.StartNew();
                var added = catalogue.AddBook(new BookInput
                {
                    Title = "A Title Added Under Load",
                    Author = "Performance Test",
                    Isbn = "978-0-00-000001-7",
                    PublicationYear = "2026"
                });
                addTimer.Stop();

                Console.WriteLine($"Single book lookup      : {lookupTimer.ElapsedMilliseconds} ms.");
                Console.WriteLine($"Duplicate-ISBN scan     : {isbnTimer.ElapsedMilliseconds} ms (full table scan by design).");
                Console.WriteLine($"Add book (validate+save): {addTimer.ElapsedMilliseconds} ms.");

                Assert.IsNotNull(book, "Precondition check: BookID 1 should still be in the catalogue.");
                Assert.IsNull(inUseBy, "Precondition check: the test ISBN should not already be in use.");
                Assert.IsTrue(added.Success, $"Adding a book should succeed. Refused with: {added.Message}");

                Assert.IsLessThan(CatalogueWriteBudgetMilliseconds, lookupTimer.ElapsedMilliseconds,
                    $"Looking up one book took {lookupTimer.ElapsedMilliseconds} ms, over the {CatalogueWriteBudgetMilliseconds} ms budget.");
                Assert.IsLessThan(CatalogueWriteBudgetMilliseconds, isbnTimer.ElapsedMilliseconds,
                    $"The duplicate-ISBN check took {isbnTimer.ElapsedMilliseconds} ms, over the {CatalogueWriteBudgetMilliseconds} ms budget.");
                Assert.IsLessThan(CatalogueWriteBudgetMilliseconds, addTimer.ElapsedMilliseconds,
                    $"Adding a book took {addTimer.ElapsedMilliseconds} ms, over the {CatalogueWriteBudgetMilliseconds} ms budget.");
            }
        }
    }
}
