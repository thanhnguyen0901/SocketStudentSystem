using Caliburn.Micro;
using StudentClient.Wpf.Services;
using System.Windows;

namespace StudentClient.Wpf.ViewModels;

public sealed class ConnectionViewModel : Screen
{
    private readonly TcpStudentService _service;
    private readonly ShellViewModel _shell;

    private string _host = "localhost";
    private string _port = "9000";
    private string _status = "Chưa kết nối.";
    private bool _isBusy;

    public ConnectionViewModel(TcpStudentService service, ShellViewModel shell)
    {
        _service = service;
        _shell = shell;
        DisplayName = "Kết nối máy chủ";
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
        set
        {
            _status = value;
            NotifyOfPropertyChange();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(CanConnect));
            NotifyOfPropertyChange(nameof(AreInputsEnabled));
        }
    }

    public bool AreInputsEnabled => !IsBusy;

    private bool _focusHost;
    public bool FocusHost
    {
        get => _focusHost;
        set
        {
            _focusHost = value;
            NotifyOfPropertyChange();
        }
    }

    private bool _focusPort;
    public bool FocusPort
    {
        get => _focusPort;
        set
        {
            _focusPort = value;
            NotifyOfPropertyChange();
        }
    }

    public bool CanConnect
        => !IsBusy
        && !string.IsNullOrWhiteSpace(Host)
        && int.TryParse(Port, out int p)
        && p is > 0 and <= 65535;

    public async Task Connect()
    {
        if (string.IsNullOrWhiteSpace(Host))
        {
            MessageBox.Show(
                "Vui lòng nhập địa chỉ máy chủ hoặc IP.",
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
                "Cổng không hợp lệ. Vui lòng nhập số nguyên trong khoảng 1–65535.",
                "Cổng không hợp lệ",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            FocusPort = true;
            FocusPort = false;
            return;
        }

        IsBusy = true;
        Status = $"Đang kết nối tới {Host}:{port}...";

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _service.ConnectAsync(Host, port, cts.Token);

            Status = $"Đã kết nối tới {Host}:{port}.";
            await _shell.ShowDbConnectAsync();
        }
        catch (OperationCanceledException)
        {
            Status = "Kết nối thất bại: đã hết thời gian chờ sau 5 giây.";

            MessageBox.Show(
                "Kết nối tới máy chủ đã hết thời gian chờ sau 5 giây.\n\nVui lòng kiểm tra lại server và thử lại.",
                "Hết thời gian chờ",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            FocusHost = true;
            FocusHost = false;
        }
        catch (Exception ex)
        {
            Status = $"Kết nối thất bại: {ex.Message}";

            MessageBox.Show(
                $"Không thể kết nối tới máy chủ:\n{ex.Message}\n\nVui lòng kiểm tra lại địa chỉ và cổng.",
                "Lỗi kết nối máy chủ",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            FocusHost = true;
            FocusHost = false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void HandleDisconnected(string? message)
    {
        IsBusy = false;
        Status = string.IsNullOrWhiteSpace(message) ? "Đã ngắt kết nối." : message;
    }
}
