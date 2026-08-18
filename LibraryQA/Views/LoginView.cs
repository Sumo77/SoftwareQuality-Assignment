using System;
using System.Windows;
using System.Windows.Controls;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Input;
using LibraryQA.Core.Models;
using LibraryQA.Core.Database;
using Microsoft.Data.Sqlite;


namespace LibraryQA.Views
{
    public partial class LoginView : UserControl
    {
        // MainWindow connects to this to know when to swap to Member/Staff view
        public event EventHandler<UserRole>? LoginSucceeded;

        public LoginView()
        {
            InitializeComponent();
            Loaded += (_, _) => UsernameBox.Focus(); // Cosmetic - puts the cursor in the username box when the view loads
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            ClearError(); // Prevents stale errors from previous attempts
            
            string username = UsernameBox.Text.Trim(); // Retrieve user input (from username and password boxes)
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) // Check for blank input
            {
                ShowError("Please enter both a username and password.");
                return;
            }

            string? role;

            try
            {
                role = LookUpRole(username, password);
            }
            catch (SqliteException ex)
            {
                ShowError("Could not reach the library database. Please try again.");
                Console.WriteLine($"Login error: {ex.Message}");
                return;
            }

            if (role == null)
            {
                ShowError("Invalid username or password.");
                PasswordBox.Clear();
                return;
            }

            UserRole userRole = UserRole.Member;

            if (role == "Staff")
            {
                userRole = UserRole.Staff;
            }

            LoginSucceeded?.Invoke(this, userRole);
        }

        private void ShowError(string message) // Display error for user feedback
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }

        private void ClearError() // Prevent stale errors from previous attempts by 'cleaning the slate'
        {
            ErrorText.Text = string.Empty;
            ErrorText.Visibility = Visibility.Collapsed;
        }

        private static string? LookUpRole(string username, string password) // Look up the user's role in the database
        {
            using (var db = new DatabaseHelper(App.ConnectionString))
            {
                int? accountId = db.ValidateLogin(username, HashPassword(password));

                return accountId == null ? null : db.GetAccountRole(accountId.Value);
            }
        }

        private static string HashPassword(string password) // Hash the password using SHA256 for secure comparison with stored hashes
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
