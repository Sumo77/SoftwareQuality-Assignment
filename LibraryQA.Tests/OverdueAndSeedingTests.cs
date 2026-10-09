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
    // Tests covering the overdue rule (DEF-09), the sample data corrections (DEF-05)
    // and the seeding retry (DEF-15).
    public class OverdueAndSeedingTests
    {
        private string _dbPath = string.Empty;
        private string _connectionString = string.Empty;

        [TestInitialize]
        public void Setup() // Setup the tests
        {
            // Fresh database per test due to randomised run/completion order
            _dbPath = Path.Combine(Path.GetTempPath(), $"overduetest_{Guid.NewGuid()}.db");

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
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }

        // TC-30: BOUNDARY - a loan due today is not overdue (REQ-4)
        [TestMethod]
        public void IsOverdue_LoanDueToday_IsNotOverdue()
        {
            DateTime today = new DateTime(2026, 3, 10);

            Assert.IsFalse(OverdueRules.IsOverdue(today, today));
            Assert.AreEqual(0, OverdueRules.DaysOverdue(today, today));
        }

        // TC-31: BOUNDARY - a loan due yesterday is overdue by exactly one day (REQ-4)
        [TestMethod]
        public void IsOverdue_LoanDueYesterday_IsOverdueByOneDay()
        {
            DateTime today = new DateTime(2026, 3, 10);
            DateTime dueYesterday = today.AddDays(-1);

            Assert.IsTrue(OverdueRules.IsOverdue(dueYesterday, today));
            Assert.AreEqual(1, OverdueRules.DaysOverdue(dueYesterday, today));
        }

        // TC-32: A loan not yet due never reports a negative day count (REQ-4, REQ-12)
        [TestMethod]
        public void DaysOverdue_LoanNotYetDue_ReturnsZeroNotNegative()
        {
            DateTime today = new DateTime(2026, 3, 10);
            DateTime dueNextWeek = today.AddDays(7);

            Assert.AreEqual(0, OverdueRules.DaysOverdue(dueNextWeek, today));
        }

        // TC-33: DEF-09 - the member view and the staff view report the same days overdue
        // for the same loan. Previously one rounded the value and the other truncated it.
        [TestMethod]
        public void OverdueDayCount_MemberAndStaffViews_AgreeForTheSameLoan()
        {
            DateTime today = DateTime.Today;

            using (var db = new DatabaseHelper(_connectionString))
            {
                var staffRows = db.GetAllOverdueLoans(today);
                Assert.IsNotEmpty(staffRows, "Precondition check: seed data provides at least one overdue loan.");

                foreach (var staffRow in staffRows)
                {
                    int memberId = Convert.ToInt32(staffRow["MemberID"]);
                    int loanId = Convert.ToInt32(staffRow["LoanID"]);

                    var AccountRow = db.GetActiveLoans(memberId, today)
                        .Single(l => Convert.ToInt32(l["LoanID"]) == loanId);

                    Assert.AreEqual(
                        Convert.ToInt32(staffRow["DaysOverdue"]),
                        Convert.ToInt32(AccountRow["DaysOverdue"]),
                        $"Loan {loanId} shows a different days-overdue count on the two screens.");
                }
            }
        }

        // TC-34: DEF-09 - overdue is judged against the local date supplied by the caller,
        // not SQLite's UTC date('now'). The same loan flips state either side of its due date.
        [TestMethod]
        public void GetAllOverdueLoans_UsesSuppliedDate_NotDatabaseUtcDate()
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                // Carol's loan (seeded due today) must not appear as overdue today...
                var overdueToday = db.GetAllOverdueLoans(DateTime.Today);
                Assert.IsFalse(
                    overdueToday.Any(r => Convert.ToInt32(r["MemberID"]) == 3),
                    "A loan due today must not be flagged overdue.");

                // ...but must appear once the date moves on by one day.
                var overdueTomorrow = db.GetAllOverdueLoans(DateTime.Today.AddDays(1));
                Assert.IsTrue(
                    overdueTomorrow.Any(r => Convert.ToInt32(r["MemberID"]) == 3),
                    "A loan one day past its due date must be flagged overdue.");
            }
        }

        // TC-35: DEF-05 - every book status matches its loan and reservation records (REQ-14)
        [TestMethod]
        public void SampleData_BookStatuses_MatchLoanAndReservationRecords()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                Assert.AreEqual(0, ScalarCount(connection,
                    @"SELECT COUNT(*) FROM Books
                      WHERE Status = 'On Loan'
                        AND BookID NOT IN (SELECT BookID FROM Loans WHERE ReturnDate IS NULL)"),
                    "A book is marked 'On Loan' with no active loan record.");

                Assert.AreEqual(0, ScalarCount(connection,
                    @"SELECT COUNT(*) FROM Books
                      WHERE Status = 'Reserved'
                        AND BookID NOT IN (SELECT BookID FROM Reservations WHERE FulfilledDate IS NULL)"),
                    "A book is marked 'Reserved' with no active reservation record.");

                Assert.AreEqual(0, ScalarCount(connection,
                    @"SELECT COUNT(*) FROM Loans L
                      JOIN Books B ON B.BookID = L.BookID
                      WHERE L.ReturnDate IS NULL AND B.Status <> 'On Loan'"),
                    "A book has an active loan but is not marked 'On Loan'.");
            }
        }

        // TC-36: DEF-15 - a database whose seed failed is seeded again rather than left empty
        [TestMethod]
        public void HasSampleData_EmptyDatabase_ReportsNotSeededSoTheAppRetries()
        {
            string emptyDbPath = Path.Combine(Path.GetTempPath(), $"unseeded_{Guid.NewGuid()}.db");

            try
            {
                // Schema only, exactly the state left behind when seeding fails part-way through.
                Assert.IsTrue(new DatabaseInitializer(emptyDbPath).InitializeDatabase());

                var seeder = new DatabaseSeeder(emptyDbPath);
                Assert.IsFalse(seeder.HasSampleData(), "A schema-only database must not count as seeded.");

                Assert.IsTrue(seeder.SeedSampleData(), "The retry seed should succeed.");
                Assert.IsTrue(seeder.HasSampleData(), "After a successful seed the database counts as seeded.");
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                if (File.Exists(emptyDbPath))
                {
                    File.Delete(emptyDbPath);
                }
            }
        }

        private static int ScalarCount(SqliteConnection connection, string sql)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }
    }
}
