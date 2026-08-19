using System;
using LibraryQA.Core.Database;

namespace LibraryQA.Core.Services
{
    /// <summary>
    /// Encapsulates the member-facing borrowing and reservation business rules
    /// (REQ-2a, REQ-3, REQ-8, REQ-14) in one place, independent of the WPF UI,
    /// so they can be unit tested directly (REQ-13).
    /// </summary>
    public class MemberActionsService
    {
        private readonly string _connectionString;

        // REQ-8: loan limit enforced in the application layer.
        private const int MaxActiveLoans = 2;

        // REQ-2a: standard loan period.
        private const int LoanPeriodDays = 14;

        public MemberActionsService(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <summary>
        /// Attempts to borrow a book for a member, per REQ-2a (loan/due date, status change)
        /// and REQ-8 (loan limit).
        /// </summary>
        public BorrowResult BorrowBook(int bookId, int memberId)
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                var book = db.GetBookById(bookId);

                if (book == null)
                {
                    return new BorrowResult { Success = false, Message = "Book not found." };
                }

                // REQ-14: status must reflect reality before we allow a state change.
                if (book["Status"].ToString() != "Available")
                {
                    return new BorrowResult { Success = false, Message = "This book is currently not available to borrow - It may be able to be reserved, otherwise please check back and try again at a later date." };
                }

                // REQ-8
                if (db.GetActiveLoanCount(memberId) >= MaxActiveLoans)
                {
                    return new BorrowResult { Success = false, Message = "The borrowing limit has been reached - Maximum " + MaxActiveLoans + " books can be borrowed at a time." };
                }

                DateTime loanDate = DateTime.Today;
                DateTime dueDate = loanDate.AddDays(LoanPeriodDays);

                int? loanId = db.CreateLoan(bookId, memberId, loanDate, dueDate);

                if (loanId == null)
                {
                    return new BorrowResult { Success = false, Message = "Unable to complete the loan. Please try again." };
                }

                db.UpdateBookStatus(bookId, "On Loan");

                return new BorrowResult { Success = true, Message = "Borrowed " + book["Title"] + " successfully.", DueDate = dueDate };
            }
        }

        /// <summary>
        /// Attempts to reserve a book that is currently on loan, per REQ-3
        /// (single active reservation per item) and REQ-14 (status integrity).
        /// </summary>
        public ReserveResult ReserveBook(int bookId, int memberId)
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                var book = db.GetBookById(bookId);

                if (book == null)
                {
                    return new ReserveResult { Success = false, Message = "Book not found." };
                }

                if (book["Status"].ToString() == "Available")
                {
                    return new ReserveResult
                    {
                        Success = false,
                        Message = "This book is currently available! — You may borrow it directly instead of reserving it."
                    };
                }

                // REQ-3: reject if another member already holds the active reservation.
                if (db.HasActiveReservation(bookId))
                {
                    return new ReserveResult { Success = false, Message = "Apologies, this book is already reserved - Please check back and try again at a later date." };
                }

                int? reservationId = db.CreateReservation(bookId, memberId, DateTime.Today);

                if (reservationId == null)
                {
                    return new ReserveResult
                    {
                        Success = false,
                        Message = "Unable to reserve the book - It may have been reserved by someone else, so please check back and try again at a later date."
                    };
                }

                db.UpdateBookStatus(bookId, "Reserved");

                return new ReserveResult { Success = true, Message = "Reserved successfully." };
            }
        }
    }
}