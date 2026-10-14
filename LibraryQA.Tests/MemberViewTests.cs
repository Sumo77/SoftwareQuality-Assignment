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
        public void Setup() // Build a fresh database for this test
        {
            // Fresh database per test due to randomised run/completion order
            _dbPath = Path.Combine(Path.GetTempPath(), $"membertest_{Guid.NewGuid()}.db");

            Assert.IsTrue(new DatabaseInitializer(_dbPath).InitializeDatabase(),
                "Database schema could not be created - check DatabaseSchema.sql is in the test output folder.");
            Assert.IsTrue(new DatabaseSeeder(_dbPath).SeedSampleData(),
                "Sample data could not be loaded - check SampleData.sql is in the test output folder.");

            _connectionString = $"Data Source={_dbPath}";
            _service = new MemberActionsService(_connectionString);
        }

        [TestCleanup]
        public void Cleanup() // Delete this test's database
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
        }

        // TC-8: Borrowing an "available" book submits a pending request with a due date 14 days from
        // the current date, but leaves the book's status unchanged until staff approve it (REQ-18, REQ-2a, REQ-14)
        [TestMethod]
        [TestCategory("Smoke")]
        public void BorrowBook_AvailableBook_CreatesPendingRequestWithDueDate()
        {
            var result = _service.BorrowBook(bookId: 1, memberId: 1);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(DateTime.Today.AddDays(14), result.DueDate);

            using (var db = new DatabaseHelper(_connectionString))
            {
                var book = db.GetBookById(1);
                Assert.AreEqual("Available", book!["Status"].ToString());
            }
        }

        // TC-8b: Once staff approve a pending loan request, the book's status updates to "On Loan" (REQ-18)
        [TestMethod]
        public void ApproveLoan_PendingRequest_ActivatesLoanAndUpdatesBookStatus()
        {
            var result = _service.BorrowBook(bookId: 1, memberId: 1);
            Assert.IsTrue(result.Success);

            using (var db = new DatabaseHelper(_connectionString))
            {
                var pending = db.GetPendingLoans();
                int loanId = Convert.ToInt32(pending.Find(l => Convert.ToInt32(l["BookID"]) == 1)!["LoanID"]);

                var approvalService = new ApprovalService(_connectionString);
                var approval = approvalService.ApproveLoan(loanId);

                Assert.IsTrue(approval.Success);

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

        // TC-10: A member already holding two active loans cannot borrow a third - the request limit
        // counts active and pending loans/reservations together (REQ-18, REQ-8)
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

                Assert.HasCount(1, aliceLoans);
                Assert.IsFalse(aliceLoans.Exists(l =>
                    Convert.ToInt32(l["BookID"]) == 8 || Convert.ToInt32(l["BookID"]) == 10));

                Assert.HasCount(2, bobLoans);
                Assert.IsFalse(bobLoans.Exists(l => Convert.ToInt32(l["BookID"]) == 7));
            }
        }

        // TC-12: Searching for a literal LIKE wildcard character ("%" or "_") does not match every
        // book in the catalogue - it only matches titles/authors/ISBNs that actually contain it (REQ-1, REQ-9)
        [TestMethod]
        public void SearchCatalogue_WildcardCharacters_DoNotMatchEveryBook()
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                int totalBooks = db.SearchCatalogue("").Count;

                var percentResults = db.SearchCatalogue("%");
                var underscoreResults = db.SearchCatalogue("_");

                Assert.IsLessThan(totalBooks, percentResults.Count,
                    "Searching for a literal '%' should not match every book in the catalogue.");
                Assert.IsLessThan(totalBooks, underscoreResults.Count,
                    "Searching for a literal '_' should not match every book in the catalogue.");
            }
        }

        // TC-14: Reserving a book that is on loan records the hold but leaves the book On Loan (DEF-06, REQ-3)
        [TestMethod]
        public void ReserveBook_BookOnLoan_KeepsStatusOnLoan()
        {
            // Seed: BookID 8 is On Loan to member 2 with no reservation; member 1 does not hold it.
            var result = _service.ReserveBook(bookId: 8, memberId: 1);

            Assert.IsTrue(result.Success, result.Message);

            using (var db = new DatabaseHelper(_connectionString))
            {
                Assert.AreEqual("On Loan", db.GetBookById(8)!["Status"]?.ToString(),
                    "A book must stay On Loan until the current borrower returns it.");
                Assert.IsTrue(db.HasActiveReservation(8),
                    "The reservation itself must still be recorded against the book.");
            }
        }

        // TC-15: A member cannot reserve a book they already have on loan (DEF-13, REQ-3)
        [TestMethod]
        public void ReserveBook_MemberAlreadyHasItOnLoan_IsRejected()
        {
            // Seed: member 2 holds BookID 8 on LoanID 6.
            var result = _service.ReserveBook(bookId: 8, memberId: 2);

            Assert.IsFalse(result.Success);
            Assert.AreEqual("You already have this book on loan.", result.Message);

            using (var db = new DatabaseHelper(_connectionString))
            {
                Assert.IsFalse(db.HasActiveReservation(8),
                    "A rejected reservation must not be written to the database.");
            }
        }
        // TC-97: The 2-item limit permits exactly two and refuses the third with a message naming the
        // limit, and returning an item releases the allowance again (REQ-8)
        [TestMethod]
        public void BorrowBook_LimitPermitsExactlyTwoAndReleasesOnReturn()
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                // Alice (1) starts with one loan, LoanID 3 on BookID 7. TC-10 covers a member already at
                // the limit; this covers the boundary - that the second item is permitted.
                Assert.AreEqual(1, db.GetActiveOrPendingCount(1),
                    "Precondition check: Alice should start with one item.");

                var second = _service.BorrowBook(bookId: 2, memberId: 1);
                Assert.IsTrue(second.Success,
                    $"A member holding one item must be allowed a second. Refused with: {second.Message}");
                Assert.AreEqual(2, db.GetActiveOrPendingCount(1));

                var third = _service.BorrowBook(bookId: 3, memberId: 1);
                Assert.IsFalse(third.Success, "A third item must be refused.");
                StringAssert.Contains(third.Message, "limit has been reached",
                    "REQ-8 asks for a message stating the limit has been reached.");
                StringAssert.Contains(third.Message, "2",
                    "The message should name the limit so the member knows what it is.");

                Assert.AreEqual(2, db.GetActiveOrPendingCount(1),
                    "A refused request must not be recorded against the member.");

                // Returning the original loan must free the allowance, or a member could be locked
                // out of borrowing permanently.
                Assert.IsTrue(db.ProcessReturn(loanId: 3, DateTime.Today, "Good"));
                Assert.AreEqual(1, db.GetActiveOrPendingCount(1));

                Assert.IsTrue(_service.BorrowBook(bookId: 3, memberId: 1).Success,
                    "Returning an item must release the allowance it was using.");
            }
        }

    }
}