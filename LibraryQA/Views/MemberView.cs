using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LibraryQA.Core.Database;
using LibraryQA.Core.Services;
using LibraryQA.Models;
using Microsoft.Data.Sqlite;

namespace LibraryQA.Views
{
    // Member Interface, allowing members to search the catalogue, borrow/reserve books, and view their loans/reservations/history
    public partial class MemberView : UserControl
    {
        private readonly int _memberId;

        public event EventHandler? LogoutRequested;

        public MemberView(int memberId) // Initialise Member View
        {
            InitializeComponent();
            _memberId = memberId;
            Loaded += MemberView_Loaded;
        }

        private void MemberView_Loaded(object sender, RoutedEventArgs e) // Load all member related data when the view is loaded
        {
            // REQ-20: expire any reservations this member (or anyone) left uncollected for more than 5 days
            try
            {
                new ReservationExpiryService(App.ConnectionString).ExpireOverdueReservations();
            }
            catch (SqliteException ex) // DEF-11
            {
                Debug.WriteLine($"Could not expire reservations - database error: {ex.Message}");
            }

            LoadCatalogue();
            LoadMyLoans();
            LoadMyReservations();
            LoadLoanHistory();
            LoadRejectionNotices();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e) // Logout and return to the login interface
        {
            LogoutRequested?.Invoke(this, EventArgs.Empty);
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e) // Search the catalogue based on the search box input
        {
            LoadCatalogue(SearchBox.Text.Trim());
        }

