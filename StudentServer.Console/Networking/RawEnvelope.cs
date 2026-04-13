using Student.Shared.Enums;
using System.Text.Json;

namespace StudentServer.Console.Networking;

internal sealed class RawEnvelope
{
    public MessageType Type { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public JsonElement Payload { get; set; }
}