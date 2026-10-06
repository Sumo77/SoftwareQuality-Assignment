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

        // TC-4: GetTotalActiveReservationsCount reflects a newly added (third) reservation (REQ-6).
        // Note: this is a data-layer test - it does not exercise StaffView's UI display.
        [TestMethod]
        public void GetTotalActiveReservationsCount_AfterNewReservation_ReturnsUpdatedCount()
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
        public void IssueLoan_InvalidBookId_RejectsGracefullyWithoutCreatingLoan()
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                const int invalidBookId = 9999;

                Assert.IsNull(db.GetBookById(invalidBookId), "Precondition check: book ID should not exist.");

                int loansBefore = db.GetAllActiveLoans().Count;
                int? loanId = db.CreateLoan(invalidBookId, memberId: 1, DateTime.Today, DateTime.Today.AddDays(14));

                Assert.IsNull(loanId, "No loan should be created for a non-existent book ID.");
                Assert.AreEqual(loansBefore, db.GetAllActiveLoans().Count, "No loan record should have been added.");
            }
        }

        // TC-6: Closes an active loan (LoanID 3, BookID 7, MemberID 1) by recording a return date
        // and updates the book's catalogue status accordingly (REQ-2, REQ-14)
        [TestMethod]
        [TestCategory("Smoke")]
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

                // Hardcoded rather than recomputed with HasActiveReservation: Check Loan 3's book has an
                // unfulfilled reservation in the seed data, and reusing the call ProcessReturn relies
                // on would let a fault in that shared logic go undetected (DEF-17).
                Assert.AreEqual(7, bookId, "Precondition check: loan 3 is on book 7.");

                var book = db.GetBookById(bookId);
                Assert.AreEqual("Reserved", book!["Status"]?.ToString(),
                    "A returned book with an unfulfilled reservation must become Reserved, not Available.");
            }
        }

        // TC-7: A member account resolves only to the Member role (REQ-7, REQ-11).
        // Routing is covered separately by TC-7b.
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

        // TC-7c: The routing decision sends Staff accounts to StaffView (REQ-6, REQ-7)
        [TestMethod]
        public void ResolveViewType_StaffRole_ResolvesToStaffView()
        {
            var viewType = MainWindow.ResolveViewType(UserRole.Staff);

            Assert.AreEqual(typeof(StaffView), viewType);
        }

        // TC-13: Fulfilling a reservation closes it and issues the loan to the collecting member (REQ-3, REQ-6)
        [TestMethod]
        public void FulfillReservation_HeldBook_ClosesReservationAndIssuesLoan()
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                // Seed: LoanID 3 is BookID 7 on loan to member 1, and ReservationID 1 holds BookID 7 for member 3.
                Assert.IsTrue(db.ProcessReturn(3, DateTime.Today, "Good"));
                Assert.AreEqual("Reserved", db.GetBookById(7)!["Status"]?.ToString(),
                    "A returned book with a pending hold must be held as Reserved.");

                int? loanId = db.FulfillReservation(1, DateTime.Today, 14);

                Assert.IsNotNull(loanId, "Collecting a held book must issue a loan.");
                Assert.AreEqual("On Loan", db.GetBookById(7)!["Status"]?.ToString());
                Assert.AreEqual(0, db.GetActiveReservations(3).Count,
                    "The reservation must be closed once the book is collected.");

                var loan = db.GetLoanById(loanId.Value);
                Assert.AreEqual(3, Convert.ToInt32(loan!["MemberID"]),
                    "The loan must be issued to the reserving member, not the previous borrower.");
            }
        }

        // TC-16: The condition chosen at return is recorded on the loan (REQ-2b, REQ-14)
        [TestMethod]
        public void ProcessReturn_WithCondition_RecordsItOnTheLoan()
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                // Seed: LoanID 6 is BookID 8 on loan to member 2.
                Assert.IsTrue(db.ProcessReturn(6, DateTime.Today, "Damaged"));

                string? recorded = null;
                foreach (var loan in db.GetLoanHistory(2))
                {
                    if (Convert.ToInt32(loan["LoanID"]) == 6)
                        recorded = loan["ReturnCondition"]?.ToString();
                }

                Assert.AreEqual("Damaged", recorded,
                    "The condition selected at return must be stored against the loan, not defaulted.");
            }
        }
    }
}
