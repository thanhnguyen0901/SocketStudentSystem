using Caliburn.Micro;
using StudentClient.Wpf.Services;

namespace StudentClient.Wpf.ViewModels;

public sealed class ConnectionViewModel : Screen
{
    private readonly TcpStudentService _service;
    private readonly ShellViewModel _shell;

    private string _host = "127.0.0.1";
    private string _port = "9000";
    private string _status = "Enter server address and click Connect.";
    private bool _isBusy;

    public ConnectionViewModel(TcpStudentService service, ShellViewModel shell)
    {
        _service = service;
        _shell = shell;
        DisplayName = "Connect to Server";
    }

    public string Host
    {
        get => _host;
        set
        {
            _host = value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(CanConnect));
        }
    }

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

    public string Status
    {
        get => _status;
        set { _status = value; NotifyOfPropertyChange(); }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(CanConnect));
        }
    }

    public bool CanConnect
        => !IsBusy
        && !string.IsNullOrWhiteSpace(Host)
        && int.TryParse(Port, out int p) && p is > 0 and <= 65535;

    public async Task Connect()
    {
        IsBusy = true;
        Status = $"Connecting to {Host}:{Port}...";

        try
        {
            int port = int.Parse(Port);
            await _service.ConnectAsync(Host, port);

            Status = $"Connected to {Host}:{Port}.";
            await _shell.ShowDbConnectAsync();
        }
        catch (Exception ex)
        {
            Status = $"Connection failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
