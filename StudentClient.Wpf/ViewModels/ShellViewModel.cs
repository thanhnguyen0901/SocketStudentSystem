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
        DisplayName = "Socket Student System";
    }

    // Caliburn.Micro: CS0672 is expected here; the override runs exactly once before first activation.
#pragma warning disable CS0672
    protected override async Task OnInitializeAsync(CancellationToken ct)
#pragma warning restore CS0672
    {
        _connectionVm = new ConnectionViewModel(_service, this);
        _dbConnectVm = new DbConnectViewModel(_service, this);
        _studentEntryVm = new StudentEntryViewModel(_service);

        await ActivateItemAsync(_connectionVm, ct);
    }

    public Task ShowDbConnectAsync(CancellationToken ct = default)
        => ActivateItemAsync(_dbConnectVm!, ct);

    public Task ShowStudentEntryAsync(CancellationToken ct = default)
        => ActivateItemAsync(_studentEntryVm!, ct);
}
