using System;
using System.Linq;
using LibraryQA.Core.Database;

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
                var book = db.GetBookById(bookId);

                if (book == null) // Check if book exists
                {
                    return new BorrowResult { Success = false, Message = "Book not found." };
                }

                if (book["Status"].ToString() != "Available") // Checks if book is avliable to borrow
                {
                    return new BorrowResult { Success = false, Message = "This book is currently not available to borrow - It may be able to be reserved, otherwise please check back and try again at a later date." };
                }

                if (db.GetActiveLoanCount(memberId) >= MaxActiveLoans) // Checks if member has reached borrowing limit
                {
                    return new BorrowResult { Success = false, Message = "The borrowing limit has been reached - Maximum " + MaxActiveLoans + " books can be borrowed at a time." };
                }

                DateTime loanDate = DateTime.Today;
                DateTime dueDate = loanDate.AddDays(LoanPeriodDays);

                int? loanId = db.CreateLoan(bookId, memberId, loanDate, dueDate); // Create book loan

                if (loanId == null) // Check book loan was successful
                {
                    return new BorrowResult { Success = false, Message = "Unable to complete the loan. Please try again." };
                }

                db.UpdateBookStatus(bookId, "On Loan"); // Update book status to properly reflect new status, return result

                return new BorrowResult { Success = true, Message = "Borrowed " + book["Title"] + " successfully.", DueDate = dueDate };
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

                int? reservationId = db.CreateReservation(bookId, memberId, DateTime.Today); // Create reservation

                if (reservationId == null) // Check if reservation was successful
                {
                    return new ReserveResult
                    {
                        Success = false,
                        Message = "Unable to reserve the book - It may have been reserved by someone else, so please check back and try again at a later date."
                    };
                }

                // Only promote the catalogue status to Reserved if the book isn't currently On Loan -
                // it must stay On Loan until the current borrower returns it. ProcessReturn is
                // responsible for transitioning the status to Reserved once the loan is closed.
                if (book["Status"].ToString() != "On Loan")
                {
                    db.UpdateBookStatus(bookId, "Reserved");
                }

                return new ReserveResult { Success = true, Message = "Reserved successfully." };
            }
        }
    }
}