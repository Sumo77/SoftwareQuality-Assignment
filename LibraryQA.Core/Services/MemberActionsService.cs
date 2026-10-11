using LibraryQA.Core.Database;
using System;
using System.Linq;

namespace LibraryQA.Core.Services
{
    // Member Actions Service handles borrowing and reserving book functions for members
    public class MemberActionsService
    {
        private readonly string _connectionString;

        private const int MaxActiveLoans = 2;

        private const int LoanPeriodDays = 14;

        public MemberActionsService(string connectionString) // Initialise Database Access Connection
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public BorrowResult BorrowBook(int bookId, int memberId) // Book borrowing process for members, performs checks then performs loan
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                var account = db.GetAccountInfo(memberId);

                if (account == null)
                {
                    return new BorrowResult { Success = false, Message = $"No member found with ID {memberId}." };
                }

                if (!string.Equals(account["Role"]?.ToString(), "Member", StringComparison.OrdinalIgnoreCase))
                {
                    return new BorrowResult { Success = false, Message = $"Account {memberId} is not a Member account and cannot borrow items." };
                }
                if (string.Equals(account["AccountStatus"]?.ToString(), "Suspended", StringComparison.OrdinalIgnoreCase))
                {
                    return new BorrowResult { Success = false, Message = $"Account {memberId} is suspended and cannot borrow items." };
                }

                var book = db.GetBookById(bookId);

                if (book == null) // Check if book exists
                {
                    return new BorrowResult { Success = false, Message = "Book not found." };
                }

                if (book["Status"].ToString() != "Available") // Checks if book is avliable to borrow
                {
                    return new BorrowResult { Success = false, Message = "This book is currently not available to borrow - It may be able to be reserved, otherwise please check back and try again at a later date." };
                }

                // REQ-18: active and pending loans/reservations together count towards the limit
                if (db.GetActiveOrPendingCount(memberId) >= MaxActiveLoans)
                {
                    return new BorrowResult { Success = false, Message = "The borrowing limit has been reached - Maximum " + MaxActiveLoans + " books can be borrowed at a time." };
                }

                DateTime loanDate = DateTime.Today;
                DateTime dueDate = loanDate.AddDays(LoanPeriodDays);

// REQ-18: the loan request is held as "Pending" until a staff member approves or rejects it
int? loanId = db.CreateLoanRequest(bookId, memberId, loanDate, dueDate);

                if (loanId == null) // Check book loan request was successful
                {
                    return new BorrowResult { Success = false, Message = "Unable to complete the loan request. Please try again." };
                }

return new BorrowResult
{
    Success = true,
    Message = "Your request to borrow " + book["Title"] + " has been submitted and is pending staff approval.",
    DueDate = dueDate,
    LoanId = loanId,
    BookTitle = book["Title"]?.ToString()
};
            }
        }

        public ReserveResult ReserveBook(int bookId, int memberId) // Book reserving process for members, performs checks then performs reservation
        {
            using (var db = new DatabaseHelper(_connectionString))
            {
                var book = db.GetBookById(bookId);

                if (book == null) // Check if book exists
                {
                    return new ReserveResult { Success = false, Message = "Book not found." };
                }

                if (book["Status"].ToString() == "Available") // Check if book is available, as a reservation is then not needed
                {
                    return new ReserveResult
                    {
                        Success = false,
                        Message = "This book is currently available! - You may borrow it directly instead of reserving it."
                    };
                }

                if (db.HasActiveReservation(bookId)) // Check if book is already reserved, as then unable to reserve
                {
                    return new ReserveResult { Success = false, Message = "Apologies, this book is already reserved - Please check back and try again at a later date." };
                }

                bool alreadyOnLoanToThisMember = db.GetActiveLoans(memberId) // Check if the member already has this exact book on loan
                    .Any(loan => Convert.ToInt32(loan["BookID"]) == bookId);

                if (alreadyOnLoanToThisMember)
                {
                    return new ReserveResult { Success = false, Message = "You already have this book on loan." };
                }

                // REQ-18: active and pending loans/reservations together count towards the limit
                if (db.GetActiveOrPendingCount(memberId) >= MaxActiveLoans)
                {
                    return new ReserveResult { Success = false, Message = "The borrowing limit has been reached - Maximum " + MaxActiveLoans + " books can be borrowed or reserved at a time." };
                }

                // REQ-18: the reservation request is held as "Pending" until a staff member approves or rejects it.
                // The book's status is not changed until approval - ApproveReservation and ProcessReturn
                // are responsible for promoting the catalogue status once the request is active.
                int? reservationId = db.CreateReservationRequest(bookId, memberId, DateTime.Today);

                if (reservationId == null) // Check if reservation request was successful
                {
                    return new ReserveResult
                    {
                        Success = false,
                        Message = "Unable to reserve the book - It may have been reserved by someone else, so please check back and try again at a later date."
                    };
                }

                return new ReserveResult { Success = true, Message = "Your request to reserve the book has been submitted and is pending staff approval." };
            }
        }
    }
}