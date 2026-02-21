using Student.Shared.Enums;

namespace Student.Shared.Messages;

/// <summary>
/// Wire format wrapper sent over the TCP channel for every message exchanged
/// between the server and any connected client.
/// </summary>
/// <typeparam name="T">
/// The concrete payload type (one of the DTO classes in <c>Student.Shared.DTOs</c>).
/// Use <c>object?</c> when the message carries no payload (e.g. a pure acknowledgement).
/// </typeparam>
public sealed class MessageEnvelope<T>
{
    /// <summary>Discriminator that identifies the payload type and message intent.</summary>
    public MessageType Type { get; set; }

    /// <summary>
    /// Correlation identifier (GUID string) set by the originator.
    /// The receiver echoes this value so the sender can match requests to responses.
    /// </summary>
    public string RequestId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Strongly-typed payload. Null for messages that carry no body.</summary>
    public T? Payload { get; set; }

    /// <summary>
    /// UTC timestamp recorded at the moment the envelope is created.
    /// Useful for latency diagnostics and audit logs.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Non-generic convenience alias for messages whose payload type is only known at runtime
/// (e.g. during generic deserialization before the type discriminator is inspected).
/// </summary>
public sealed class MessageEnvelope
{
    /// <summary>Discriminator that identifies the payload type and message intent.</summary>
    public MessageType Type { get; set; }

    /// <inheritdoc cref="MessageEnvelope{T}.RequestId"/>
    public string RequestId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Raw payload object. Cast to the expected DTO after reading <see cref="Type"/>.</summary>
    public object? Payload { get; set; }

    /// <inheritdoc cref="MessageEnvelope{T}.Timestamp"/>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    // ── Factory helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Creates a typed envelope, copying the <see cref="RequestId"/> so it can be used
    /// as a correlated response to an incoming request.
    /// </summary>
    public static MessageEnvelope<T> CreateResponse<T>(
        MessageType type,
        T payload,
        string requestId)
        => new()
        {
            Type      = type,
            Payload   = payload,
            RequestId = requestId,
            Timestamp = DateTimeOffset.UtcNow,
        };

    /// <summary>Creates a new outbound request envelope with a fresh correlation ID.</summary>
    public static MessageEnvelope<T> CreateRequest<T>(MessageType type, T payload)
        => new()
        {
            Type    = type,
            Payload = payload,
        };
}
