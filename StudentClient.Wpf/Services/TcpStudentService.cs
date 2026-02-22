using Student.Shared.DTOs;
using Student.Shared.Enums;
using Student.Shared.Helpers;
using System.Text.Json;

namespace StudentClient.Wpf.Services;

public sealed class TcpStudentService : IDisposable
{
    private readonly TcpClientService _tcp;

    public TcpStudentService(TcpClientService tcp) => _tcp = tcp;

    public bool IsConnected => _tcp.IsConnected;

    // Reset to false whenever a new TCP connection is opened.
    public bool IsDbConnected { get; private set; }

    public async Task ConnectAsync(string host, int port, CancellationToken ct = default)
    {
        await _tcp.ConnectAsync(host, port, ct);
        IsDbConnected = false;
    }

    public void Disconnect()
    {
        _tcp.CloseConnection();
        IsDbConnected = false;
    }

    public async Task<DbConnectResponse> SendDbConnectAsync(DbConnectRequest request, CancellationToken ct = default)
    {
        var envelope = await _tcp.RequestAsync<DbConnectRequest, DbConnectResponse>(MessageType.DbConnect, request, ct);

        if (envelope.Payload.Success)
        {
            IsDbConnected = true;
        }

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
            throw new InvalidOperationException(error?.Message ?? "Server rejected the results query.");
        }

        var rows = envelope.Payload.Deserialize<List<StudentResultDto>>(JsonDefaults.Options);
        return rows ?? [];
    }

    public void Dispose() => _tcp.Dispose();
}