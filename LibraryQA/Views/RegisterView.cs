using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using LibraryQA.Core.Services;
using Microsoft.Data.Sqlite;

namespace LibraryQA.Views
{
    // Registration Interface (REQ-16). A prospective member supplies their details, the account is
    // created with the status Pending, and library staff activate it from the Members tab before
    // it can be used to log in.
    public partial class RegisterView : UserControl
    {
        // MainWindow listens to this to return to the login screen.
        public event EventHandler? BackToLoginRequested;

        private readonly RegistrationService _registrationService;

        public RegisterView()
        {
            InitializeComponent();
            _registrationService = new RegistrationService(App.ConnectionString);
            Loaded += (_, _) => FirstNameBox.Focus();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            BackToLoginRequested?.Invoke(this, EventArgs.Empty);
        }

        private void CreateAccountButton_Click(object sender, RoutedEventArgs e)
        {
            UserMessage.Clear(StatusText); // Prevents a stale message from a previous attempt

            // The two passwords are compared here because only this screen knows there are two of
            // them. Every other rule belongs to RegistrationService, so it applies to any caller.
            if (PasswordBox.Password != ConfirmPasswordBox.Password)
            {
                UserMessage.Show(StatusText, MessageKind.Warning, "The two passwords do not match.");
                ConfirmPasswordBox.Clear();
                ConfirmPasswordBox.Focus();
                return;
            }

            RegistrationResult result;

            try
            {
                result = _registrationService.Register(
                    UsernameBox.Text,
                    PasswordBox.Password,
                    FirstNameBox.Text,
                    LastNameBox.Text,
                    EmailBox.Text,
                    PhoneBox.Text);
            }
            catch (SqliteException ex) // DEF-11: a database failure must not crash the application
            {
                Debug.WriteLine($"Registration failed - database error: {ex.Message}");
                UserMessage.Show(StatusText, MessageKind.Error, UserMessage.DatabaseUnavailable);
                return;
            }

            if (!result.Success)
            {
                UserMessage.Show(StatusText, MessageKind.Warning, result.Message);
                return;
            }

            // The password boxes are cleared so the credentials are not left on screen.
            PasswordBox.Clear();
            ConfirmPasswordBox.Clear();
            CreateAccountButton.IsEnabled = false;

            UserMessage.Show(StatusText, MessageKind.Success, result.Message);
        }

        private void UsernameBox_LostFocus(object sender, RoutedEventArgs e) // REQ-16: flag a taken username before the form is submitted
        {
            string username = UsernameBox.Text.Trim();

            if (username.Length == 0)
            {
                return;
            }

            try
            {
                if (_registrationService.IsUsernameAvailable(username))
                {
                    UserMessage.Clear(StatusText);
                }
                else
                {
                    UserMessage.Show(StatusText, MessageKind.Warning, "That username is already taken. Please choose another.");
                }
            }
            catch (SqliteException ex) // DEF-11
            {
                Debug.WriteLine($"Username check failed - database error: {ex.Message}");
            }
        }
    }
}
