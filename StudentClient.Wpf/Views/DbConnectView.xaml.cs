using StudentClient.Wpf.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace StudentClient.Wpf.Views;

public partial class DbConnectView : UserControl
{
    public DbConnectView()
    {
        InitializeComponent();

        // Subscribe to DataContextChanged so we can wire VM property-change events.
        DataContextChanged += OnDataContextChanged;
    }

    // Rewire PropertyChanged subscription whenever the DataContext is replaced.
    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is DbConnectViewModel oldVm)
            oldVm.PropertyChanged -= OnVmPropertyChanged;

        if (e.NewValue is DbConnectViewModel newVm)
            newVm.PropertyChanged += OnVmPropertyChanged;
    }

    // When the ViewModel sets FocusSqlHost=true, move keyboard focus to the SqlHost field
    // so the user can correct it without having to click.
    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DbConnectViewModel.FocusSqlHost)
            && sender is DbConnectViewModel vm
            && vm.FocusSqlHost)
        {
            SqlHost.Focus();
            SqlHost.SelectAll();
        }
    }

    // PasswordBox has no bindable Password DP; forward value manually.
    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is DbConnectViewModel vm)
            vm.Password = PasswordBox.Password;
    }
}
