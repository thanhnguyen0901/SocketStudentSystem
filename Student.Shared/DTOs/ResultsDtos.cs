namespace Student.Shared.DTOs;

public static class ResultsMode
{
    public const string All = "ALL";
    public const string ById = "BY_ID";
}

public sealed record ResultsGetRequest(
    string Mode,
    string? StudentId)
{
    public bool IsValid()
        => Mode == ResultsMode.All
        || (Mode == ResultsMode.ById && !string.IsNullOrWhiteSpace(StudentId));
}

public sealed record StudentResultDto(
    string FullName,
    string StudentId,
    double Average)
{
    public static StudentResultDto Create(
        string fullName,
        string studentId,
        double math,
        double literature,
        double english)
        => new(
            fullName,
            studentId,
            Average: System.Math.Round((math + literature + english) / 3.0, 2));
}
