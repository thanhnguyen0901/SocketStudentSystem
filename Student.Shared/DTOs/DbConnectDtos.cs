namespace Student.Shared.DTOs;

public sealed record DbConnectRequest(
    string SqlHost,
    int SqlPort,
    string Username,
    string Password,
    string Database)
{
    public bool IsValid()
        => !string.IsNullOrWhiteSpace(SqlHost)
        && !string.IsNullOrWhiteSpace(Username)
        && !string.IsNullOrWhiteSpace(Database)
        && SqlPort is > 0 and <= 65535;
}

public sealed record DbConnectResponse(bool Success, string? ErrorMessage);
