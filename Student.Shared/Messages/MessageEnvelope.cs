using Student.Shared.Enums;

namespace Student.Shared.Messages;

public sealed record MessageEnvelope<TPayload>(
    MessageType Type,
    string RequestId,
    DateTimeOffset Timestamp,
    TPayload Payload)
{
    public static MessageEnvelope<TPayload> Create(
        MessageType type,
        TPayload payload,
        string? requestId = null)
        => new(
            Type: type,
            RequestId: requestId ?? Guid.NewGuid().ToString(),
            Timestamp: DateTimeOffset.UtcNow,
            Payload: payload);

    public static MessageEnvelope<TPayload> CreateResponse(
        MessageType type,
        TPayload payload,
        string requestId)
        => Create(type, payload, requestId);
}

public sealed class MessageEnvelope
{
    public MessageType Type { get; set; }
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public object? Payload { get; set; }

    public static MessageEnvelope<TPayload> CreateResponse<TPayload>(
        MessageType type,
        TPayload payload,
        string requestId)
        => MessageEnvelope<TPayload>.CreateResponse(type, payload, requestId);

    public static MessageEnvelope<TPayload> CreateRequest<TPayload>(
        MessageType type,
        TPayload payload)
        => MessageEnvelope<TPayload>.Create(type, payload);
}
