using Student.Shared.DTOs;
using Student.Shared.Enums;

namespace StudentClient.Wpf.Services;

public sealed class TcpStudentService : IDisposable
{
    private readonly TcpClientService _tcp;

    public TcpStudentService(TcpClientService tcp) => _tcp = tcp;

    public bool IsConnected => _tcp.IsConnected;

    // Reset to false whenever a new TCP connection is opened.
    public bool IsDbConnected { get; private set; }

    public async Task ConnectAsync(
        string host,
        int port,
        CancellationToken ct = default)
    {
        await _tcp.ConnectAsync(host, port, ct);
        IsDbConnected = false;
    }

    public void Disconnect()
    {
        _tcp.CloseConnection();
        IsDbConnected = false;
    }

    public async Task<DbConnectResponse> SendDbConnectAsync(
        DbConnectRequest request,
        CancellationToken ct = default)
    {
        var envelope = await _tcp.RequestAsync<DbConnectRequest, DbConnectResponse>(
            MessageType.DbConnect, request, ct);

        if (envelope.Payload.Success)
            IsDbConnected = true;

        return envelope.Payload;
    }

    public async Task<SimpleResponse> SendStudentAddAsync(
        StudentAddRequest request,
        CancellationToken ct = default)
    {
        var envelope = await _tcp.RequestAsync<StudentAddRequest, SimpleResponse>(
            MessageType.StudentAdd, request, ct);

        return envelope.Payload;
    }

    public async Task<List<StudentResultDto>> SendResultsGetAsync(
        ResultsGetRequest request,
        CancellationToken ct = default)
    {
        var envelope = await _tcp.RequestAsync<ResultsGetRequest, List<StudentResultDto>?>(
            MessageType.ResultsGet, request, ct);

        return envelope.Payload ?? [];
    }

    public void Dispose() => _tcp.Dispose();
}
