using System.Buffers.Binary;
using System.IO;
using System.Text.Json;

namespace StudentClient.Wpf.Services;

/// <summary>
/// Serializes messages to a <see cref="Stream"/> using the
/// 4-byte length-prefix framing protocol:
/// <code>
///   [ Int32 LE : payload length (4 bytes) ][ UTF-8 JSON payload (N bytes) ]
/// </code>
/// </summary>
public static class LengthPrefixedJsonMessageWriter
{
    private const int HeaderSize = sizeof(int); // 4 bytes

    /// <summary>
    /// Serializes <paramref name="message"/> to UTF-8 JSON, prepends a 4-byte
    /// little-endian length prefix, and writes both to <paramref name="stream"/>.
    /// The stream is flushed after writing so the bytes are sent immediately.
    /// </summary>
    /// <typeparam name="T">Type of the message object to serialize.</typeparam>
    /// <param name="stream">A connected, writable <see cref="Stream"/> (typically a <c>NetworkStream</c>).</param>
    /// <param name="message">The message to serialize and send.</param>
    /// <param name="ct">Token used to abort the write operation.</param>
    public static async Task WriteAsync<T>(Stream stream, T message, CancellationToken ct = default)
    {
        // ── Step 1: serialize message to UTF-8 JSON bytes ─────────────────
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(message, FramingJsonOptions.Default);

        // ── Step 2: build the 4-byte little-endian length header ──────────
        byte[] header = new byte[HeaderSize];
        BinaryPrimitives.WriteInt32LittleEndian(header, payload.Length);

        // ── Step 3: write header + payload ────────────────────────────────
        // Both writes are buffered by the OS TCP stack and will be coalesced
        // into as few IP segments as possible (Nagle or OS discretion).
        await stream.WriteAsync(header, ct);
        await stream.WriteAsync(payload, ct);

        // Flush ensures bytes leave the application buffer immediately.
        await stream.FlushAsync(ct);
    }
}
