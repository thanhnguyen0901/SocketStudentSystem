using StudentClient.Wpf.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace StudentClient.Wpf.Views;

public partial class ConnectionView : UserControl
{
    public ConnectionView()
    {
        InitializeComponent();
        // Wire up DataContext changes so focus triggers work after DI injection.
        DataContextChanged += OnDataContextChanged;
    }

    // Rewire PropertyChanged subscription whenever the DataContext is replaced.
    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ConnectionViewModel oldVm)
            oldVm.PropertyChanged -= OnVmPropertyChanged;

        if (e.NewValue is ConnectionViewModel newVm)
            newVm.PropertyChanged += OnVmPropertyChanged;
    }

    // React to FocusHost / FocusPort trigger properties set by the ViewModel.
    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not ConnectionViewModel vm) return;

        if (e.PropertyName == nameof(ConnectionViewModel.FocusHost) && vm.FocusHost)
        {
            Host.Focus();
            Host.SelectAll();
        }
        else if (e.PropertyName == nameof(ConnectionViewModel.FocusPort) && vm.FocusPort)
        {
            Port.Focus();
            Port.SelectAll();
        }
    }
}