        private void BorrowButton_Click(object sender, RoutedEventArgs e) // Borrow the selected book from the catalogue
        {
            if (CatalogueListView.SelectedItem is not BookDisplayModel selectedBook) // Check if a book is selected
            {
                MessageBox.Show("Please select a book to borrow.", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var service = new MemberActionsService(App.ConnectionString);
            var result = service.BorrowBook(selectedBook.BookID, _memberId);

            if (!result.Success) // Check if the borrow action was successful
            {
                MessageBox.Show(result.Message, "Borrow Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show(
                $"'{selectedBook.Title}' borrowed successfully. Due back {result.DueDate:d MMM yyyy}.",
                "Borrowed", MessageBoxButton.OK, MessageBoxImage.Information);

            LoadMyLoans();
            LoadCatalogue(SearchBox.Text.Trim()); // Refresh catalogue so status updates
        }

        private void ReserveButton_Click(object sender, RoutedEventArgs e) // Reserve the selected book from the catalogue
        {
            if (CatalogueListView.SelectedItem is not BookDisplayModel selectedBook) // Check if a book is selected
            {
                MessageBox.Show("Please select a book to reserve.", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var service = new MemberActionsService(App.ConnectionString);
            var result = service.ReserveBook(selectedBook.BookID, _memberId);

            if (!result.Success) // Check if the reserve action was successful
            {
                MessageBox.Show(result.Message, "Reserve Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show($"'{selectedBook.Title}' reserved successfully.",
                "Reserved", MessageBoxButton.OK, MessageBoxImage.Information);

            LoadMyReservations();
            LoadCatalogue(SearchBox.Text.Trim()); // Refresh catalogue so status updates
        }

        private void LoadMyLoans() // Load the member's active and pending loans and display them in the list view
        {
            using (var db = new DatabaseHelper(App.ConnectionString))
            {
                var rawLoans = db.GetActiveLoans(_memberId);

                var loans = rawLoans.Select(l => new MyLoanDisplayModel // Map the raw loan data to the display model
                {
                    LoanID = Convert.ToInt32(l["LoanID"]),
                    BookID = Convert.ToInt32(l["BookID"]),
                    Title = l["Title"]?.ToString() ?? "",
                    BorrowedOn = l["LoanDate"]?.ToString() ?? "",
                    DueDate = l["DueDate"]?.ToString() ?? "",
                    Status = (bool)l["IsOverdue"] ? "Overdue" : "On Loan"
                }).ToList();

                // REQ-18: pending loan requests are shown too, so the member can see they are awaiting approval
                var pendingLoans = db.GetPendingLoans()
                    .Where(l => Convert.ToInt32(l["MemberID"]) == _memberId)
                    .Select(l => new MyLoanDisplayModel
                    {
                        LoanID = Convert.ToInt32(l["LoanID"]),
                        BookID = Convert.ToInt32(l["BookID"]),
                        Title = l["Title"]?.ToString() ?? "",
                        BorrowedOn = l["LoanDate"]?.ToString() ?? "",
                        DueDate = l["DueDate"]?.ToString() ?? "",
                        Status = "Pending"
                    });

                MyLoansListView.ItemsSource = loans.Concat(pendingLoans).ToList();
            }
        }

        private void LoadMyReservations() // Load the member's active reservations and display them in the list view
        {
            using (var db = new DatabaseHelper(App.ConnectionString))
            {
                var rawReservations = db.GetActiveReservations(_memberId);

                var reservations = rawReservations.Select(r => new MyReservationDisplayModel // Map the raw reservation data to the display model
                {
                    ReservationID = Convert.ToInt32(r["ReservationID"]),
                    BookID = Convert.ToInt32(r["BookID"]),
                    Title = r["Title"]?.ToString() ?? "",
                    ReservedOn = r["ReservationDate"]?.ToString() ?? "",
                    Status = r["Status"]?.ToString() ?? ""
                }).ToList();

                MyReservationsListView.ItemsSource = reservations;
            }
        }

        private void LoadLoanHistory() // Load the member's loan history and display it in the list view
        {
            using (var db = new DatabaseHelper(App.ConnectionString))
            {
                var rawHistory = db.GetLoanHistory(_memberId);

                var history = rawHistory.Select(h => new LoanHistoryDisplayModel // Map the raw loan history data to the display model
                {
                    LoanID = Convert.ToInt32(h["LoanID"]),
                    BookID = Convert.ToInt32(h["BookID"]),
                    Title = h["Title"]?.ToString() ?? "",
                    BorrowedOn = h["LoanDate"]?.ToString() ?? "",
                    DueDate = h["DueDate"]?.ToString() ?? "",
                    ReturnedOn = h["ReturnDate"]?.ToString() ?? "",
                    Condition = h["ReturnCondition"]?.ToString() ?? ""
                }).ToList();

                LoanHistoryListView.ItemsSource = history;
            }
        }

        private void LoadRejectionNotices() // REQ-18: Informs the member when a loan or reservation request of theirs has been rejected
        {
            try
            {
                using (var db = new DatabaseHelper(App.ConnectionString))
                {
                    var rejected = db.GetRejectedNotices(_memberId);
                    if (rejected.Count == 0)
                    {
                        return;
                    }

                    string summary = string.Join("; ", rejected.Select(r =>
                        $"your {r["RequestType"]} request for '{r["Title"]}' was rejected"));

                    UserMessage.Show(StatusText, MessageKind.Warning, $"Staff have reviewed your requests: {summary}.");
                }
            }
            catch (SqliteException ex) // DEF-11
            {
                Debug.WriteLine($"Could not load rejection notices - database error: {ex.Message}");
            }
        }

        private void LoadCatalogue(string query = "") // Load the catalogue based on the search query and display it in the list view
        {
            using (var db = new DatabaseHelper(App.ConnectionString))
            {
                var rawResults = db.SearchCatalogue(query); // Search the catalogue using the provided query

                CatalogueListView.ItemsSource = rawResults.Select(r => new BookDisplayModel // Map the raw catalogue data to the display model
                {
                    BookID = Convert.ToInt32(r["BookID"]),
                    Title = r["Title"]?.ToString() ?? "",
                    Author = r["Author"]?.ToString() ?? "",
                    Status = r["Status"]?.ToString() ?? ""
                }).ToList();
            }
        }
    }
}