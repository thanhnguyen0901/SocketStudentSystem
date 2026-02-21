namespace Student.Shared.DTOs;

/// <summary>Supported query modes for <see cref="ResultsGetRequest"/>.</summary>
public static class ResultsMode
{
    /// <summary>Return grade records for every student in the database.</summary>
    public const string All = "ALL";

    /// <summary>Return the grade record for a single student identified by <see cref="ResultsGetRequest.StudentId"/>.</summary>
    public const string ById = "BY_ID";
}

/// <summary>
/// Sent by the client to retrieve grade results.
/// </summary>
/// <param name="Mode">
/// Query mode: <see cref="ResultsMode.All"/> or <see cref="ResultsMode.ById"/>.
/// </param>
/// <param name="StudentId">
/// Required when <paramref name="Mode"/> is <see cref="ResultsMode.ById"/>;
/// ignored (may be null) for <see cref="ResultsMode.All"/>.
/// </param>
public sealed record ResultsGetRequest(
    string  Mode,
    string? StudentId)
{
    /// <summary>Returns true when the request is internally consistent.</summary>
    public bool IsValid()
        => Mode == ResultsMode.All
        || (Mode == ResultsMode.ById && !string.IsNullOrWhiteSpace(StudentId));
}

/// <summary>
/// Represents a single student's computed result returned inside a
/// <c>MessageEnvelope&lt;IReadOnlyList&lt;StudentResultDto&gt;&gt;</c> response.
/// </summary>
/// <param name="FullName">Full display name of the student.</param>
/// <param name="StudentId">Unique student identifier.</param>
/// <param name="Math">Math score.</param>
/// <param name="Literature">Literature score.</param>
/// <param name="English">English score.</param>
/// <param name="Average">
/// Arithmetic average of the three subject scores,
/// computed as <c>(Math + Literature + English) / 3.0</c>.
/// </param>
public sealed record StudentResultDto(
    string FullName,
    string StudentId,
    double Math,
    double Literature,
    double English,
    double Average)
{
    /// <summary>
    /// Convenience factory: creates a <see cref="StudentResultDto"/> and calculates
    /// the average automatically so callers do not need to repeat the formula.
    /// </summary>
    public static StudentResultDto Create(
        string fullName,
        string studentId,
        double math,
        double literature,
        double english)
        => new(
            fullName,
            studentId,
            math,
            literature,
            english,
            Average: System.Math.Round((math + literature + english) / 3.0, 2));
}
