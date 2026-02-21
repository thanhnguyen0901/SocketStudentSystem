using StudentClient.Wpf.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace StudentClient.Wpf.Views;

public partial class DbConnectView : UserControl
{
    public DbConnectView()
    {
        InitializeComponent();
    }

    // PasswordBox has no bindable Password DP; forward value manually.
    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is DbConnectViewModel vm)
            vm.Password = PasswordBox.Password;
    }
}
