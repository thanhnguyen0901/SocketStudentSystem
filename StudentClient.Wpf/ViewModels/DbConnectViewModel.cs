using Caliburn.Micro;
using Student.Shared.DTOs;
using StudentClient.Wpf.Services;
using System.Windows;

namespace StudentClient.Wpf.ViewModels;

public sealed class DbConnectViewModel : Screen
{
    private readonly TcpStudentService _service;
    private readonly ShellViewModel _shell;

    private string _sqlHost = "localhost";
    private string _sqlPort = "1433";
    private string _username = "sa";
    private string _password = string.Empty;
    private string _database = "SocketStudentSystemDb";
    private string _status = "Chưa kết nối cơ sở dữ liệu.";
    private bool _isBusy;

    public DbConnectViewModel(TcpStudentService service, ShellViewModel shell)
    {
        _service = service;
        _shell = shell;
        _service.StateChanged += OnServiceStateChanged;
        DisplayName = "Kết nối cơ sở dữ liệu";
    }

    public string SqlHost
    {
        get => _sqlHost;
        set
        {
            _sqlHost = value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(CanConnectDb));
        }
    }

    public string SqlPort
    {
        get => _sqlPort;
        set
        {
            _sqlPort = value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(CanConnectDb));
        }
    }

    public string Username
    {
        get => _username;
        set
        {
            _username = value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(CanConnectDb));
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            _password = value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(CanConnectDb));
        }
    }

    public string Database
    {
        get => _database;
        set
        {
            _database = value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(CanConnectDb));
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
            NotifyOfPropertyChange(nameof(CanConnectDb));
            NotifyOfPropertyChange(nameof(AreInputsEnabled));
        }
    }

    public bool AreInputsEnabled => !IsBusy && _service.IsConnected;

    private bool _focusSqlHost;
    public bool FocusSqlHost
    {
        get => _focusSqlHost;
        set
        {
            _focusSqlHost = value;
            NotifyOfPropertyChange();
        }
    }

    public bool CanConnectDb
        => !IsBusy
        && _service.IsConnected
        && !string.IsNullOrWhiteSpace(SqlHost)
        && int.TryParse(SqlPort, out int p)
        && p is > 0 and <= 65535
        && !string.IsNullOrWhiteSpace(Username)
        && !string.IsNullOrWhiteSpace(Password)
        && !string.IsNullOrWhiteSpace(Database);

    public async Task ConnectDb()
    {
        if (!_service.IsConnected)
        {
            Status = "Mất kết nối tới máy chủ. Vui lòng kết nối lại.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            MessageBox.Show(
                "Vui lòng nhập mật khẩu SQL Server.",
                "Thiếu thông tin",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        IsBusy = true;
        Status = $"Đang kết nối tới {SqlHost}:{SqlPort}/{Database}...";

        try
        {
            var request = new DbConnectRequest(
                SqlHost: SqlHost,
                SqlPort: int.Parse(SqlPort),
                Username: Username,
                Password: Password,
                Database: Database);

            var response = await _service.SendDbConnectAsync(request);

            if (response.Success)
            {
                Status = "Đã kết nối cơ sở dữ liệu thành công.";
                await _shell.ShowStudentEntryAsync();
            }
            else
            {
                var message = response.ErrorMessage ?? "Không xác định được nguyên nhân.";
                Status = $"Kết nối cơ sở dữ liệu thất bại: {message}";

                MessageBox.Show(
                    $"Không thể kết nối cơ sở dữ liệu:\n{message}",
                    "Lỗi kết nối cơ sở dữ liệu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                FocusSqlHost = true;
                FocusSqlHost = false;
            }
        }
        catch (Exception ex)
        {
            Status = $"Kết nối cơ sở dữ liệu thất bại: {ex.Message}";

            MessageBox.Show(
                $"Không thể kết nối cơ sở dữ liệu:\n{ex.Message}",
                "Lỗi kết nối cơ sở dữ liệu",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            FocusSqlHost = true;
            FocusSqlHost = false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnServiceStateChanged(object? sender, EventArgs e)
    {
        NotifyOfPropertyChange(nameof(CanConnectDb));
        NotifyOfPropertyChange(nameof(AreInputsEnabled));

        if (!_service.IsConnected)
        {
            Status = "Mất kết nối tới máy chủ. Vui lòng kết nối lại.";
        }
    }
}
