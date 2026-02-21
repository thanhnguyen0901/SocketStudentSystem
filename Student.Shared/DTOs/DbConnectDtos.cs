namespace Student.Shared.DTOs;

/// <summary>
/// Sent by the client to request a SQL Server / SQL-compatible database connection
/// on the server side.
/// </summary>
/// <param name="Host">Hostname or IP of the database server.</param>
/// <param name="Port">TCP port of the database server (default SQL Server: 1433).</param>
/// <param name="Username">Login username for the database engine.</param>
/// <param name="Password">Login password (transmitted only over an encrypted channel).</param>
/// <param name="Database">Initial catalog / database name to open.</param>
public sealed record DbConnectRequest(
    string Host,
    int    Port,
    string Username,
    string Password,
    string Database)
{
    /// <summary>Returns true when all required fields are non-empty and the port is in range.</summary>
    public bool IsValid()
        => !string.IsNullOrWhiteSpace(Host)
        && !string.IsNullOrWhiteSpace(Username)
        && !string.IsNullOrWhiteSpace(Database)
        && Port is > 0 and <= 65535;
}

/// <summary>
/// Sent by the server in reply to a <see cref="DbConnectRequest"/>.
/// </summary>
/// <param name="Success">True when the server opened the database connection successfully.</param>
/// <param name="ErrorMessage">Human-readable failure description; null on success.</param>
public sealed record DbConnectResponse(
    bool    Success,
    string? ErrorMessage);
