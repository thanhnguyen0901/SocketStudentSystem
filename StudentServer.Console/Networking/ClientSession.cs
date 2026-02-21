using System.Net;
using System.Net.Sockets;
using Student.Shared.Messages;
using Student.Shared.Enums;
using StudentServer.Console.Networking;

namespace StudentServer.Console.Networking;

/// <summary>
/// Encapsulates the lifecycle of a single connected TCP client:
/// read → log → reply → repeat until the client disconnects or an error occurs.
/// </summary>
internal sealed class ClientSession
{
    private readonly TcpClient  _client;
    private readonly string     _endpoint;  // cached for logging after socket closes

    public ClientSession(TcpClient client)
    {
        _client   = client;
        // Capture the remote endpoint string immediately; it becomes unavailable
        // once the connection is disposed.
        _endpoint = client.Client.RemoteEndPoint?.ToString() ?? "<unknown>";
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Runs the session asynchronously until the client disconnects or
    /// <paramref name="ct"/> is cancelled.
    /// All exceptions are caught here so that the server accept-loop is never
    /// interrupted by a misbehaving client.
    /// </summary>
    public async Task RunAsync(CancellationToken ct)
    {
        Log($"Client connected from {_endpoint}.");

        try
        {
            await using NetworkStream stream = _client.GetStream();

            // Keep processing messages until the client closes the connection
            // or the server is shutting down.
            while (!ct.IsCancellationRequested)
            {
                await HandleNextMessageAsync(stream, ct);
            }
        }
        catch (EndOfStreamException)
        {
            // Normal graceful disconnect – the client closed its side cleanly.
            Log($"Client {_endpoint} disconnected.");
        }
        catch (OperationCanceledException)
        {
            // Server is shutting down; no action needed.
            Log($"Session with {_endpoint} cancelled (server shutdown).");
        }
        catch (IOException ex)
        {
            // Network-level error (reset, timeout, etc.).
            Log($"[IO Error] {_endpoint}: {ex.Message}");
        }
        catch (Exception ex)
        {
            // Catch-all so the server accept-loop stays alive.
            Log($"[Unhandled Error] {_endpoint}: {ex}");
        }
        finally
        {
            _client.Close();
            Log($"Connection to {_endpoint} closed.");
        }
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Reads one framed message from <paramref name="stream"/>, logs its metadata,
    /// then sends a simple ACK reply.
    /// </summary>
    private async Task HandleNextMessageAsync(NetworkStream stream, CancellationToken ct)
    {
        // ── Read ─────────────────────────────────────────────────────────────
        // Deserialize into the non-generic envelope first so we can inspect
        // the MessageType discriminator before deciding on the payload type.
        // Full dispatch logic will be added in a later iteration.
        var envelope = await LengthPrefixedJsonMessageReader
            .ReadAsync<MessageEnvelope>(stream, ct);

        // ── Log ──────────────────────────────────────────────────────────────
        Log($"  ← [{_endpoint}] Type={envelope.Type} | RequestId={envelope.RequestId}");

        // ── Reply (placeholder ACK) ──────────────────────────────────────────
        // Build a correlated response envelope.  The MessageType used here is a
        // temporary placeholder; real dispatch (DbConnect, StudentAdd …) will
        // replace this in the business-logic layer.
        var ack = MessageEnvelope.CreateResponse(
            type:      (MessageType)0xFF,       // 0xFF = placeholder "Ack" until enum is extended
            payload:   $"ACK:{envelope.Type}",
            requestId: envelope.RequestId);

        await LengthPrefixedJsonMessageWriter.WriteAsync(stream, ack, ct);

        Log($"  → [{_endpoint}] ACK sent for RequestId={envelope.RequestId}");
    }

    /// <summary>Writes a timestamped server log line to stdout.</summary>
    private static void Log(string message)
        => System.Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss.fff}] {message}");
}
