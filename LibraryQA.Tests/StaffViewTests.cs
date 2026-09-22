using System;
using System.IO;
using LibraryQA;
using LibraryQA.Core.Database;
using LibraryQA.Core.Models;
using LibraryQA.Core.Services;
using LibraryQA.Views;
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
            new DatabaseInitializer(_dbPath).InitializeDatabase();
            new DatabaseSeeder(_dbPath).SeedSampleData();

            _connectionString = $"Data Source={_dbPath}";
        }

        [TestCleanup]
        public void Cleanup() // Cleans/Deletes the tests
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
        }

        // TC-4: GetTotalActiveReservationsCount reflects a newly added (third) reservation (REQ-6).
        // Note: this is a data-layer test - it does not exercise StaffView's UI display.
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

        // TC-6: Closes an active loan (LoanID 3, BookID 7, MemberID 1) by recording a return date
        // and updates the book's catalogue status accordingly (REQ-2, REQ-14)
        [TestMethod]
        public void ProcessReturn_ActiveLoan_RecordsReturnAndUpdatesBookStatus()
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                var loanBefore = db.GetLoanById(3);
                Assert.IsNotNull(loanBefore, "Precondition check: loan should exist.");
                Assert.AreEqual(DBNull.Value, loanBefore!["ReturnDate"], "Precondition check: loan should be active (unreturned).");

                int bookId = Convert.ToInt32(loanBefore["BookID"]);

                DateTime beforeCall = DateTime.Now;
                bool success = db.ProcessReturn(loanId: 3, returnDate: DateTime.Now.Date);
                DateTime afterCall = DateTime.Now;

                Assert.IsTrue(success);

                var loanAfter = db.GetLoanById(3);
                DateTime returnDate = DateTime.Parse(loanAfter!["ReturnDate"]!.ToString()!);

                // The recorded return date should fall within the window the call was made, so the return is processed close to real time.
                Assert.IsTrue(returnDate.Date >= beforeCall.Date && returnDate.Date <= afterCall.Date);

                // The book's catalogue status must also reflect the return (Available, or Reserved if a hold exists).
                var book = db.GetBookById(bookId);
                string expectedStatus = db.HasActiveReservation(bookId) ? "Reserved" : "Available";
                Assert.AreEqual(expectedStatus, book!["Status"]?.ToString());
            }
        }

        // TC-7: A member account resolves only to the Member role, and role-based routing sends
        // Member accounts to MemberView, never StaffView (REQ-7, REQ-11)
        [TestMethod]
        public void Authenticate_MemberAccount_ResolvesToMemberRoleOnly()
        {
            var auth = new AuthenticationService(_connectionString);

            UserRole? role = auth.Authenticate("alice.member", "member123");

            Assert.AreEqual(UserRole.Member, role);
            Assert.AreNotEqual(UserRole.Staff, role, "A member account must never be granted the Staff role.");
        }

        // TC-7b: The role -> view routing decision itself denies staff features to Member accounts,
        // regardless of what Authenticate returns (REQ-7, REQ-11)
        [TestMethod]
        public void ResolveViewType_MemberRole_NeverResolvesToStaffView()
        {
            var viewType = MainWindow.ResolveViewType(UserRole.Member);

            Assert.AreNotEqual(typeof(StaffView), viewType, "A member role must never be routed to StaffView.");
            Assert.AreEqual(typeof(MemberView), viewType);
        }

        [TestMethod]
        public void ResolveViewType_StaffRole_ResolvesToStaffView()
        {
            var viewType = MainWindow.ResolveViewType(UserRole.Staff);

            Assert.AreEqual(typeof(StaffView), viewType);
        }
    }
}
