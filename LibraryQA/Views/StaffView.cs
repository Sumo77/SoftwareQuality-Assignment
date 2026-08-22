using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LibraryQA.Core.Database;

namespace LibraryQA.Views
{
    // Staff Interface, allowing staff to manage loans, returns, reservations, and view reporting data
    public partial class StaffView : UserControl
    {
        private const int LoanPeriodDays = 14;

        public event EventHandler? LogoutRequested;

        public StaffView() // Initialise Member View
        {
            InitializeComponent();
        }

        private void StaffView_Loaded(object sender, RoutedEventArgs e) // Load all staff related data when the interface is loaded
        {
            RefreshAllData();
        }

        private void RefreshAllData() // Refresh all data displayed in the staff interface
        {
            LoadOverdueItems();
            LoadReservations();
            LoadReporting();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e) // Logout and return to the login interface
        {
            LogoutRequested?.Invoke(this, EventArgs.Empty);
        }

        private void IssueButton_Click(object sender, RoutedEventArgs e) // Issue a loan for the specified member and book
        {
            if (!int.TryParse(MemberIdBox.Text.Trim(), out int memberId) ||
                !int.TryParse(BookIdBox.Text.Trim(), out int bookId)) // Validate that the Member ID and Book ID are numeric
            {
                ShowStatus("Please enter a valid numeric Member ID and Book ID.", isError: true);
                return;
            }

            using var db = new DatabaseHelper(App.ConnectionString);

            var book = db.GetBookById(bookId);
            if (book == null) // Check if the book exists in the database
            {
                ShowStatus($"No book found with ID {bookId}.", isError: true);
                return;
            }

            if (!string.Equals(book["Status"]?.ToString(), "Available", StringComparison.OrdinalIgnoreCase)) // Check if the book is available for loan
            {
                ShowStatus($"'{book["Title"]}' is already {book["Status"]} and cannot be issued.", isError: true);
                return;
            }

            var loanDate = DateTime.Now.Date;
            var dueDate = loanDate.AddDays(LoanPeriodDays);

            var loanId = db.CreateLoan(bookId, memberId, loanDate, dueDate);
            if (loanId == null) // Check if the loan was successfully created
            {
                ShowStatus("Unable to issue the loan. The item may no longer be available.", isError: true);
                return;
            }

            ShowStatus($"Loan #{loanId} issued for '{book["Title"]}' (due {dueDate:yyyy-MM-dd}).", isError: false);
            RefreshAllData();
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e) // Process the return of a loan for the specified member and loan ID
        {
            if (!int.TryParse(MemberIdBox.Text.Trim(), out int memberId) ||
                !int.TryParse(LoanIdBox.Text.Trim(), out int loanId)) // Validate that the Member ID and Loan ID are numeric
            {
                ShowStatus("Please enter a valid numeric Member ID and Loan ID to process a return.", isError: true);
                return;
            }

            using var db = new DatabaseHelper(App.ConnectionString);

            var loan = db.GetLoanById(loanId);
            if (loan == null) // Check if the loan exists in the database
            {
                ShowStatus($"No loan found with ID {loanId}.", isError: true);
                return;
            }

            if (loan["ReturnDate"] != DBNull.Value) // Check if the loan has already been returned
            {
                ShowStatus($"Loan #{loanId} has already been returned.", isError: true);
                return;
            }

            if (Convert.ToInt32(loan["MemberID"]) != memberId) // Check if the loan belongs to the specified member
            {
                ShowStatus($"Loan #{loanId} belongs to Member ID {loan["MemberID"]}, not {memberId}. Please check the IDs and try again.", isError: true);
                return;
            }

            bool success = db.ProcessReturn(loanId, DateTime.Now.Date);
            if (!success)
            {
                ShowStatus($"Loan #{loanId} could not be returned. It may already be returned or does not exist.", isError: true);
                return;
            }

            int bookId = Convert.ToInt32(loan["BookID"]); // Get the Book ID associated with the loan to update its status
            string newStatus = db.HasActiveReservation(bookId) ? "Reserved" : "Available";
            db.UpdateBookStatus(bookId, newStatus);

            ShowStatus($"Loan #{loanId} processed as returned.", isError: false);
            RefreshAllData();
        }

