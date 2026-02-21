using Student.Shared.Helpers;
using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text.Json;

namespace StudentServer.Console.Networking;

// Wire format: [4-byte LE length][UTF-8 JSON payload]
public static class LengthPrefixedJsonProtocol
{
    private const int MaxPayloadBytes = 1 * 1024 * 1024; // 1 MiB guard
    private const int HeaderSize = sizeof(int);

    public static async Task WriteAsync<T>(
        NetworkStream stream,
        T message,
        CancellationToken ct = default)
    {
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(message, JsonDefaults.Options);

        if (payload.Length > MaxPayloadBytes)
            throw new ArgumentException(
                $"Serialized payload ({payload.Length:N0} B) exceeds the maximum of {MaxPayloadBytes:N0} B.");

        // Single WriteAsync call avoids Nagle-algorithm latency.
        byte[] frame = new byte[HeaderSize + payload.Length];
        BinaryPrimitives.WriteInt32LittleEndian(frame, payload.Length);
        payload.CopyTo(frame, HeaderSize);

        await stream.WriteAsync(frame, ct);
        await stream.FlushAsync(ct);
    }

    public static async Task<T> ReadAsync<T>(
        NetworkStream stream,
        CancellationToken ct = default)
    {
        byte[] header = new byte[HeaderSize];
        await ReadExactlyAsync(stream, header, ct);

        int payloadLength = BinaryPrimitives.ReadInt32LittleEndian(header);

        if (payloadLength <= 0)
            throw new InvalidDataException(
                $"Framing error: length prefix {payloadLength} is not positive.");

        if (payloadLength > MaxPayloadBytes)
            throw new InvalidDataException(
                $"Framing error: length prefix {payloadLength:N0} B exceeds the maximum of {MaxPayloadBytes:N0} B.");

        byte[] payload = new byte[payloadLength];
        await ReadExactlyAsync(stream, payload, ct);

        return JsonSerializer.Deserialize<T>(payload, JsonDefaults.Options)
               ?? throw new JsonException($"Deserializing '{typeof(T).Name}' produced null.");
    }

    // Loops over partial TCP reads until the buffer is fully filled.
    internal static async Task ReadExactlyAsync(
        Stream stream,
        byte[] buffer,
        CancellationToken ct)
    {
        int offset = 0;
        while (offset < buffer.Length)
        {
            int bytesRead = await stream.ReadAsync(
                buffer.AsMemory(offset, buffer.Length - offset), ct);

            if (bytesRead == 0)
                throw new IOException(
                    "Remote host closed the connection before the full frame was received.");

            offset += bytesRead;
        }
    }
}
