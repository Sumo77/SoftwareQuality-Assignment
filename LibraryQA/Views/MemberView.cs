using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LibraryQA.Core.Database;
using LibraryQA.Core.Services;
using LibraryQA.Models;

namespace LibraryQA.Views
{
    public partial class MemberView : UserControl
    {
        private readonly int _memberId;

        public event EventHandler? LogoutRequested;

        public MemberView(int memberId)
        {
            InitializeComponent();
            _memberId = memberId;
            Loaded += MemberView_Loaded;
        }

        private void MemberView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCatalogue();
            LoadMyLoans();
            LoadMyReservations();
            LoadLoanHistory(); // REQ-5: members must be able to view their loan history
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            LogoutRequested?.Invoke(this, EventArgs.Empty);
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            LoadCatalogue(SearchBox.Text.Trim());
        }

        private void BorrowButton_Click(object sender, RoutedEventArgs e)
        {
            if (CatalogueListView.SelectedItem is not BookDisplayModel selectedBook)
            {
                MessageBox.Show("Please select a book to borrow.", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var service = new MemberActionsService(App.ConnectionString);
            var result = service.BorrowBook(selectedBook.BookID, _memberId);

            if (!result.Success)
            {
                MessageBox.Show(result.Message, "Borrow Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show(
                $"'{selectedBook.Title}' borrowed successfully. Due back {result.DueDate:d MMM yyyy}.",
                "Borrowed", MessageBoxButton.OK, MessageBoxImage.Information);

            LoadMyLoans();
            SearchButton_Click(sender, e); // refresh catalogue so status updates
        }

        private void ReserveButton_Click(object sender, RoutedEventArgs e)
        {
            if (CatalogueListView.SelectedItem is not BookDisplayModel selectedBook)
            {
                MessageBox.Show("Please select a book to reserve.", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var service = new MemberActionsService(App.ConnectionString);
            var result = service.ReserveBook(selectedBook.BookID, _memberId);

            if (!result.Success)
            {
                MessageBox.Show(result.Message, "Reserve Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show($"'{selectedBook.Title}' reserved successfully.",
                "Reserved", MessageBoxButton.OK, MessageBoxImage.Information);

            LoadMyReservations();
            SearchButton_Click(sender, e);
        }

        private void LoadMyLoans()
        {
            using (var db = new DatabaseHelper(App.ConnectionString))
            {
                var rawLoans = db.GetActiveLoans(_memberId);

                var loans = rawLoans.Select(l => new MyLoanDisplayModel
                {
                    LoanID = Convert.ToInt32(l["LoanID"]),
                    BookID = Convert.ToInt32(l["BookID"]),
                    Title = l["Title"]?.ToString() ?? "",
                    BorrowedOn = l["LoanDate"]?.ToString() ?? "",
                    DueDate = l["DueDate"]?.ToString() ?? "",
                    Status = (bool)l["IsOverdue"] ? "Overdue" : "On Loan"
                }).ToList();

                MyLoansListView.ItemsSource = loans;
            }
        }

        private void LoadMyReservations()
        {
            using (var db = new DatabaseHelper(App.ConnectionString))
            {
                var rawReservations = db.GetActiveReservations(_memberId);

                var reservations = rawReservations.Select(r => new MyReservationDisplayModel
                {
                    ReservationID = Convert.ToInt32(r["ReservationID"]),
                    BookID = Convert.ToInt32(r["BookID"]),
                    Title = r["Title"]?.ToString() ?? "",
                    ReservedOn = r["ReservationDate"]?.ToString() ?? ""
                }).ToList();

                MyReservationsListView.ItemsSource = reservations;
            }
        }

        private void LoadLoanHistory()
        {
            using (var db = new DatabaseHelper(App.ConnectionString))
            {
                var rawHistory = db.GetLoanHistory(_memberId);

                var history = rawHistory.Select(h => new LoanHistoryDisplayModel
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

        private void LoadCatalogue(string query = "")
        {
            using (var db = new DatabaseHelper(App.ConnectionString))
            {
                var rawResults = db.SearchCatalogue(query);

                CatalogueListView.ItemsSource = rawResults.Select(r => new BookDisplayModel
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