namespace Student.Shared.DTOs;

public sealed record StudentAddRequest(string FullName, string StudentId, double Math, double Literature, double English)
{
    public bool IsValid() =>
        !string.IsNullOrWhiteSpace(FullName)
        && !string.IsNullOrWhiteSpace(StudentId)
        && IsValidScore(Math)
        && IsValidScore(Literature)
        && IsValidScore(English);

    private static bool IsValidScore(double score) => score is >= 0.0 and <= 10.0;
}

public sealed record SimpleResponse(bool Success, string? ErrorMessage)
{
    public static SimpleResponse Ok() => new(true, null);
    public static SimpleResponse Fail(string errorMessage) => new(false, errorMessage);
}
