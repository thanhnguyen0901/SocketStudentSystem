using Caliburn.Micro;
using StudentClient.Wpf.Services;

namespace StudentClient.Wpf.ViewModels;

public sealed class ShellViewModel : Conductor<IScreen>
{
    private readonly TcpStudentService _service;

    private ConnectionViewModel? _connectionVm;
    private DbConnectViewModel? _dbConnectVm;
    private StudentEntryViewModel? _studentEntryVm;

    public ShellViewModel(TcpStudentService service)
    {
        _service = service;
        _service.StateChanged += OnServiceStateChanged;
        DisplayName = "Hệ thống quản lý sinh viên Socket";
    }

#pragma warning disable CS0672
    protected override async Task OnInitializeAsync(CancellationToken ct)
#pragma warning restore CS0672
    {
        _connectionVm = new ConnectionViewModel(_service, this);
        _dbConnectVm = new DbConnectViewModel(_service, this);
        _studentEntryVm = new StudentEntryViewModel(_service);

        await ActivateItemAsync(_connectionVm, ct);
    }

    public Task ShowConnectionAsync(string? status = null, CancellationToken ct = default)
    {
        _connectionVm?.HandleDisconnected(status);
        return ActivateItemAsync(_connectionVm!, ct);
    }

    public Task ShowDbConnectAsync(CancellationToken ct = default)
        => ActivateItemAsync(_dbConnectVm!, ct);

    public Task ShowStudentEntryAsync(CancellationToken ct = default)
        => ActivateItemAsync(_studentEntryVm!, ct);

    private async void OnServiceStateChanged(object? sender, EventArgs e)
    {
        if (_service.IsConnected || _connectionVm is null)
        {
            return;
        }

        await Execute.OnUIThreadAsync(() =>
            ShowConnectionAsync("Mất kết nối tới máy chủ. Vui lòng kết nối lại."));
    }
}
