using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LibraryQA.Core.Database;

namespace LibraryQA.Views
{
    public partial class StaffView : UserControl
    {
        private const int LoanPeriodDays = 14;

        //MainWindow subscribes to this to return to LoginView.
        public event EventHandler? LogoutRequested;

        public StaffView()
        {
            InitializeComponent();
        }

        private void StaffView_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshAllData();
        }

        private void RefreshAllData()
        {
            LoadOverdueItems();
            LoadReservations();
            LoadReporting();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            LogoutRequested?.Invoke(this, EventArgs.Empty);
        }

        private void IssueButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(MemberIdBox.Text.Trim(), out int memberId) ||
                !int.TryParse(BookIdBox.Text.Trim(), out int bookId))
            {
                ShowStatus("Please enter a valid numeric Member ID and Book ID.", isError: true);
                return;
            }

            using var db = new DatabaseHelper(App.ConnectionString);

            var book = db.GetBookById(bookId);
            if (book == null)
            {
                ShowStatus($"No book found with ID {bookId}.", isError: true);
                return;
            }

            if (!string.Equals(book["Status"]?.ToString(), "Available", StringComparison.OrdinalIgnoreCase))
            {
                ShowStatus($"'{book["Title"]}' is already {book["Status"]} and cannot be issued.", isError: true);
                return;
            }

            var loanDate = DateTime.Now.Date;
            var dueDate = loanDate.AddDays(LoanPeriodDays);

            var loanId = db.CreateLoan(bookId, memberId, loanDate, dueDate);
            if (loanId == null)
            {
                ShowStatus("Unable to issue the loan. The item may no longer be available.", isError: true);
                return;
            }

            ShowStatus($"Loan #{loanId} issued for '{book["Title"]}' (due {dueDate:yyyy-MM-dd}).", isError: false);
            RefreshAllData();
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(MemberIdBox.Text.Trim(), out int memberId) ||
                !int.TryParse(LoanIdBox.Text.Trim(), out int loanId))
            {
                ShowStatus("Please enter a valid numeric Member ID and Loan ID to process a return.", isError: true);
                return;
            }

            using var db = new DatabaseHelper(App.ConnectionString);

            var loan = db.GetLoanById(loanId);
            if (loan == null)
            {
                ShowStatus($"No loan found with ID {loanId}.", isError: true);
                return;
            }

            if (loan["ReturnDate"] != DBNull.Value)
            {
                ShowStatus($"Loan #{loanId} has already been returned.", isError: true);
                return;
            }

            if (Convert.ToInt32(loan["MemberID"]) != memberId)
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

            int bookId = Convert.ToInt32(loan["BookID"]);
            string newStatus = db.HasActiveReservation(bookId) ? "Reserved" : "Available";
            db.UpdateBookStatus(bookId, newStatus);

            ShowStatus($"Loan #{loanId} processed as returned.", isError: false);
            RefreshAllData();
        }

        private void LoadOverdueItems()
        {
            using var db = new DatabaseHelper(App.ConnectionString);

            var overdue = db.GetAllOverdueLoans()
                .Select(row => new OverdueItem
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

        private void LoadReservations()
        {
            using var db = new DatabaseHelper(App.ConnectionString);

            var reservations = db.GetAllActiveReservations()
                .Select(row => new ReservationItem
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

        private void LoadReporting()
        {
            using var db = new DatabaseHelper(App.ConnectionString);

            var stats = db.GetStaffStatistics();
            TotalOnLoanText.Text = $"Items currently on loan: {stats["TotalOnLoan"]}";
            TotalOverdueText.Text = $"Items overdue: {stats["TotalOverdue"]}";
            TotalReservationsText.Text = $"Active reservations: {stats["TotalReservations"]}";

            var mostBorrowed = db.GetMostBorrowedBooks()
                .Select(row => new MostBorrowedItem
                {
                    Title = row["Title"]?.ToString() ?? "",
                    TimesBorrowed = Convert.ToInt32(row["BorrowCount"])
                })
                .ToList();

            MostBorrowedListView.ItemsSource = mostBorrowed;
        }

        private void ShowStatus(string message, bool isError)
        {
            IssueReturnStatusText.Text = message;
            IssueReturnStatusText.Foreground = isError
                ? System.Windows.Media.Brushes.Red
                : System.Windows.Media.Brushes.Green;
        }

        private class OverdueItem
        {
            public int LoanId { get; set; }
            public int BookId { get; set; }
            public string Title { get; set; } = "";
            public string MemberName { get; set; } = "";
            public int MemberId { get; set; }
            public string DueDate { get; set; } = "";
            public int DaysOverdue { get; set; }
        }

        private class ReservationItem
        {
            public string Title { get; set; } = "";
            public int BookId { get; set; }
            public string MemberName { get; set; } = "";
            public int MemberId { get; set; }
            public string ReservedOn { get; set; } = "";
        }

        private class MostBorrowedItem
        {
            public string Title { get; set; } = "";
            public int TimesBorrowed { get; set; }
        }
    }
}
