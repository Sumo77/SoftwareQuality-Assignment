using System.Windows;
using LibraryQA.Core.Models;
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
            if (role == UserRole.Staff)
            {
                var staffView = new StaffView();
                staffView.LogoutRequested += (_, _) => ShowLogin();

                RootGrid.Children.Clear();
                RootGrid.Children.Add(staffView);
            }
            else
            {
                var memberView = new MemberView();
                memberView.LogoutRequested += (_, _) => ShowLogin();

                RootGrid.Children.Clear();
                RootGrid.Children.Add(memberView);
            }
        }
    }
}