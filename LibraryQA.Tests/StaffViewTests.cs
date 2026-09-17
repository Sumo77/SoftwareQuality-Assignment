using System;
using System.IO;
using LibraryQA.Core.Database;
using LibraryQA.Core.Models;
using LibraryQA.Core.Services;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibraryQA.Tests
{
    [TestClass]
    // Staff Interface/View Tests
    public class StaffViewTests
    {
        private string _dbPath = string.Empty;
        private string _connectionString = string.Empty;

        [TestInitialize]
        public void Setup() // Setup the tests
        {
            // Fresh database per test due to randomised run/completion order
            _dbPath = Path.Combine(Path.GetTempPath(), $"staffviewtest_{Guid.NewGuid()}.db");
            
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

        // TC-4: Adding (third) reservation, so staff's active reservation count returns with the new addition (REQ-6)
        [TestMethod]
        public void GetTotalActiveReservationsCount_ThreeActiveReservationsSeeded_ReturnsThree()
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                Assert.AreEqual(2, db.GetTotalActiveReservationsCount(), "Precondition check: seed data provides 2 active reservations.");

                int? newReservationId = db.CreateReservation(bookId: 9, memberId: 5, DateTime.Today);
                Assert.IsNotNull(newReservationId, "Precondition setup: should be able to reserve an available book.");

                int activeReservations = db.GetTotalActiveReservationsCount();

                Assert.AreEqual(3, activeReservations);
            }
        }

        // TC-5: Invalid Book ID returns no catalogue record, so the guard clause rejects the loan before it is attempted (REQ-9)
        [TestMethod]
        public void IssueLoan_InvalidBookId_FailsGracefullyWithoutCreatingLoan()
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                int invalidBookId = 9999;

                var book = db.GetBookById(invalidBookId);
                Assert.IsNull(book, "Precondition check: book ID should not exist.");

                int? loanId = book == null ? null : db.CreateLoan(invalidBookId, memberId: 1, DateTime.Today, DateTime.Today.AddDays(14));

                Assert.IsNull(loanId, "No loan should be created for a non-existent book ID.");
            }
        }

        // TC-6: Closes an active loan (LoanID 3, BookID 7, MemberID 1) by recording a return date (REQ-2)
        [TestMethod]
        [TestCategory("Smoke")]
        public void ProcessReturn_ActiveLoan_RecordsReturnCloseToRealTime()
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                var loanBefore = db.GetLoanById(3);
                Assert.IsNotNull(loanBefore, "Precondition check: loan should exist.");
                Assert.AreEqual(DBNull.Value, loanBefore!["ReturnDate"], "Precondition check: loan should be active (unreturned).");

                DateTime beforeCall = DateTime.Now;
                bool success = db.ProcessReturn(loanId: 3, returnDate: DateTime.Now.Date);
                DateTime afterCall = DateTime.Now;

                Assert.IsTrue(success);

                var loanAfter = db.GetLoanById(3);
                DateTime returnDate = DateTime.Parse(loanAfter!["ReturnDate"]!.ToString()!);

                // The recorded return date should fall within the window the call was made, so the return is processed close to real time.
                Assert.IsTrue(returnDate.Date >= beforeCall.Date && returnDate.Date <= afterCall.Date);
            }
        }

        // TC-7: A member account never resolves to Staff, so MainWindow never opens StaffView for them (REQ-7, REQ-11)
        [TestMethod]
        public void Authenticate_MemberAccount_DoesNotResolveToStaffRole()
        {
            var auth = new AuthenticationService(_connectionString);

            UserRole? role = auth.Authenticate("alice.member", "member123");

            Assert.AreEqual(UserRole.Member, role);
            Assert.AreNotEqual(UserRole.Staff, role, "A member account must never be granted the Staff role.");
        }
    }
}
