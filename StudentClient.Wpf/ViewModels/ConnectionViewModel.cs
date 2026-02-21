using Caliburn.Micro;
using StudentClient.Wpf.Services;
using System.Windows;

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

    // Momentary focus triggers: the View watches these and calls Focus()/SelectAll()
    // on the corresponding TextBox, then the ViewModel resets them to false.
    private bool _focusHost;
    public bool FocusHost
    {
        get => _focusHost;
        set { _focusHost = value; NotifyOfPropertyChange(); }
    }

    private bool _focusPort;
    public bool FocusPort
    {
        get => _focusPort;
        set { _focusPort = value; NotifyOfPropertyChange(); }
    }

    public bool CanConnect
        => !IsBusy
        && !string.IsNullOrWhiteSpace(Host)
        && int.TryParse(Port, out int p) && p is > 0 and <= 65535;

    public async Task Connect()
    {
        // Defensive pre-validation (CanConnect normally guards the button, but
        // these checks ensure we never proceed with invalid inputs).
        if (string.IsNullOrWhiteSpace(Host))
        {
            MessageBox.Show(
                "Vui lòng nhập Host / IP của server.",
                "Thiếu thông tin",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            FocusHost = true;
            FocusHost = false;
            return;
        }

        if (!int.TryParse(Port, out int port) || port is <= 0 or > 65535)
        {
            MessageBox.Show(
                "Port không hợp lệ. Vui lòng nhập số nguyên trong khoảng 1–65535.",
                "Port không hợp lệ",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            FocusPort = true;
            FocusPort = false;
            return;
        }

        IsBusy = true;
        Status = $"Connecting to {Host}:{port}...";

        try
        {
            await _service.ConnectAsync(Host, port);

            Status = $"Connected to {Host}:{port}.";
            await _shell.ShowDbConnectAsync();
        }
        catch (Exception ex)
        {
            Status = $"Connection failed: {ex.Message}";

            MessageBox.Show(
                $"Kết nối server thất bại:\n{ex.Message}\n\nVui lòng kiểm tra lại Host/Port.",
                "Lỗi kết nối Server",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            // Return focus to Host so the user can correct the address.
            FocusHost = true;
            FocusHost = false;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
