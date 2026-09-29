using System;
using System.Diagnostics;
using System.IO;
using LibraryQA.Core.Database;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibraryQA.Tests
{
    [TestClass]
    // Catalogue search performance against REQ-15, measured on a generated 5,000-book catalogue.
    // The timings printed here are the evidence for Task 7.
    public class CataloguePerformanceTests
    {
        private const int GeneratedBookCount = 5000;

        // Deliberately generous. REQ-15 asks for "near real time"; this asserts the search has not
        // become pathologically slow, while the printed timing is what gets reported in Task 7.
        // A shared CI runner is slower than a local machine, so a tight bound would flake.
        private const int SearchBudgetMilliseconds = 2000;

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
    }
}
