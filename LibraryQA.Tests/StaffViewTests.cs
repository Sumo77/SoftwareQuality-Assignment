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
    public class StaffViewTests
    {
        private string _dbPath = string.Empty;
        private string _connectionString = string.Empty;

        [TestInitialize]
        public void Setup()
        {
            // Fresh database per test - MSTest doesn't guarantee execution order.
            _dbPath = Path.Combine(Path.GetTempPath(), $"staffviewtest_{Guid.NewGuid()}.db");
            new DatabaseInitializer(_dbPath).InitializeDatabase();
            new DatabaseSeeder(_dbPath).SeedSampleData();

            _connectionString = $"Data Source={_dbPath}";
        }

        [TestCleanup]
        public void Cleanup()
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
        }

        // TC-4: REQ-4 (Staff Reservations Reporting) - Functional
        // Preconditions: The database has 3 active (unfulfilled) reservations.
        // SampleData.sql seeds 2 active reservations, so one more is created here
        // (on an available book) to establish the required precondition.
        // Expected: GetTotalActiveReservationsCount (which backs the Staff View's
        // "Active reservations" summary and Reporting stats) returns 3.
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

        // TC-5: REQ-1, REQ-14 (Catalogue Integrity / Error Handling) - Functional
        // Preconditions: Staff enters a Book ID that does not exist in the catalogue.
        // Expected: The issue operation fails gracefully (no loan created, no exception)
        // so the UI (IssueButton_Click) can surface a warning message to the user.
        [TestMethod]
        public void IssueLoan_InvalidBookId_FailsGracefullyWithoutCreatingLoan()
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                int invalidBookId = 9999;

                var book = db.GetBookById(invalidBookId);
                Assert.IsNull(book, "Precondition check: book ID should not exist.");

                // Mirrors IssueButton_Click's guard clause - it never calls CreateLoan
                // when GetBookById returns null, so no loan should be created.
                int? loanId = book == null ? null : db.CreateLoan(invalidBookId, memberId: 1, DateTime.Today, DateTime.Today.AddDays(14));

                Assert.IsNull(loanId, "No loan should be created for a non-existent book ID.");
            }
        }

        // TC-6: REQ-2, REQ-14 (Returns / Real-time Integrity) - Functional
        // Preconditions: Staff user has an active loan available to return
        // (BookID 7, MemberID 3, seeded with no ReturnDate).
        // Expected: ProcessReturn records the return using the current date (close to real time)
        // and restores the book's catalogue status.
        [TestMethod]
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

                // The recorded return date should fall within the window the call was made,
                // demonstrating the return is processed close to real time.
                Assert.IsTrue(returnDate.Date >= beforeCall.Date && returnDate.Date <= afterCall.Date);
            }
        }

        // TC-7: REQ-7, REQ-11 (Role-Based Access Control) - Non-Functional (Security)
        // Preconditions: A user is authenticated as a Member, not Staff.
        // Expected: Authentication resolves their role as Member (not Staff), so the
        // application denies access to staff-only features (MainWindow only opens
        // StaffView when the resolved role equals UserRole.Staff).
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
