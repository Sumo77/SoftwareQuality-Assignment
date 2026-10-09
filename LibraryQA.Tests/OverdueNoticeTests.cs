using System;
using System.IO;
using LibraryQA.Core.Database;
using LibraryQA.Core.Services;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibraryQA.Tests
{
    [TestClass]
    // Overdue Notices (REQ-17)
    public class OverdueNoticeTests
    {
        private string _dbPath = string.Empty;
        private string _connectionString = string.Empty;

        [TestInitialize]
        public void Setup() // Setup the tests
        {
            // Fresh database per test due to randomised run/completion order
            _dbPath = Path.Combine(Path.GetTempPath(), $"noticetest_{Guid.NewGuid()}.db");

            Assert.IsTrue(new DatabaseInitializer(_dbPath).InitializeDatabase(),
                "Database schema could not be created - check DatabaseSchema.sql is in the test output folder.");
            Assert.IsTrue(new DatabaseSeeder(_dbPath).SeedSampleData(),
                "Sample data could not be loaded - check SampleData.sql is in the test output folder.");

            _connectionString = $"Data Source={_dbPath}";
        }

        [TestCleanup]
        public void Cleanup() // Cleans/Deletes the tests
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
        }

        // TC-46: A member with an overdue item is told which item it is and how late (REQ-17)
        [TestMethod]
        public void OverdueNotice_MemberWithOverdueLoan_NamesTheItemAndTheDelay()
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                // Seed: member 5 (Emma) has one loan, 'Steve Jobs', that fell due yesterday.
                string? notice = OverdueNotice.ForMember(db.GetActiveLoans(5));

                Assert.IsNotNull(notice, "A member with an overdue item must be told at login.");
                StringAssert.Contains(notice, "You have 1 overdue item");
                StringAssert.Contains(notice, "'Steve Jobs' is overdue by 1 day.",
                    "The notice must name the item and how late it is, not just that something is late.");
            }
        }

        // TC-47: A member with nothing overdue is shown no notice at all (REQ-17)
        [TestMethod]
        public void OverdueNotice_NothingOverdue_ReturnsNoNotice()
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                // Seed: member 1 (Alice) has one active loan, not due for another 11 days.
                Assert.IsNull(OverdueNotice.ForMember(db.GetActiveLoans(1)),
                    "A member with nothing overdue must not be warned.");

                // Boundary: member 3 (Carol) is due today, which is not yet overdue (DEF-09).
                Assert.IsNull(OverdueNotice.ForMember(db.GetActiveLoans(3)));
            }
        }

        // TC-48: The notice stops once the overdue item is returned (REQ-17)
        [TestMethod]
        public void OverdueNotice_AfterTheItemIsReturned_StopsShowing()
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                // Seed: member 2 (Bob) holds two books, LoanID 6 being overdue by 6 days.
                Assert.IsNotNull(OverdueNotice.ForMember(db.GetActiveLoans(2)),
                    "Precondition check: Bob should start with an overdue item.");
                Assert.IsNotNull(OverdueNotice.ForStaff(db.GetAllOverdueLoans()));

                Assert.IsTrue(db.ProcessReturn(6, DateTime.Today, "Good"));

                Assert.IsNull(OverdueNotice.ForMember(db.GetActiveLoans(2)),
                    "Returning the item must stop the notice, without anyone clearing it by hand.");
            }
        }

        // TC-54: The staff notice counts every overdue item across all members, and is absent when
        // there are none (REQ-17)
        [TestMethod]
        public void OverdueNotice_ForStaff_CountsEveryOverdueItem()
        {
            Assert.IsNull(OverdueNotice.ForStaff(new List<Dictionary<string, object>>()),
                "With nothing overdue the dashboard must show no notice at all.");

            using (var db = new DatabaseHelper(_connectionString))
            {
                // Seed: three loans are overdue - Bob's by 6 days, David's by 4, Emma's by 1.
                string? notice = OverdueNotice.ForStaff(db.GetAllOverdueLoans());

                Assert.IsNotNull(notice);
                StringAssert.Contains(notice, "3 items are overdue");
            }
        }
    }
}