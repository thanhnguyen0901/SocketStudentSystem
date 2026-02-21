using System.Buffers.Binary;
using System.Text.Json;

namespace StudentServer.Console.Networking;

/// <summary>
/// Deserializes messages from a <see cref="Stream"/> that uses the
/// 4-byte length-prefix framing protocol:
/// <code>
///   [ UInt32 LE : payload length (4 bytes) ][ UTF-8 JSON payload (N bytes) ]
/// </code>
/// </summary>
public static class LengthPrefixedJsonMessageReader
{
    // Pre-allocate a small reusable buffer for the 4-byte header so every read
    // does not trigger a heap allocation.
    private const int HeaderSize = sizeof(int); // 4 bytes

    /// <summary>
    /// Reads the next framed message from <paramref name="stream"/> and
    /// deserializes it as <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Target deserialization type (e.g. <c>MessageEnvelope</c>).</typeparam>
    /// <param name="stream">A connected, readable <see cref="Stream"/> (typically a <c>NetworkStream</c>).</param>
    /// <param name="ct">Token used to abort the read operation.</param>
    /// <returns>The deserialized message.</returns>
    /// <exception cref="EndOfStreamException">
    /// Thrown when <c>ReadAsync</c> returns 0, which means the remote host
    /// closed the connection gracefully.
    /// </exception>
    /// <exception cref="JsonException">Thrown when the payload is not valid JSON.</exception>
    public static async Task<T> ReadAsync<T>(Stream stream, CancellationToken ct = default)
    {
        // ── Step 1: read the 4-byte length prefix ─────────────────────────
        byte[] header = new byte[HeaderSize];
        await ReadExactlyAsync(stream, header, ct);

        int payloadLength = BinaryPrimitives.ReadInt32LittleEndian(header);

        if (payloadLength <= 0)
            throw new InvalidDataException(
                $"Invalid framing: payload length {payloadLength} is not positive.");

        // ── Step 2: read exactly payloadLength bytes of UTF-8 JSON ────────
        byte[] payload = new byte[payloadLength];
        await ReadExactlyAsync(stream, payload, ct);

        // ── Step 3: deserialize ───────────────────────────────────────────
        return JsonSerializer.Deserialize<T>(payload, FramingJsonOptions.Default)
               ?? throw new JsonException($"Deserialization of type '{typeof(T).Name}' returned null.");
    }

    /// <summary>
    /// Reads exactly <c>buffer.Length</c> bytes into <paramref name="buffer"/>,
    /// re-entering <see cref="Stream.ReadAsync(byte[],int,int,CancellationToken)"/>
    /// as many times as needed to handle TCP fragmentation (partial reads).
    /// </summary>
    /// <exception cref="EndOfStreamException">
    /// Thrown immediately when the remote side closes the connection
    /// (i.e. <c>ReadAsync</c> returns 0 before the buffer is full).
    /// </exception>
    private static async Task ReadExactlyAsync(Stream stream, byte[] buffer, CancellationToken ct)
    {
        int totalRead = 0;

        while (totalRead < buffer.Length)
        {
            int bytesRead = await stream.ReadAsync(
                buffer, totalRead, buffer.Length - totalRead, ct);

            if (bytesRead == 0)
                throw new EndOfStreamException(
                    "The remote host closed the connection before the full message was received.");

            totalRead += bytesRead;
        }
    }
}
