using System.Windows;

namespace StudentClient.Wpf.Views;

/// <summary>
/// Code-behind for ShellView.
/// Caliburn.Micro sets the DataContext automatically via naming convention;
/// no manual wiring is needed here.
/// </summary>
public partial class ShellView : Window
{
    public ShellView()
    {
        InitializeComponent();
    }
}
