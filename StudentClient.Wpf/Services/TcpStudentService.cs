using Student.Shared.DTOs;
using Student.Shared.Enums;
using Student.Shared.Helpers;
using System.Text.Json;

namespace StudentClient.Wpf.Services;

public sealed class TcpStudentService : IDisposable
{
    private readonly TcpClientService _tcp;
    private bool _isDbConnected;

    public TcpStudentService(TcpClientService tcp)
    {
        _tcp = tcp;
        _tcp.ConnectionStateChanged += OnTcpConnectionStateChanged;
    }

    public event EventHandler? StateChanged;

    public bool IsConnected => _tcp.IsConnected;

    public bool IsDbConnected => _isDbConnected;

    public async Task ConnectAsync(string host, int port, CancellationToken ct = default)
    {
        await _tcp.ConnectAsync(host, port, ct);
        SetDbConnected(false);
    }

    public void Disconnect()
    {
        _tcp.CloseConnection();
        SetDbConnected(false);
    }

    public async Task<DbConnectResponse> SendDbConnectAsync(DbConnectRequest request, CancellationToken ct = default)
    {
        var envelope = await _tcp.RequestAsync<DbConnectRequest, DbConnectResponse>(MessageType.DbConnect, request, ct);

        SetDbConnected(envelope.Payload.Success);

        return envelope.Payload;
    }

    public async Task<SimpleResponse> SendStudentAddAsync(StudentAddRequest request, CancellationToken ct = default)
    {
        var envelope = await _tcp.RequestAsync<StudentAddRequest, SimpleResponse>(MessageType.StudentAdd, request, ct);

        return envelope.Payload;
    }

    public async Task<List<StudentResultDto>> SendResultsGetAsync(ResultsGetRequest request, CancellationToken ct = default)
    {
        // Use JsonElement as payload so we can inspect the response type before
        // committing to a concrete deserialization target (Results vs ResultsFail).
        var envelope = await _tcp.RequestAsync<ResultsGetRequest, JsonElement>(MessageType.ResultsGet, request, ct);

        if (envelope.Type == MessageType.ResultsFail)
        {
            // Deserialize the error details to surface a user-readable message.
            var error = envelope.Payload.Deserialize<ResultsGetError>(JsonDefaults.Options);
            throw new InvalidOperationException(error?.Message ?? "Máy chủ từ chối yêu cầu lấy dữ liệu.");
        }

        var rows = envelope.Payload.Deserialize<List<StudentResultDto>>(JsonDefaults.Options);
        return rows ?? [];
    }

    public void Dispose()
    {
        _tcp.ConnectionStateChanged -= OnTcpConnectionStateChanged;
        _tcp.Dispose();
    }

    private void OnTcpConnectionStateChanged(object? sender, EventArgs e)
    {
        if (!IsConnected)
        {
            _isDbConnected = false;
        }

        OnStateChanged();
    }

    private void SetDbConnected(bool value)
    {
        if (_isDbConnected == value)
        {
            OnStateChanged();
            return;
        }

        _isDbConnected = value;
        OnStateChanged();
    }

    private void OnStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
}
