namespace Student.Shared.Enums;

/// <summary>
/// Identifies the intent of every <c>MessageEnvelope</c> exchanged over the TCP channel.
/// The server and client use this discriminator to deserialize the correct payload type.
/// </summary>
public enum MessageType : byte
{
    // ── Connection handshake ──────────────────────────────────────────────────

    /// <summary>Client → Server: request a database connection.</summary>
    DbConnect = 1,

    /// <summary>Server → Client: database connection established successfully.</summary>
    DbConnectOk = 2,

    /// <summary>Server → Client: database connection attempt failed.</summary>
    DbConnectFail = 3,

    // ── Student operations ────────────────────────────────────────────────────

    /// <summary>Client → Server: add a new student record.</summary>
    StudentAdd = 10,

    /// <summary>Server → Client: student record added successfully.</summary>
    StudentAddOk = 11,

    /// <summary>Server → Client: student record could not be added.</summary>
    StudentAddFail = 12,

    // ── Results queries ───────────────────────────────────────────────────────

    /// <summary>Client → Server: request grade results (all or by student ID).</summary>
    ResultsGet = 20,

    /// <summary>Server → Client: results payload in response to <see cref="ResultsGet"/>.</summary>
    Results = 21,
}
