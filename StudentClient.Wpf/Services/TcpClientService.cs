using Student.Shared.Enums;
using Student.Shared.Messages;
using System.IO;
using System.Net.Sockets;

namespace StudentClient.Wpf.Services;

// Manages a single TcpClient/NetworkStream lifetime with a semaphore to
// serialize concurrent callers (safety net; UI flow is sequential in practice).
public sealed class TcpClientService : IDisposable
{
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;

    private readonly SemaphoreSlim _gate = new(1, 1);

    public bool IsConnected => _tcpClient is { Connected: true };

    public async Task ConnectAsync(
        string host,
        int port,
        CancellationToken ct = default)
    {
        CloseConnection();
        _tcpClient = new TcpClient();
        await _tcpClient.ConnectAsync(host, port, ct);
        _stream = _tcpClient.GetStream();
    }

    public void CloseConnection()
    {
        _stream?.Dispose();
        _tcpClient?.Dispose();
        _stream = null;
        _tcpClient = null;
    }

    public async Task SendAsync<TPayload>(
        MessageType type,
        TPayload payload,
        CancellationToken ct = default)
    {
        EnsureConnected();

        await _gate.WaitAsync(ct);
        try
        {
            var envelope = MessageEnvelope.CreateRequest(type, payload);
            await LengthPrefixedJsonProtocol.WriteAsync(_stream!, envelope, ct);
        }
        catch (Exception ex) when (IsNetworkException(ex))
        {
            CloseConnection();
            throw new InvalidOperationException(
                "The connection to the server was lost. Please reconnect.", ex);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<MessageEnvelope<TResponse>> RequestAsync<TRequest, TResponse>(
        MessageType type,
        TRequest payload,
        CancellationToken ct = default)
    {
        EnsureConnected();

        await _gate.WaitAsync(ct);
        try
        {
            var request = MessageEnvelope.CreateRequest(type, payload);
            await LengthPrefixedJsonProtocol.WriteAsync(_stream!, request, ct);

            return await LengthPrefixedJsonProtocol
                .ReadAsync<MessageEnvelope<TResponse>>(_stream!, ct);
        }
        catch (Exception ex) when (IsNetworkException(ex))
        {
            CloseConnection();
            throw new InvalidOperationException(
                "The connection to the server was lost. Please reconnect.", ex);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        CloseConnection();
        _gate.Dispose();
    }

    private void EnsureConnected()
    {
        if (_stream is null || !IsConnected)
            throw new InvalidOperationException(
                "Not connected to the server. Call ConnectAsync first.");
    }

    private static bool IsNetworkException(Exception ex)
        => ex is IOException or SocketException;
}
