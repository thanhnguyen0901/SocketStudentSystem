using Caliburn.Micro;

namespace StudentClient.Wpf.ViewModels;

/// <summary>
/// Root view-model for the application shell.
/// Implements <see cref="Screen"/> so Caliburn manages the window lifecycle
/// (OnActivate / OnDeactivate / TryCloseAsync).
/// </summary>
public class ShellViewModel : Screen
{
    // -------------------------------------------------------------------------
    // Backing fields
    // -------------------------------------------------------------------------

    private string _host = "127.0.0.1";
    private string _port = "5000";
    private string _status = "Disconnected";

    // -------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------

    public ShellViewModel()
    {
        // DisplayName drives the window title via Caliburn's WindowManager.
        DisplayName = "Socket Student System";
    }

    // -------------------------------------------------------------------------
    // Bindable properties (x:Name convention in ShellView.xaml)
    // -------------------------------------------------------------------------

    /// <summary>Server hostname or IP address.</summary>
    public string Host
    {
        get => _host;
        set
        {
            _host = value;
            NotifyOfPropertyChange();           // raises PropertyChanged for "Host"
            NotifyOfPropertyChange(nameof(CanConnect)); // re-evaluate guard
        }
    }

    /// <summary>Server TCP port.</summary>
    public string Port
    {
        get => _port;
        set
        {
            _port = value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(CanConnect));
        }
    }

    /// <summary>Human-readable connection status shown in the UI.</summary>
    public string Status
    {
        get => _status;
        set
        {
            _status = value;
            NotifyOfPropertyChange();
        }
    }

    // -------------------------------------------------------------------------
    // Actions (bound to buttons via x:Name Caliburn convention)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Guard method for the Connect action.
    /// The Connect button is enabled only when both Host and Port are non-empty.
    /// Caliburn automatically evaluates CanConnect whenever Host or Port change.
    /// </summary>
    public bool CanConnect
        => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(Port);

    /// <summary>
    /// Placeholder connection handler.
    /// Real socket logic will be added in a later iteration.
    /// </summary>
    public void Connect()
    {
        // TODO: inject and call IConnectionService once socket layer is implemented.
        Status = $"Connecting to {Host}:{Port}…";
    }
}
