using System.Windows;
using LibraryQA.Core.Models;
using LibraryQA.Core.Services;
using LibraryQA.Views;

namespace LibraryQA
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ShowLogin();
        }

        private void ShowLogin()
        {
            var loginView = new LoginView();
            loginView.LoginSucceeded += LoginView_LoginSucceeded;

            RootGrid.Children.Clear();
            RootGrid.Children.Add(loginView);
        }

        private void LoginView_LoginSucceeded(object? sender, UserRole role)
        {
            var loginView = sender as LoginView;
            string username = loginView?.UsernameBox.Text.Trim() ?? "";

            var authService = new AuthenticationService(App.ConnectionString);
            int? accountId = authService.GetAccountId(username);

            if (accountId == null)
            {
                MessageBox.Show("Could not resolve the logged-in account. Please try again.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (role == UserRole.Staff)
            {
                var staffView = new StaffView();
                staffView.LogoutRequested += (_, _) => ShowLogin();

                RootGrid.Children.Clear();
                RootGrid.Children.Add(staffView);
            }
            else
            {
                var memberView = new MemberView(accountId.Value);
                memberView.LogoutRequested += (_, _) => ShowLogin();

                RootGrid.Children.Clear();
                RootGrid.Children.Add(memberView);
            }
        }
    }
}