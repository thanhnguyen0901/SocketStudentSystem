using Student.Shared.Enums;
using System.Text.Json;

namespace StudentServer.Console.Networking;

// First-pass deserialization target: Type is read before the payload is re-deserialized
// to the correct strongly-typed DTO.
internal sealed class RawEnvelope
{
    public MessageType Type { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public JsonElement Payload { get; set; }
}