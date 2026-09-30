using LibraryQA.Core.Database;
using LibraryQA.Core.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LibraryQA.Views
{
    // Staff Interface, allowing staff to manage loans, returns, reservations, members, and view reporting data
    public partial class StaffView : UserControl
    {
        public event EventHandler? LogoutRequested;

        public StaffView() // Initialise Staff View
        {
            InitializeComponent();
        }

        private void StaffView_Loaded(object sender, RoutedEventArgs e) // Load all staff related data when the interface is loaded
        {
            RefreshAllData();
        }

        private void RefreshAllData() // Refresh all data displayed in the staff interface
        {
            LoadMemberPicker();
            LoadBookPicker();
            LoadLoanPicker();
            LoadOverdueItems();
            LoadReservations();
            LoadMembersList();
            LoadReporting();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e) // Logout and return to the login interface
        {
            LogoutRequested?.Invoke(this, EventArgs.Empty);
        }

        private void IssueButton_Click(object sender, RoutedEventArgs e) // Issue a loan for the selected member and book
        {
            if (MemberComboBox.SelectedItem is not PickerItem selectedMember ||
                BookComboBox.SelectedItem is not PickerItem selectedBook)
            {
                ShowStatus("Please select a member and a book.", isError: true);
                return;
            }

            var service = new MemberActionsService(App.ConnectionString);
            var result = service.BorrowBook(selectedBook.Id, selectedMember.Id);

            if (!result.Success)
            {
                ShowStatus(result.Message, isError: true);
                return;
            }

            ShowStatus($"Loan #{result.LoanId} issued for '{result.BookTitle}' (due {result.DueDate:yyyy-MM-dd}).", isError: false);
            RefreshAllData();
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e) // Process the return of the selected active loan
        {
            if (LoanComboBox.SelectedItem is not PickerItem selectedLoan)
            {
                ShowStatus("Please select an active loan to return.", isError: true);
                return;
            }

            if (ConditionComboBox.SelectedItem is not ComboBoxItem selectedConditionItem)
            {
                ShowStatus("Please select the condition of the returned item.", isError: true);
                return;
            }

            string condition = selectedConditionItem.Content?.ToString() ?? "Good";
            int loanId = selectedLoan.Id;

            using var db = new DatabaseHelper(App.ConnectionString);

            var loan = db.GetLoanById(loanId);
            if (loan == null || loan["ReturnDate"] != DBNull.Value) // Guard against a stale picker entry (already returned since the list was loaded)
            {
                ShowStatus($"Loan #{loanId} could not be returned - it may have already been processed. Refreshing the list.", isError: true);
                RefreshAllData();
                return;
            }

            bool success = db.ProcessReturn(loanId, DateTime.Now.Date, condition); // Also updates the book's status internally (Available or Reserved)
            if (!success)
            {
                ShowStatus($"Loan #{loanId} could not be returned. It may already be returned or does not exist.", isError: true);
                return;
            }

            ShowStatus($"Loan #{loanId} processed as returned ({condition} condition).", isError: false);
            RefreshAllData();
        }

        private void SuspendButton_Click(object sender, RoutedEventArgs e) // Suspend the selected member's account (REQ-19)
        {
            if (MembersListView.SelectedItem is not MemberRow selectedMember)
            {
                ShowMemberActionStatus("Please select a member from the list.", isError: true);
                return;
            }

            using var db = new DatabaseHelper(App.ConnectionString);
            db.SetAccountStatus(selectedMember.AccountId, "Suspended");

            ShowMemberActionStatus($"{selectedMember.FullName} (ID {selectedMember.AccountId}) has been suspended.", isError: false);
            RefreshAllData();
        }

        private void ReactivateButton_Click(object sender, RoutedEventArgs e) // Reactivate the selected member's account (REQ-19)
        {
            if (MembersListView.SelectedItem is not MemberRow selectedMember)
            {
                ShowMemberActionStatus("Please select a member from the list.", isError: true);
                return;
            }

            using var db = new DatabaseHelper(App.ConnectionString);
            db.SetAccountStatus(selectedMember.AccountId, "Active");

            ShowMemberActionStatus($"{selectedMember.FullName} (ID {selectedMember.AccountId}) has been reactivated.", isError: false);
            RefreshAllData();
        }

        private void LoadMemberPicker() // Populates the Issue-Loan member picker with active (non-suspended) members only
        {
            using var db = new DatabaseHelper(App.ConnectionString);

            var members = db.GetAllMembers()
                .Where(row => string.Equals(row["AccountStatus"]?.ToString(), "Active", StringComparison.OrdinalIgnoreCase))
                .Select(row => new PickerItem
                {
                    Id = Convert.ToInt32(row["AccountID"]),
                    Display = $"{row["AccountID"]} - {row["FirstName"]} {row["LastName"]} ({row["Username"]})"
                })
                .ToList();

            MemberComboBox.ItemsSource = members;
        }

        private void LoadBookPicker() // Populates the Issue-Loan book picker with Available books only
        {
            using var db = new DatabaseHelper(App.ConnectionString);

            var books = db.SearchCatalogue("") // empty search term matches the whole catalogue
                .Where(row => string.Equals(row["Status"]?.ToString(), "Available", StringComparison.OrdinalIgnoreCase))
                .Select(row => new PickerItem
                {
                    Id = Convert.ToInt32(row["BookID"]),
                    Display = $"{row["BookID"]} - {row["Title"]}"
                })
                .ToList();

            BookComboBox.ItemsSource = books;
        }

        private void LoadLoanPicker() // Populates the Process-Return picker with every currently active loan system-wide
        {
            using var db = new DatabaseHelper(App.ConnectionString);

            var loans = db.GetAllActiveLoans()
                .Select(row => new PickerItem
                {
                    Id = Convert.ToInt32(row["LoanID"]),
                    Display = $"#{row["LoanID"]} - {row["Title"]} ({row["MemberName"]}, due {row["DueDate"]})"
                })
                .ToList();

            LoanComboBox.ItemsSource = loans;
        }

        private void LoadMembersList() // Populates the Members tab with every member, any status, for suspend/reactivate management
        {
            using var db = new DatabaseHelper(App.ConnectionString);

            var members = db.GetAllMembers()
                .Select(row => new MemberRow
                {
                    AccountId = Convert.ToInt32(row["AccountID"]),
                    FullName = $"{row["FirstName"]} {row["LastName"]}",
                    Username = row["Username"]?.ToString() ?? "",
                    Status = row["AccountStatus"]?.ToString() ?? ""
                })
                .ToList();

            MembersListView.ItemsSource = members;
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
                    ReservationId = Convert.ToInt32(row["ReservationID"]),
                    Title = row["Title"]?.ToString() ?? "",
                    BookId = Convert.ToInt32(row["BookID"]),
                    MemberName = row["MemberName"]?.ToString() ?? "",
                    MemberId = Convert.ToInt32(row["MemberID"]),
                    ReservedOn = row["ReservationDate"]?.ToString() ?? ""
                })
                .ToList();

            ReservationsListView.ItemsSource = reservations;
        }

        private void FulfilButton_Click(object sender, RoutedEventArgs e) // Marks a held reservation as collected: closes the reservation and issues the loan for the collecting member
        {
            if (!int.TryParse(ReservationIdBox.Text.Trim(), out int reservationId)) // Validate that the Reservation ID is numeric
            {
                ShowReservationStatus("Please enter a valid numeric Reservation ID.", isError: true);
                return;
            }

            using var db = new DatabaseHelper(App.ConnectionString);

            var loanId = db.FulfillReservation(reservationId, DateTime.Now.Date, LoanPeriodDays);
            if (loanId == null) // Check if the reservation could be fulfilled
            {
                ShowReservationStatus($"Reservation #{reservationId} could not be fulfilled. It may not exist, already be fulfilled, or the book may not yet be held for collection.", isError: true);
                return;
            }

            ShowReservationStatus($"Reservation #{reservationId} fulfilled - Loan #{loanId} issued.", isError: false);
            RefreshAllData();
        }

        private void LoadReporting() // Load reporting statistics and the most borrowed books, then display them in the interface
        {
            using var db = new DatabaseHelper(App.ConnectionString);

            var stats = db.GetStaffStatistics();
            TotalOnLoanText.Text = $"Items currently on loan: {stats["TotalOnLoan"]}";
            TotalOverdueText.Text = $"Items overdue: {stats["TotalOverdue"]}";
            TotalReservationsText.Text = $"Active reservations: {stats["TotalReservations"]}";
            TotalMembersText.Text = $"Total members: {stats["TotalMembers"]}";
            TotalBooksText.Text = $"Total books: {stats["TotalBooks"]}";
            AvailableBooksText.Text = $"Available books: {stats["AvailableBooks"]}";

            var mostBorrowed = db.GetMostBorrowedBooks()
                .Select(row => new MostBorrowedItem // Map each row from the database to a MostBorrowedItem object
                {
                    Title = row["Title"]?.ToString() ?? "",
                    TimesBorrowed = Convert.ToInt32(row["BorrowCount"])
                })
                .ToList();

            MostBorrowedListView.ItemsSource = mostBorrowed;
        }

        private void ShowStatus(string message, bool isError) // Display a status message for Issue/Return, with different formatting for errors and success messages
        {
            IssueReturnStatusText.Text = isError ? $"⚠ {message}" : $"✓ {message}";
            IssueReturnStatusText.Foreground = isError
                ? System.Windows.Media.Brushes.Red
                : System.Windows.Media.Brushes.Green;
        }

        private void ShowMemberActionStatus(string message, bool isError) // Display a status message for Suspend/Reactivate actions
        {
            MemberActionStatusText.Text = isError ? $"⚠ {message}" : $"✓ {message}";
            MemberActionStatusText.Foreground = isError
                ? System.Windows.Media.Brushes.Red
                : System.Windows.Media.Brushes.Green;
        }

        private void ShowReservationStatus(string message, bool isError) // Display a status message for reservation fulfilment, with different formatting for errors and success messages
        {
            ReservationStatusText.Text = isError ? $"⚠ {message}" : $"✓ {message}";
            ReservationStatusText.Foreground = isError
                ? System.Windows.Media.Brushes.Red
                : System.Windows.Media.Brushes.Green;
        }

        // Generic display item for the Member/Book/Loan pickers - WPF's ComboBox
        // shows ToString() by default when no DisplayMemberPath is set, so this
        // keeps the XAML simple while still carrying the real ID for lookups.
        private class PickerItem
        {
            public int Id { get; set; }
            public string Display { get; set; } = "";
            public override string ToString() => Display;
        }

        private class MemberRow // Represents a member account row for the Members management tab
        {
            public int AccountId { get; set; }
            public string FullName { get; set; } = "";
            public string Username { get; set; } = "";
            public string Status { get; set; } = "";
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
            public int ReservationId { get; set; }
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
