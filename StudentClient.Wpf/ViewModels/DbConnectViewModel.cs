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
    private string _status = "Enter SQL Server credentials and click Connect DB.";
    private bool _isBusy;

    public DbConnectViewModel(TcpStudentService service, ShellViewModel shell)
    {
        _service = service;
        _shell = shell;
        DisplayName = "Connect to Database";
    }

    public string SqlHost
    {
        get => _sqlHost;
        set { _sqlHost = value; NotifyOfPropertyChange(); NotifyOfPropertyChange(nameof(CanConnectDb)); }
    }

    public string SqlPort
    {
        get => _sqlPort;
        set { _sqlPort = value; NotifyOfPropertyChange(); NotifyOfPropertyChange(nameof(CanConnectDb)); }
    }

    public string Username
    {
        get => _username;
        set { _username = value; NotifyOfPropertyChange(); NotifyOfPropertyChange(nameof(CanConnectDb)); }
    }

    // Set by DbConnectView code-behind via PasswordBox.PasswordChanged
    // (PasswordBox has no bindable Password dependency property).
    public string Password
    {
        get => _password;
        set { _password = value; NotifyOfPropertyChange(); NotifyOfPropertyChange(nameof(CanConnectDb)); }
    }

    public string Database
    {
        get => _database;
        set { _database = value; NotifyOfPropertyChange(); NotifyOfPropertyChange(nameof(CanConnectDb)); }
    }

    public string Status
    {
        get => _status;
        set { _status = value; NotifyOfPropertyChange(); }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; NotifyOfPropertyChange(); NotifyOfPropertyChange(nameof(CanConnectDb)); }
    }

    // Momentary trigger: set to true to ask the View to focus SqlHost, then immediately reset.
    private bool _focusSqlHost;
    public bool FocusSqlHost
    {
        get => _focusSqlHost;
        set { _focusSqlHost = value; NotifyOfPropertyChange(); }
    }

    public bool CanConnectDb
        => !IsBusy
        && !string.IsNullOrWhiteSpace(SqlHost)
        && int.TryParse(SqlPort, out int p) && p is > 0 and <= 65535
        && !string.IsNullOrWhiteSpace(Username)
        && !string.IsNullOrWhiteSpace(Database);

    public async Task ConnectDb()
    {
        IsBusy = true;
        Status = $"Connecting to {SqlHost}:{SqlPort}/{Database}...";

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
                Status = "Database connected.";
                await _shell.ShowStudentEntryAsync();
            }
            else
            {
                var msg = response.ErrorMessage ?? "Unknown error.";
                Status = $"DB connect failed: {msg}";

                // Show a visible popup so the error is not missed.
                MessageBox.Show(
                    $"Kết nối DB thất bại:\n{msg}",
                    "Lỗi kết nối Database",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                // Ask the View to move focus back to SqlHost so the user can correct it.
                FocusSqlHost = true;
                FocusSqlHost = false;
            }
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";

            MessageBox.Show(
                $"Kết nối DB thất bại:\n{ex.Message}",
                "Lỗi kết nối Database",
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
}
