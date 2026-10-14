using System;
using System.IO;
using System.Linq;
using LibraryQA.Core.Database;
using LibraryQA.Core.Services;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibraryQA.Tests
{
    [TestClass]
    // REQ-6 (Staff Portal) reporting. The requirement promises staff four figures - items on loan,
    // overdue items, active reservations and the most frequently borrowed titles - and the two
    // methods behind them, GetStaffStatistics and GetMostBorrowedBooks, had no test coverage at all.
    // Expected counts are taken from the seed file by hand rather than recomputed with the same
    // production queries under test, so a fault cannot move expected and actual together (DEF-17).
    public class StaffReportingTests
    {
        private string _dbPath = string.Empty;
        private string _connectionString = string.Empty;
        private ApprovalService _approvalService = null!;

        [TestInitialize]
        public void Setup()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"staffreporttest_{Guid.NewGuid()}.db");

            Assert.IsTrue(new DatabaseInitializer(_dbPath).InitializeDatabase(),
                "Database schema could not be created - check DatabaseSchema.sql is in the test output folder.");
            Assert.IsTrue(new DatabaseSeeder(_dbPath).SeedSampleData(),
                "Sample data could not be loaded - check SampleData.sql is in the test output folder.");

            _connectionString = $"Data Source={_dbPath}";
            _approvalService = new ApprovalService(_connectionString);
        }

        [TestCleanup]
        public void Cleanup()
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
        }

        // TC-94: The staff dashboard figures match the records, count only live loans, and use the
        // local date supplied by the caller rather than the database's UTC date (REQ-6, REQ-4, DEF-09)
        [TestMethod]
        public void GetStaffStatistics_ReportsLiveCountsAgainstTheSuppliedDate()
        {
            using var db = new DatabaseHelper(_connectionString);

            // Seed: 6 active unreturned loans, 3 of them overdue (by 6, 4 and 1 days), 2 active
            // reservations, 5 members, 31 books of which 25 are Available.
            var stats = db.GetStaffStatistics(DateTime.Today);

            Assert.AreEqual(6, stats["TotalOnLoan"]);
            Assert.AreEqual(3, stats["TotalOverdue"]);
            Assert.AreEqual(2, stats["TotalReservations"]);
            Assert.AreEqual(5, stats["TotalMembers"]);
            Assert.AreEqual(31, stats["TotalBooks"]);
            Assert.AreEqual(25, stats["AvailableBooks"]);

            // DEF-09 boundary: LoanID 8 falls due today, which is not yet overdue. Rolling the
            // supplied date forward one day must tip exactly that one loan over, which proves the
            // count follows the caller's date and not date('now') inside SQL.
            Assert.AreEqual(4, db.GetStaffStatistics(DateTime.Today.AddDays(1))["TotalOverdue"],
                "A loan due today must become overdue tomorrow, and only that one.");

            // REQ-18: a request awaiting approval is not a loan yet, so it must not inflate the
            // on-loan figure staff are shown.
            Assert.IsNotNull(db.CreateLoanRequest(bookId: 2, memberId: 1,
                loanDate: DateTime.Today, dueDate: DateTime.Today.AddDays(14)));

            Assert.AreEqual(6, db.GetStaffStatistics(DateTime.Today)["TotalOnLoan"],
                "A pending request must not be counted as an item on loan.");

            // Processing a return must move both figures, and release the book back to the catalogue.
            // LoanID 6 is BookID 8, overdue by 6 days, with no reservation held against it.
            Assert.IsTrue(db.ProcessReturn(loanId: 6, DateTime.Today, "Good"));

            var afterReturn = db.GetStaffStatistics(DateTime.Today);
            Assert.AreEqual(5, afterReturn["TotalOnLoan"], "Returning an item must reduce the on-loan count.");
            Assert.AreEqual(2, afterReturn["TotalOverdue"], "Returning an overdue item must reduce the overdue count.");
            Assert.AreEqual(26, afterReturn["AvailableBooks"], "The returned book must be available again.");
        }

        // TC-95: The most-borrowed report ranks titles by how often they have been borrowed, counts
        // closed loans as well as current ones, honours the requested limit, and omits titles that
        // have never been borrowed (REQ-6)
        [TestMethod]
        public void GetMostBorrowedBooks_RanksByBorrowCountAndExcludesNeverBorrowedTitles()
        {
            using var db = new DatabaseHelper(_connectionString);

            // Every seeded book has exactly one loan, so a ranking has to be built. Each cycle is a
            // full borrow and return - the single-live-loan rule (DEF-21) forbids stacking them.
            for (int i = 0; i < 2; i++)
            {
                int? loan = db.CreateLoan(bookId: 1, memberId: 1, DateTime.Today.AddDays(-30), DateTime.Today.AddDays(-16));
                Assert.IsNotNull(loan, "Precondition check: book 1 should be free to borrow again.");
                Assert.IsTrue(db.ProcessReturn(loan.Value, DateTime.Today.AddDays(-20), "Good"));
            }

            int? extra = db.CreateLoan(bookId: 2, memberId: 2, DateTime.Today.AddDays(-30), DateTime.Today.AddDays(-16));
            Assert.IsNotNull(extra);
            Assert.IsTrue(db.ProcessReturn(extra.Value, DateTime.Today.AddDays(-20), "Good"));

            // Book 1 now has 3 loans, book 2 has 2, every other borrowed book has 1.
            var ranked = db.GetMostBorrowedBooks();

            Assert.AreEqual(1, Convert.ToInt32(ranked[0]["BookID"]), "The most borrowed title must come first.");
            Assert.AreEqual(3, Convert.ToInt32(ranked[0]["BorrowCount"]),
                "Closed loans must still count towards how often a title has been borrowed.");
            Assert.AreEqual(2, Convert.ToInt32(ranked[1]["BookID"]));
            Assert.AreEqual(2, Convert.ToInt32(ranked[1]["BorrowCount"]));

            var counts = ranked.Select(b => Convert.ToInt32(b["BorrowCount"])).ToList();
            CollectionAssert.AreEqual(counts.OrderByDescending(n => n).ToList(), counts,
                "Results must be ordered by borrow count, highest first.");

            // BookID 4 ('Pride and Prejudice') has never been borrowed, so it must not appear at all -
            // a report of popular titles padded with unborrowed ones would mislead staff.
            Assert.IsFalse(ranked.Any(b => Convert.ToInt32(b["BookID"]) == 4),
                "A title with no loan records must be absent from the most-borrowed report.");

            Assert.AreEqual(3, db.GetMostBorrowedBooks(topN: 3).Count, "The requested limit must be honoured.");
        }
    }
}
