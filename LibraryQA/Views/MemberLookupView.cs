using LibraryQA.Core.Database;
using LibraryQA.Core.Services;
using Microsoft.Data.Sqlite;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LibraryQA.Views
{
    // Member Lookup, allowing staff to see one member's loans, overdue items and reservations (REQ-6)
    public partial class MemberLookupView : UserControl
    {
        public MemberLookupView() // Initialise Member Lookup View
        {
            InitializeComponent();
        }

        private void MemberLookupView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) // Reload the member list each time the tab is shown, so new members appear
        {
            if (e.NewValue is true)
            {
                LoadMembers();
            }
        }

        private void LoadMembers() // Populates the picker with every member, including suspended ones, keeping the current selection
        {
            try
            {
                int? selectedId = (MemberComboBox.SelectedItem as PickerItem)?.Id;

                using var db = new DatabaseHelper(App.ConnectionString);

                var members = db.GetAllMembers()
                    .Select(row => new PickerItem
                    {
                        Id = Convert.ToInt32(row["AccountID"]),
                        Display = $"{row["AccountID"]} - {row["FirstName"]} {row["LastName"]}"
                    })
                    .ToList();

                MemberComboBox.ItemsSource = members;
                MemberComboBox.SelectedItem = members.FirstOrDefault(member => member.Id == selectedId);
            }
            catch (SqliteException)
            {
                UserMessage.Show(StatusText, MessageKind.Error, UserMessage.DatabaseUnavailable);
            }
        }

        private void LookUpButton_Click(object sender, RoutedEventArgs e) // Look up the selected member
        {
            UserMessage.Clear(StatusText);

            if (MemberComboBox.SelectedItem is not PickerItem selected)
            {
                ResultPanel.Visibility = Visibility.Collapsed;
                UserMessage.Show(StatusText, MessageKind.Warning, UserMessage.NothingSelected("member"));
                return;
            }

            MemberLookupResult result;

            try
            {
                result = new MemberLookupService(App.ConnectionString).LookUpMember(selected.Id);
            }
            catch (SqliteException)
            {
                ResultPanel.Visibility = Visibility.Collapsed;
                UserMessage.Show(StatusText, MessageKind.Error, UserMessage.DatabaseUnavailable);
                return;
            }

            if (!result.Success)
            {
                ResultPanel.Visibility = Visibility.Collapsed;
                UserMessage.Show(StatusText, MessageKind.Warning, result.Message);
                return;
            }

            ShowResult(result);
        }

        private void ShowResult(MemberLookupResult result) // Display the lookup result
        {
            NameText.Text = result.FullName;
            DetailText.Text = $"Member ID {result.MemberId}  |  Account status: {result.AccountStatus}";

            if (result.Email != null || result.PhoneNumber != null)
            {
                string email = string.IsNullOrWhiteSpace(result.Email) ? "no email on file" : result.Email;
                string phone = string.IsNullOrWhiteSpace(result.PhoneNumber) ? "no phone on file" : result.PhoneNumber;
                ContactText.Text = $"Contact for overdue follow-up: {email}  |  {phone}";
            }
            else
            {
                ContactText.Text = "Contact details are only shown while this member has overdue items.";
            }

            LoansHeading.Text = $"Current loans ({result.ActiveLoans.Count})";
            LoansListView.ItemsSource = result.ActiveLoans
                .Select(loan => new LoanRow
                {
                    LoanId = Convert.ToInt32(loan["LoanID"]),
                    Title = loan["Title"]?.ToString() ?? "",
                    BorrowedOn = loan["LoanDate"]?.ToString() ?? "",
                    DueDate = loan["DueDate"]?.ToString() ?? "",
                    Status = Convert.ToBoolean(loan["IsOverdue"]) ? "Overdue" : "On loan"
                })
                .ToList();

            OverdueHeading.Text = $"Overdue items ({result.OverdueLoans.Count})";
            OverdueListView.ItemsSource = result.OverdueLoans
                .Select(loan => new OverdueRow
                {
                    LoanId = Convert.ToInt32(loan["LoanID"]),
                    Title = loan["Title"]?.ToString() ?? "",
                    DueDate = loan["DueDate"]?.ToString() ?? "",
                    DaysOverdue = Convert.ToInt32(loan["DaysOverdue"])
                })
                .ToList();

            ReservationsHeading.Text = $"Active reservations ({result.Reservations.Count})";
            ReservationsListView.ItemsSource = result.Reservations
                .Select(reservation => new ReservationRow
                {
                    ReservationId = Convert.ToInt32(reservation["ReservationID"]),
                    Title = reservation["Title"]?.ToString() ?? "",
                    ReservedOn = reservation["ReservationDate"]?.ToString() ?? ""
                })
                .ToList();

            ResultPanel.Visibility = Visibility.Visible;
        }

        private class PickerItem // Represents a member choice in the picker - ComboBox shows ToString() by default
        {
            public int Id { get; set; }
            public string Display { get; set; } = "";
            public override string ToString() => Display;
        }

        private class LoanRow // Represents one of the member's active loans for display in the staff interface
        {
            public int LoanId { get; set; }
            public string Title { get; set; } = "";
            public string BorrowedOn { get; set; } = "";
            public string DueDate { get; set; } = "";
            public string Status { get; set; } = "";
        }

        private class OverdueRow // Represents one of the member's overdue items for display in the staff interface
        {
            public int LoanId { get; set; }
            public string Title { get; set; } = "";
            public string DueDate { get; set; } = "";
            public int DaysOverdue { get; set; }
        }

        private class ReservationRow // Represents one of the member's active reservations for display in the staff interface
        {
            public int ReservationId { get; set; }
            public string Title { get; set; } = "";
            public string ReservedOn { get; set; } = "";
        }
    }
}