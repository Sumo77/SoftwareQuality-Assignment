using System;
using System.Windows;
using System.Windows.Controls;

namespace LibraryQA.Views
{
    public partial class MemberView : UserControl
    {
        //MainWindow connects to this to return to LoginView
        public event EventHandler? LogoutRequested;

        public MemberView()
        {
            InitializeComponent();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            LogoutRequested?.Invoke(this, EventArgs.Empty);
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            //TODO: query the catalogue via the data layer and bind results to CatalogueListView.ItemsSource
        }

        private void BorrowButton_Click(object sender, RoutedEventArgs e)
        {
            //TODO: call the loan service for the item selected in CatalogueListView. Should reject the action if the item is already on loan
        }

        private void ReserveButton_Click(object sender, RoutedEventArgs e)
        {
            //TODO: call the reservation service for the item selected in CatalogueListView. Should reject duplicate reservations by the same member
        }
    }
}