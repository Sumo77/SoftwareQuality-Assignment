using LibraryQA.Core.Database;
using LibraryQA.Core.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using Microsoft.Data.Sqlite;

namespace LibraryQA.Views
{
    // Staff Interface, allowing staff to manage loans, returns, reservations, members, and view reporting data
    public partial class StaffView : UserControl
    {
        public event EventHandler? LogoutRequested;
        private const int LoanPeriodDays = 14;
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
            try
            {
                LoadMemberPicker();
                LoadBookPicker();
                LoadLoanPicker();
                LoadOverdueItems();
                LoadReservations();
                LoadMembersList();
                LoadReporting();
            }
            catch (SqliteException ex) // DEF-11: a database failure must not close the application
            {
                Debug.WriteLine($"Could not load staff data - database error: {ex.Message}");
                UserMessage.Show(IssueReturnStatusText, MessageKind.Error, UserMessage.DatabaseUnavailable);
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e) // Logout and return to the login interface
        {
            LogoutRequested?.Invoke(this, EventArgs.Empty);
        }

        private void IssueButton_Click(object sender, RoutedEventArgs e) // Issue a loan for the selected member and book
        {
            UserMessage.Clear(IssueReturnStatusText);

            if (MemberComboBox.SelectedItem is not PickerItem selectedMember)
            {
                ShowStatus(UserMessage.NothingSelected("member"), isError: true);
                return;
            }

            if (BookComboBox.SelectedItem is not PickerItem selectedBook)
            {
                ShowStatus(UserMessage.NothingSelected("book"), isError: true);
                return;
            }

            BorrowResult result;

            try
            {
                var service = new MemberActionsService(App.ConnectionString);
                result = service.BorrowBook(selectedBook.Id, selectedMember.Id);
            }
            catch (SqliteException ex) // DEF-11
            {
                Debug.WriteLine($"Issue loan failed - database error: {ex.Message}");
                UserMessage.Show(IssueReturnStatusText, MessageKind.Error, UserMessage.DatabaseUnavailable);
                return;
            }

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
            UserMessage.Clear(IssueReturnStatusText);

            if (LoanComboBox.SelectedItem is not PickerItem selectedLoan)
            {
                ShowStatus(UserMessage.NothingSelected("loan to return"), isError: true);
                return;
            }

            if (ConditionComboBox.SelectedItem is not ComboBoxItem selectedConditionItem)
            {
                ShowStatus(UserMessage.NothingSelected("return condition"), isError: true);
                return;
            }

            string condition = selectedConditionItem.Content?.ToString() ?? "Good";
            int loanId = selectedLoan.Id;

            try
            {
                using var db = new DatabaseHelper(App.ConnectionString);

                var loan = db.GetLoanById(loanId);
                if (loan == null || loan["ReturnDate"] != DBNull.Value) // Guard against a stale picker entry (already returned since the list was loaded)
                {
                    ShowStatus($"Loan #{loanId} could not be returned - it may have already been processed. Refreshing the list.", isError: true);
                    RefreshAllData();
                    return;
                }

                if (!db.ProcessReturn(loanId, DateTime.Now.Date, condition)) // Also updates the book's status internally (Available or Reserved)
                {
                    ShowStatus($"Loan #{loanId} could not be returned. It may already be returned or does not exist.", isError: true);
                    return;
                }
            }
            catch (SqliteException ex) // DEF-11
            {
                Debug.WriteLine($"Process return failed - database error: {ex.Message}");
                UserMessage.Show(IssueReturnStatusText, MessageKind.Error, UserMessage.DatabaseUnavailable);
                return;
            }

            ShowStatus($"Loan #{loanId} processed as returned ({condition} condition).", isError: false);
            RefreshAllData();
        }

        private void SuspendButton_Click(object sender, RoutedEventArgs e) // Suspend the selected member's account (REQ-19)
        {
            UserMessage.Clear(MemberActionStatusText);

            if (MembersListView.SelectedItem is not MemberRow selectedMember)
            {
                ShowMemberActionStatus(UserMessage.NothingSelected("member"), isError: true);
                return;
            }

            try
            {
                using var db = new DatabaseHelper(App.ConnectionString);

                // The return value was previously ignored, so a failed update still reported success.
                if (!db.SetAccountStatus(selectedMember.AccountId, "Suspended"))
                {
                    ShowMemberActionStatus($"{selectedMember.FullName} (ID {selectedMember.AccountId}) could not be suspended.", isError: true);
                    return;
                }
            }
            catch (SqliteException ex) // DEF-11
            {
                Debug.WriteLine($"Suspend failed - database error: {ex.Message}");
                UserMessage.Show(MemberActionStatusText, MessageKind.Error, UserMessage.DatabaseUnavailable);
                return;
            }

            ShowMemberActionStatus($"{selectedMember.FullName} (ID {selectedMember.AccountId}) has been suspended.", isError: false);
            RefreshAllData();
        }