        private void LoadOverdueItems() // Load all overdue loans and display them in the list view
        {
            using var db = new DatabaseHelper(App.ConnectionString);

            var overdue = db.GetAllOverdueLoans()
                .Select(row => new OverdueItem // Map each row from the database to an OverdueItem object
                {
                    LoanId = Convert.ToInt32(row["LoanID"]),
                    BookId = Convert.ToInt32(row["BookID"]),
                    Title = row["Title"]?.ToString() ?? "",
                    MemberName = row["MemberName"]?.ToString() ?? "",
                    MemberId = Convert.ToInt32(row["MemberID"]),
                    DueDate = row["DueDate"]?.ToString() ?? "",
                    DaysOverdue = Convert.ToInt32(row["DaysOverdue"])
                })
                .ToList();

            OverdueListView.ItemsSource = overdue;
        }

        private void LoadReservations() // Load all active reservations and display them in the list view
        {
            using var db = new DatabaseHelper(App.ConnectionString);

            var reservations = db.GetAllActiveReservations()
                .Select(row => new ReservationItem // Map each row from the database to a ReservationItem object
                {
                    Title = row["Title"]?.ToString() ?? "",
                    BookId = Convert.ToInt32(row["BookID"]),
                    MemberName = row["MemberName"]?.ToString() ?? "",
                    MemberId = Convert.ToInt32(row["MemberID"]),
                    ReservedOn = row["ReservationDate"]?.ToString() ?? ""
                })
                .ToList();

            ReservationsListView.ItemsSource = reservations;
        }

        private void LoadReporting() // Load reporting statistics and the most borrowed books, then display them in the interface
        {
            using var db = new DatabaseHelper(App.ConnectionString);

            var stats = db.GetStaffStatistics();
            TotalOnLoanText.Text = $"Items currently on loan: {stats["TotalOnLoan"]}";
            TotalOverdueText.Text = $"Items overdue: {stats["TotalOverdue"]}";
            TotalReservationsText.Text = $"Active reservations: {stats["TotalReservations"]}";

            var mostBorrowed = db.GetMostBorrowedBooks()
                .Select(row => new MostBorrowedItem // Map each row from the database to a MostBorrowedItem object
                {
                    Title = row["Title"]?.ToString() ?? "",
                    TimesBorrowed = Convert.ToInt32(row["BorrowCount"])
                })
                .ToList();

            MostBorrowedListView.ItemsSource = mostBorrowed;
        }

        private void ShowStatus(string message, bool isError) // Display a status message in the interface, with different formatting for errors and success messages
        {
            IssueReturnStatusText.Text = isError ? $"⚠ {message}" : $"✓ {message}";
            IssueReturnStatusText.Foreground = isError
                ? System.Windows.Media.Brushes.Red
                : System.Windows.Media.Brushes.Green;
        }

        private class OverdueItem // Represents an overdue loan item with relevant details for display in the staff interface
        {
            public int LoanId { get; set; }
            public int BookId { get; set; }
            public string Title { get; set; } = "";
            public string MemberName { get; set; } = "";
            public int MemberId { get; set; }
            public string DueDate { get; set; } = "";
            public int DaysOverdue { get; set; }
        }

        private class ReservationItem // Represents an active reservation item with relevant details for display in the staff interface
        {
            public string Title { get; set; } = "";
            public int BookId { get; set; }
            public string MemberName { get; set; } = "";
            public int MemberId { get; set; }
            public string ReservedOn { get; set; } = "";
        }

        private class MostBorrowedItem // Represents a book that has been borrowed the most times, with relevant details for display in the staff interface
        {
            public string Title { get; set; } = "";
            public int TimesBorrowed { get; set; }
        }
    }
}
