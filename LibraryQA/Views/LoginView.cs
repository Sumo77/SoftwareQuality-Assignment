using LibraryQA.Core.Models;
using LibraryQA.Core.Services;
using Microsoft.Data.Sqlite;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;



namespace LibraryQA.Views
{
    // Login Interface, directing users to either the Member or Staff view based on their sign-in role
    public partial class LoginView : UserControl
    {
        // MainWindow connects to this to know when to swap to Member/Staff view
        public event EventHandler<UserRole>? LoginSucceeded;

        private readonly AuthenticationService _authService;

        public LoginView()
        {
            InitializeComponent();
            _authService = new AuthenticationService(App.ConnectionString);
            Loaded += (_, _) => UsernameBox.Focus(); // Cosmetic - puts the cursor in the username box when the view loads
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            ClearError(); // Prevents stale errors from previous attempts
            
            string username = UsernameBox.Text.Trim(); // Retrieve user input (from username and password boxes)
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username)) // Check username for blank input
            {
                ShowError("Please enter a username.");
                UsernameBox.Focus(); // Focuses on the username box for user convenience
                return;
            }

            if (string.IsNullOrWhiteSpace(password)) // Check password for blank input
            {
                ShowError("Please enter a password.");
                PasswordBox.Focus(); // Focuses on the password box for user convenience
                return;
            }

            UserRole? role;

            try
            {
                role = _authService.Authenticate(username, password); // Authenticate user credentials against the database and retrieve their role
            }
            catch (SqliteException ex)
            {
                Debug.WriteLine($"Login failed - database error: {ex.Message}");
                ShowError("Could not reach the library database. Please try again.");
                return;
            }

            if (role == null)
            {
                ShowError("Invalid username or password.");
                PasswordBox.Clear(); // Remove invalid input from password box for user convenience
                return;
            }

            LoginSucceeded?.Invoke(this, role.Value);
        }

        private void ShowError(string message) // Display error for user feedback
        {
            ErrorText.Text = $"⚠ {message}";
            ErrorText.Visibility = Visibility.Visible;
        }

        private void ClearError() // Prevent stale errors by 'cleaning the slate'
        {
            ErrorText.Text = string.Empty;
            ErrorText.Visibility = Visibility.Collapsed;
        }

    }
}
