namespace Student.Shared.DTOs;

/// <summary>
/// Sent by the client to add a new student with their subject scores.
/// </summary>
/// <param name="FullName">Full display name of the student.</param>
/// <param name="StudentId">Unique student identifier (e.g. "SV001").</param>
/// <param name="Math">Math score in the range [0, 10].</param>
/// <param name="Literature">Literature score in the range [0, 10].</param>
/// <param name="English">English score in the range [0, 10].</param>
public sealed record StudentAddRequest(
    string FullName,
    string StudentId,
    double Math,
    double Literature,
    double English)
{
    /// <summary>Returns true when identity fields are non-empty and all scores are within [0, 10].</summary>
    public bool IsValid()
        => !string.IsNullOrWhiteSpace(FullName)
        && !string.IsNullOrWhiteSpace(StudentId)
        && IsValidScore(Math)
        && IsValidScore(Literature)
        && IsValidScore(English);

    private static bool IsValidScore(double score) => score is >= 0.0 and <= 10.0;
}

/// <summary>
/// Sent by the server in reply to a <see cref="StudentAddRequest"/>.
/// </summary>
/// <param name="Success">True when the record was persisted successfully.</param>
/// <param name="ErrorMessage">Human-readable failure description; null on success.</param>
public sealed record StudentAddResponse(
    bool    Success,
    string? ErrorMessage);