        private void ActivateButton_Click(object sender, RoutedEventArgs e) // Activate a new account or reactivate a suspended one (REQ-16, REQ-19)
        {
            UserMessage.Clear(MemberActionStatusText);

            if (MembersListView.SelectedItem is not MemberRow selectedMember)
            {
                ShowMemberActionStatus(UserMessage.NothingSelected("member"), isError: true);
                return;
            }

            // Pending (REQ-16) and Suspended (REQ-19) both end as Active, so they are one action.
            // What matters to staff is the outcome, not which of the two states it started in.
            if (selectedMember.Status == "Active")
            {
                ShowMemberActionStatus($"{selectedMember.FullName} (ID {selectedMember.AccountId}) is already active.", isError: true);
                return;
            }

            if (selectedMember.Status == "Locked")
            {
                // Clearing a lockout must also reset the failed-attempt count, so it has its own
                // action rather than being folded in here (REQ-21).
                ShowMemberActionStatus($"{selectedMember.FullName} (ID {selectedMember.AccountId}) is locked - use Unlock Selected instead.", isError: true);
                return;
            }

            string previousStatus = selectedMember.Status;

            try
            {
                using var db = new DatabaseHelper(App.ConnectionString);

                if (!db.SetAccountStatus(selectedMember.AccountId, "Active"))
                {
                    ShowMemberActionStatus($"{selectedMember.FullName} (ID {selectedMember.AccountId}) could not be activated.", isError: true);
                    return;
                }
            }
            catch (SqliteException ex) // DEF-11
            {
                Debug.WriteLine($"Activate failed - database error: {ex.Message}");
                UserMessage.Show(MemberActionStatusText, MessageKind.Error, UserMessage.DatabaseUnavailable);
                return;
            }

            string outcome = previousStatus == "Pending"
                ? "has been activated and can now log in"
                : "has been reactivated";

            ShowMemberActionStatus($"{selectedMember.FullName} (ID {selectedMember.AccountId}) {outcome}.", isError: false);
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
                    Email = row["Email"]?.ToString() ?? "",
                    PhoneNumber = row["PhoneNumber"]?.ToString() ?? "",
                    Status = row["AccountStatus"]?.ToString() ?? ""
                })
                .OrderBy(member => member.AccountId)
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
            UserMessage.Clear(ReservationStatusText);

            if (!int.TryParse(ReservationIdBox.Text.Trim(), out int reservationId)) // Validate that the Reservation ID is numeric
            {
                ShowReservationStatus(UserMessage.FieldMustBeANumber("Reservation ID"), isError: true);
                return;
            }

            int? loanId;

            try
            {
                using var db = new DatabaseHelper(App.ConnectionString);
                loanId = db.FulfillReservation(reservationId, DateTime.Now.Date, LoanPeriodDays);
            }
            catch (SqliteException ex) // DEF-11
            {
                Debug.WriteLine($"Fulfil reservation failed - database error: {ex.Message}");
                UserMessage.Show(ReservationStatusText, MessageKind.Error, UserMessage.DatabaseUnavailable);
                return;
            }

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

        private void ShowStatus(string message, bool isError) // Issue / Return status line - formatting handled centrally by UserMessage (DEF-04)
        {
            UserMessage.Show(IssueReturnStatusText, isError ? MessageKind.Warning : MessageKind.Success, message);
        }

        private void ShowMemberActionStatus(string message, bool isError) // Suspend / Reactivate status line
        {
            UserMessage.Show(MemberActionStatusText, isError ? MessageKind.Warning : MessageKind.Success, message);
        }

        private void ShowReservationStatus(string message, bool isError) // Reservation fulfilment status line
        {
            UserMessage.Show(ReservationStatusText, isError ? MessageKind.Warning : MessageKind.Success, message);
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
            public string Email { get; set; } = "";
            public string PhoneNumber { get; set; } = "";
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
