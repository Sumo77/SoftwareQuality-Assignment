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
            //change later 
            RootGrid.Children.Add(new MemberView(1));
        }

        // TODO: unused while testing MemberView with a hardcoded id above.
        // Uncomment / fix once real login wiring (with a real memberId) is ready.
        //
        // private void ShowLogin()
        // {
        //     var loginView = new LoginView();
        //     loginView.LoginSucceeded += LoginView_LoginSucceeded;
        //
        //     RootGrid.Children.Clear();
        //     RootGrid.Children.Add(loginView);
        // }
        //
        // private void LoginView_LoginSucceeded(object? sender, UserRole role)
        // {
        //     if (role == UserRole.Staff)
        //     {
        //         var staffView = new StaffView();
        //         staffView.LogoutRequested += (_, _) => ShowLogin();
        //
        //         RootGrid.Children.Clear();
        //         RootGrid.Children.Add(staffView);
        //     }
        //     else
        //     {
        //         var memberView = new MemberView(1 /* TODO: real memberId */);
        //         memberView.LogoutRequested += (_, _) => ShowLogin(); 
        //
        //         RootGrid.Children.Clear();
        //         RootGrid.Children.Add(memberView);
        //     }
        // }
    }
}