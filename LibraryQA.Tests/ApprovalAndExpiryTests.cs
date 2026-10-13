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
    // REQ-18 (Loan and Reservation Approval) and REQ-20 (Reservation Expiry) tests.
    // Numbering continues from TC-54 (the last test added prior to this feature).
    public class ApprovalAndExpiryTests
    {
        private string _dbPath = string.Empty;
        private string _connectionString = string.Empty;
        private MemberActionsService _memberService = null!;
        private ApprovalService _approvalService = null!;
        private ReservationExpiryService _expiryService = null!;

        [TestInitialize]
        public void Setup()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"approvaltest_{Guid.NewGuid()}.db");

            Assert.IsTrue(new DatabaseInitializer(_dbPath).InitializeDatabase(),
                "Database schema could not be created - check DatabaseSchema.sql is in the test output folder.");
            Assert.IsTrue(new DatabaseSeeder(_dbPath).SeedSampleData(),
                "Sample data could not be loaded - check SampleData.sql is in the test output folder.");

            _connectionString = $"Data Source={_dbPath}";
            _memberService = new MemberActionsService(_connectionString);
            _approvalService = new ApprovalService(_connectionString);
            _expiryService = new ReservationExpiryService(_connectionString);
        }

        [TestCleanup]
        public void Cleanup()
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
        }

        // TC-55: A loan request is held as "Pending" and is not yet active (REQ-18 AC1)
        [TestMethod]
        public void BorrowBook_NewRequest_IsHeldPendingNotActive()
        {
            // Emma (MemberID 5) has 1 active loan seeded (BookID 14) and no reservations, so she is under the limit.
            var result = _memberService.BorrowBook(bookId: 2, memberId: 5);
            Assert.IsTrue(result.Success, result.Message);

            using var db = new DatabaseHelper(_connectionString);
            var pending = db.GetPendingLoans();
            Assert.IsTrue(pending.Exists(l => Convert.ToInt32(l["BookID"]) == 2 && Convert.ToInt32(l["MemberID"]) == 5));

            var active = db.GetActiveLoans(memberId: 5);
            Assert.IsFalse(active.Exists(l => Convert.ToInt32(l["BookID"]) == 2));
        }

        // TC-56: A reservation request is held as "Pending" and is not yet active (REQ-18 AC1)
        [TestMethod]
        public void ReserveBook_NewRequest_IsHeldPendingNotActive()
        {
            var result = _memberService.ReserveBook(bookId: 7, memberId: 5);
            Assert.IsFalse(result.Success); // Book 7 already has an active reservation seeded for member 3

            var secondResult = _memberService.ReserveBook(bookId: 8, memberId: 5);
            Assert.IsTrue(secondResult.Success);

            using var db = new DatabaseHelper(_connectionString);
            var pending = db.GetPendingReservations();
            Assert.IsTrue(pending.Exists(r => Convert.ToInt32(r["BookID"]) == 8 && Convert.ToInt32(r["MemberID"]) == 5));
        }

        // TC-57: Approving a pending loan request activates it and updates the book's status (REQ-18 AC2)
        [TestMethod]
        public void ApproveLoan_PendingLoan_BecomesActiveAndBookStatusUpdates()
        {
            _memberService.BorrowBook(bookId: 2, memberId: 5);

            using var db = new DatabaseHelper(_connectionString);
            int loanId = Convert.ToInt32(db.GetPendingLoans().Single(l => Convert.ToInt32(l["BookID"]) == 2)["LoanID"]);

            var approval = _approvalService.ApproveLoan(loanId);
            Assert.IsTrue(approval.Success);

            Assert.AreEqual("On Loan", db.GetBookById(2)!["Status"].ToString());
            Assert.IsTrue(db.GetActiveLoans(memberId: 5).Exists(l => Convert.ToInt32(l["LoanID"]) == loanId));
        }

        // TC-58: Approving a pending reservation activates it (REQ-18 AC2)
        [TestMethod]
        public void ApproveReservation_PendingReservation_BecomesActive()
        {
            _memberService.ReserveBook(bookId: 8, memberId: 5);

            using var db = new DatabaseHelper(_connectionString);
            int reservationId = Convert.ToInt32(db.GetPendingReservations().Single(r => Convert.ToInt32(r["BookID"]) == 8)["ReservationID"]);

            var approval = _approvalService.ApproveReservation(reservationId);
            Assert.IsTrue(approval.Success);

            var activeReservations = db.GetActiveReservations(memberId: 5);
            Assert.IsTrue(activeReservations.Exists(r => Convert.ToInt32(r["ReservationID"]) == reservationId && r["Status"]!.ToString() == "Active"));
        }

        // TC-59: Rejecting a pending loan request closes it without changing the book's status, and the
        // member is informed of the rejection (REQ-18 AC3)
        [TestMethod]
        public void RejectLoan_PendingLoan_ClosesRequestAndInformsMember()
        {
            _memberService.BorrowBook(bookId: 2, memberId: 5);

            using var db = new DatabaseHelper(_connectionString);
            int loanId = Convert.ToInt32(db.GetPendingLoans().Single(l => Convert.ToInt32(l["BookID"]) == 2)["LoanID"]);

            var rejection = _approvalService.RejectLoan(loanId);
            Assert.IsTrue(rejection.Success);

            Assert.AreEqual("Available", db.GetBookById(2)!["Status"].ToString());

            var notices = db.GetRejectedNotices(memberId: 5);
            Assert.IsTrue(notices.Exists(n => n["RequestType"]!.ToString() == "Loan"));
        }

        // TC-60: Rejecting a pending reservation closes it without changing the book's status (REQ-18 AC3)
        [TestMethod]
        public void RejectReservation_PendingReservation_ClosesRequestWithoutChangingBookStatus()
        {
            _memberService.ReserveBook(bookId: 8, memberId: 5);

            using var db = new DatabaseHelper(_connectionString);
            int reservationId = Convert.ToInt32(db.GetPendingReservations().Single(r => Convert.ToInt32(r["BookID"]) == 8)["ReservationID"]);
            string statusBefore = db.GetBookById(8)!["Status"].ToString()!;

            var rejection = _approvalService.RejectReservation(reservationId);
            Assert.IsTrue(rejection.Success);

            Assert.AreEqual(statusBefore, db.GetBookById(8)!["Status"].ToString());
        }

        // TC-61: A member with 2 active-or-pending items is rejected when requesting a third, with a
        // message stating the limit has been reached (REQ-18 AC4)
        [TestMethod]
        public void BorrowBook_MemberWithTwoActiveOrPendingItems_RejectsThirdRequest()
        {
            // Carol (MemberID 3) already has 1 active loan (BookID 17) and 1 active reservation (BookID 7) seeded,
            // so she is already at the combined limit of 2.
            var thirdRequest = _memberService.BorrowBook(bookId: 2, memberId: 3);

            Assert.IsFalse(thirdRequest.Success);
            StringAssert.Contains(thirdRequest.Message, "limit has been reached");
        }

        // TC-62: Once a pending request is rejected by staff, that item no longer counts towards the
        // member's limit (REQ-18 AC5)
        [TestMethod]
        public void RejectLoan_FreesUpMembersRequestLimit()
        {
            _memberService.BorrowBook(bookId: 2, memberId: 5); // Emma now has 1 active + 1 pending = at the limit

            using var db = new DatabaseHelper(_connectionString);
            int loanId = Convert.ToInt32(db.GetPendingLoans().Single(l => Convert.ToInt32(l["BookID"]) == 2)["LoanID"]);
            _approvalService.RejectLoan(loanId);

            var newRequest = _memberService.BorrowBook(bookId: 4, memberId: 5);
            Assert.IsTrue(newRequest.Success);
        }

        // TC-63: A reserved item not collected within 5 days of becoming available is cancelled and the
        // item's status returns to "Available" (REQ-20 AC1)
        [TestMethod]
        public void ExpireReservations_UncollectedAfterFiveDays_CancelsReservationAndFreesBook()
        {
            using var db = new DatabaseHelper(_connectionString);

            // BookID 17 is on loan to Carol (LoanID 8) with an active reservation held by David (ReservationID 2).
            Assert.IsTrue(db.ProcessReturn(loanId: 8, returnDate: DateTime.Today.AddDays(-6)));
            Assert.AreEqual("Reserved", db.GetBookById(17)!["Status"].ToString());

            int expiredCount = _expiryService.ExpireOverdueReservations(DateTime.Today);

            Assert.IsGreaterThan(0, expiredCount);
            Assert.AreEqual("Available", db.GetBookById(17)!["Status"].ToString());
        }

        // TC-64: A reservation collected within the 5-day window is fulfilled and does not expire (REQ-20 AC2)
        [TestMethod]
        public void ExpireReservations_CollectedWithinWindow_DoesNotExpire()
        {
            using var db = new DatabaseHelper(_connectionString);

            Assert.IsTrue(db.ProcessReturn(loanId: 8, returnDate: DateTime.Today.AddDays(-2)));

            int reservationId = Convert.ToInt32(db.GetAllActiveReservations()
                .Single(r => Convert.ToInt32(r["BookID"]) == 17 && r["Status"]!.ToString() == "Active")["ReservationID"]);
            var loanId = db.FulfillReservation(reservationId, DateTime.Today, loanPeriodDays: 14);
            Assert.IsNotNull(loanId);

            int expiredCount = _expiryService.ExpireOverdueReservations(DateTime.Today);
            Assert.AreEqual(0, expiredCount);
            Assert.AreEqual("On Loan", db.GetBookById(17)!["Status"].ToString());
        }

        // TC-65: Once a reservation expires, it no longer appears in the member's active reservations (REQ-20 AC3)
        [TestMethod]
        public void ExpireReservations_AfterExpiry_NoLongerAppearsInMembersActiveReservations()
        {
            using var db = new DatabaseHelper(_connectionString);

            Assert.IsTrue(db.ProcessReturn(loanId: 8, returnDate: DateTime.Today.AddDays(-6)));

            _expiryService.ExpireOverdueReservations(DateTime.Today);

            var davidReservations = db.GetActiveReservations(memberId: 4);
            Assert.IsFalse(davidReservations.Exists(r => Convert.ToInt32(r["BookID"]) == 17));
        }
    }
}
