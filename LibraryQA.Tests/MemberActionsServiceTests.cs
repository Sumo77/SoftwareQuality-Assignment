using System;
using System.IO;
using LibraryQA.Core.Database;
using LibraryQA.Core.Services;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibraryQA.Tests
{
    [TestClass]
    public class MemberActionsServiceTests
    {
        private string _dbPath = string.Empty;
        private string _connectionString = string.Empty;
        private MemberActionsService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"membertest_{Guid.NewGuid()}.db");
            new DatabaseInitializer(_dbPath).InitializeDatabase();
            new DatabaseSeeder(_dbPath).SeedSampleData();

            _connectionString = $"Data Source={_dbPath}";
            _service = new MemberActionsService(_connectionString);
        }

        [TestCleanup]
        public void Cleanup()
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
        }

        // TC-8: REQ-2a (Borrow), REQ-14 (Integrity)
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

        // TC-9: REQ-3 (Reservations), REQ-14 (Integrity)
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

        // TC-10: REQ-8 (Loan Limits)
        [TestMethod]
        public void BorrowBook_MemberAtLoanLimit_RejectsWithLimitMessage()
        {
            var result = _service.BorrowBook(bookId: 4, memberId: 2);

            Assert.IsFalse(result.Success);
            StringAssert.Contains(result.Message, "limit has been reached");
        }

        // TC-11: REQ-5 (Member Portal - data isolation)
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