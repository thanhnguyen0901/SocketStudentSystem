using System.Text.Json;
using System.Text.Json.Serialization;

namespace StudentServer.Console.Networking;

/// <summary>
/// Shared <see cref="JsonSerializerOptions"/> used by every framing class in this namespace.
/// Centralising the options ensures the reader and writer always agree on the wire format.
/// </summary>
internal static class FramingJsonOptions
{
    /// <summary>
    /// Deterministic serializer options:
    /// <list type="bullet">
    ///   <item>camelCase property names – compact and idiomatic for JSON APIs.</item>
    ///   <item><see cref="JsonIgnoreCondition.WhenWritingNull"/> – omit null fields to save bandwidth.</item>
    ///   <item>Enum values written as strings – more readable in logs and easier to extend.</item>
    /// </list>
    /// The instance is created once and reused; <see cref="JsonSerializerOptions"/> is thread-safe
    /// after construction.
    /// </summary>
    internal static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.General)
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented               = false,       // compact wire format
        Converters                  = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };
}
