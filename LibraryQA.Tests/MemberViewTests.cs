using System;
using System.IO;
using LibraryQA.Core.Database;
using LibraryQA.Core.Services;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibraryQA.Tests
{
    [TestClass]
    // Member Interface/View Tests
    public class MemberViewTests
    {
        private string _dbPath = string.Empty;
        private string _connectionString = string.Empty;
        private MemberActionsService _service = null!;

        [TestInitialize]
        public void Setup() // Setup the tests
        {
            // Fresh database per test due to randomised run/completion order
            _dbPath = Path.Combine(Path.GetTempPath(), $"membertest_{Guid.NewGuid()}.db");
            new DatabaseInitializer(_dbPath).InitializeDatabase();
            new DatabaseSeeder(_dbPath).SeedSampleData();

            _connectionString = $"Data Source={_dbPath}";
            _service = new MemberActionsService(_connectionString);
        }

        [TestCleanup]
        public void Cleanup() // Cleans/Deletes the tests
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
        }

        // TC-8: Borrowing an "available" book sets a due date to 14 days from current date and changes status to "on loan" (REQ-2a, REQ-14)
        [TestMethod]
        public void BorrowBook_AvailableBook_SetsDueDateAndUpdatesStatus()
        {
            var result = _service.BorrowBook(bookId: 1, memberId: 1);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(DateTime.Today.AddDays(14), result.DueDate);

            using (var db = new DatabaseHelper(_connectionString))
            {
                var book = db.GetBookById(1);
                Assert.AreEqual("On Loan", book!["Status"].ToString());
            }
        }

        // TC-9: Reserving a book that already has an active reservation is rejected and leaves its status unchanged (REQ-3, REQ-14)
        [TestMethod]
        public void ReserveBook_AlreadyReserved_RejectsAndKeepsStatusUnchanged()
        {
            var result = _service.ReserveBook(bookId: 7, memberId: 4);

            Assert.IsFalse(result.Success);
            StringAssert.Contains(result.Message, "already reserved");

            using (var db = new DatabaseHelper(_connectionString))
            {
                var book = db.GetBookById(7);
                Assert.AreEqual("On Loan", book!["Status"].ToString());
            }
        }

        // TC-10: A member already holding two active loans cannot borrow a third (REQ-8)
        [TestMethod]
        public void BorrowBook_MemberAtLoanLimit_RejectsWithLimitMessage()
        {
            var result = _service.BorrowBook(bookId: 4, memberId: 2);

            Assert.IsFalse(result.Success);
            StringAssert.Contains(result.Message, "limit has been reached");
        }

        // TC-11: Each member's active loan list contains only their own records and never another members (REQ-5)
        [TestMethod]
        public void GetActiveLoans_ReturnsOnlyTheRequestedMembersLoans()
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                var aliceLoans = db.GetActiveLoans(memberId: 1);
                var bobLoans = db.GetActiveLoans(memberId: 2);

                Assert.AreEqual(1, aliceLoans.Count);
                Assert.IsFalse(aliceLoans.Exists(l =>
                    Convert.ToInt32(l["BookID"]) == 8 || Convert.ToInt32(l["BookID"]) == 10));

                Assert.AreEqual(2, bobLoans.Count);
                Assert.IsFalse(bobLoans.Exists(l => Convert.ToInt32(l["BookID"]) == 7));
            }
        }
    }
}