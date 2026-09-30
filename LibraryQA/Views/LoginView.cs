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
            UserMessage.Clear(ErrorText); // Prevents stale errors from previous attempts

            string username = UsernameBox.Text.Trim(); // Retrieve user input (from username and password boxes)
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username)) // Check username for blank input
            {
                UserMessage.Show(ErrorText, MessageKind.Warning, UserMessage.FieldIsRequired("username"));
                UsernameBox.Focus(); // Focuses on the username box for user convenience
                return;
            }

            if (string.IsNullOrWhiteSpace(password)) // Check password for blank input
            {
                UserMessage.Show(ErrorText, MessageKind.Warning, UserMessage.FieldIsRequired("password"));
                PasswordBox.Focus(); // Focuses on the password box for user convenience
                return;
            }
            if (_authService.IsAccountSuspended(username))
            {
                UserMessage.Show(ErrorText, MessageKind.Error, "This account has been suspended. Please contact library staff.");
                PasswordBox.Clear();
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
                UserMessage.Show(ErrorText, MessageKind.Error, UserMessage.DatabaseUnavailable);
                return;
            }

            if (role == null)
            {
                UserMessage.Show(ErrorText, MessageKind.Error, UserMessage.InvalidCredentials);
                PasswordBox.Clear(); // Remove invalid input from password box for user convenience
                return;
            }

            LoginSucceeded?.Invoke(this, role.Value);
        }

    }
}
