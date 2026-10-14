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
    // DEF-21: one book, one live loan. A "Pending" request leaves the book Available, so that status
    // was not enough of a guard and two members could both be approved (REQ-2a, REQ-14).
    public class LoanApprovalIntegrityTests
    {
        private string _dbPath = string.Empty;
        private string _connectionString = string.Empty;
        private MemberActionsService _memberService = null!;
        private ApprovalService _approvalService = null!;

        [TestInitialize]
        public void Setup() // Build a fresh database for this test
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"loanintegritytest_{Guid.NewGuid()}.db");

            Assert.IsTrue(new DatabaseInitializer(_dbPath).InitializeDatabase(),
                "Database schema could not be created - check DatabaseSchema.sql is in the test output folder.");
            Assert.IsTrue(new DatabaseSeeder(_dbPath).SeedSampleData(),
                "Sample data could not be loaded - check SampleData.sql is in the test output folder.");

            _connectionString = $"Data Source={_dbPath}";
            _memberService = new MemberActionsService(_connectionString);
            _approvalService = new ApprovalService(_connectionString);
        }

        [TestCleanup]
        public void Cleanup() // Delete this test's database
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
        }

        // TC-91: A second loan request for a book that already has a pending request is refused,
        // even though the book is still "Available" (REQ-2a, REQ-14, DEF-21)
        [TestMethod]
        public void BorrowBook_BookAlreadyHasAPendingRequest_RefusesTheSecondMember()
        {
            // Alice (1) and Emma (5) each hold one item, so both are under the limit of 2.
            // BookID 2 is seeded as Available and has no loan or reservation records.
            var first = _memberService.BorrowBook(bookId: 2, memberId: 1);
            Assert.IsTrue(first.Success, first.Message);

            using var db = new DatabaseHelper(_connectionString);

            // The book is deliberately still Available - REQ-18 does not move it until approval -
            // so availability alone cannot be what protects it.
            Assert.AreEqual("Available", db.GetBookById(2)!["Status"].ToString(),
                "Precondition check: a pending request must not change the book's status (REQ-18).");

            var second = _memberService.BorrowBook(bookId: 2, memberId: 5);

            Assert.IsFalse(second.Success,
                "A book with a pending request is already claimed and must not be requested by a second member.");
            Assert.AreEqual(1, db.GetPendingLoans().Count(l => Convert.ToInt32(l["BookID"]) == 2),
                "Exactly one pending request may exist for one book.");
        }

        // TC-92: Two pending requests for one book cannot both be approved - the second approval is
        // refused and the book keeps exactly one active loan (REQ-2a, REQ-14, DEF-21)
        [TestMethod]
        public void ApproveLoan_SecondRequestForTheSameBook_IsRefusedAndLeavesOneActiveLoan()
        {
            Assert.IsTrue(_memberService.BorrowBook(bookId: 2, memberId: 1).Success);

            using var db = new DatabaseHelper(_connectionString);
            int firstLoanId = Convert.ToInt32(
                db.GetPendingLoans().Single(l => Convert.ToInt32(l["BookID"]) == 2)["LoanID"]);

            // Build the dangerous state directly rather than through BorrowBook, so this test still
            // fails if the guard in CreateLoanRequest is removed but the approval path is left alone.
            int? secondLoanId = db.CreateLoanRequest(bookId: 2, memberId: 5,
                loanDate: DateTime.Today, dueDate: DateTime.Today.AddDays(14));

            Assert.IsTrue(_approvalService.ApproveLoan(firstLoanId).Success,
                "The first request must still be approvable.");
            Assert.AreEqual("On Loan", db.GetBookById(2)!["Status"].ToString());

            if (secondLoanId != null)
            {
                Assert.IsFalse(_approvalService.ApproveLoan(secondLoanId.Value).Success,
                    "Approving a request whose book is no longer Available must be refused.");
            }

            Assert.AreEqual(1, db.GetAllActiveLoans().Count(l => Convert.ToInt32(l["BookID"]) == 2),
                "One book may never hold more than one active loan.");
        }

        // TC-93: The guard does not over-reach - a book can be borrowed again once it is returned,
        // and its earlier loan is kept as history (REQ-2a, REQ-2b, REQ-14)
        [TestMethod]
        public void BorrowBook_AfterTheBookIsReturned_CanBeRequestedAgain()
        {
            Assert.IsTrue(_memberService.BorrowBook(bookId: 2, memberId: 1).Success);

            using var db = new DatabaseHelper(_connectionString);
            int loanId = Convert.ToInt32(
                db.GetPendingLoans().Single(l => Convert.ToInt32(l["BookID"]) == 2)["LoanID"]);
            Assert.IsTrue(_approvalService.ApproveLoan(loanId).Success);

            Assert.IsTrue(db.ProcessReturn(loanId, DateTime.Today, "Good"));
            Assert.AreEqual("Available", db.GetBookById(2)!["Status"].ToString());

            var secondRequest = _memberService.BorrowBook(bookId: 2, memberId: 5);
            Assert.IsTrue(secondRequest.Success,
                "A returned book must be borrowable again - the live-loan guard must not catch closed loans.");

            Assert.IsNotNull(db.GetLoanById(loanId),
                "The closed loan must be kept as history, not deleted to free the book.");
            Assert.IsFalse(db.GetAllActiveLoans().Any(l => Convert.ToInt32(l["LoanID"]) == loanId),
                "The closed loan must no longer count as live.");
            Assert.AreEqual(1, db.GetPendingLoans().Count(l => Convert.ToInt32(l["BookID"]) == 2),
                "The new request must be the only live claim on the book.");
        }
    }
}
