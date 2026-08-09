using System;
using System.Windows;
using System.Windows.Controls;
using LibraryQA.Core.Models;

namespace LibraryQA.Views
{
    public partial class LoginView : UserControl
    {
        //MainWindow connects to this to know when to swap to Member/Staff view
        public event EventHandler<UserRole>? LoginSucceeded;

        public LoginView()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text.Trim();
            string password = PasswordBox.Password;

            //TODO: replace with real validation against the Members/Staff tables
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Please enter both a username and password.");
                return;
            }

            var selectedItem = (ComboBoxItem)RoleComboBox.SelectedItem;
            var role = selectedItem.Content.ToString() == "Staff" ? UserRole.Staff : UserRole.Member;

            LoginSucceeded?.Invoke(this, role);
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
