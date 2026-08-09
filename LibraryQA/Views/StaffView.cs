using System;
using System.Windows;
using System.Windows.Controls;

namespace LibraryQA.Views
{
    public partial class StaffView : UserControl
    {
        //MainWindow subscribes to this to return to LoginView.
        public event EventHandler? LogoutRequested;

        public StaffView()
        {
            InitializeComponent();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            LogoutRequested?.Invoke(this, EventArgs.Empty);
        }

        private void IssueButton_Click(object sender, RoutedEventArgs e)
        {
            //TODO: call the loan service to issue MemberIdBox's book to BookIdBox's member. Reject if the item is already on loan
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: call the loan service to process a return, updating the catalogue status back to Available
        }
    }
}