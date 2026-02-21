namespace StudentServer.Console.Data;

// In-memory mirror of dbo.StudentsEncrypted; all score and name fields are DES ciphertext.
internal sealed class EncryptedStudentRow
{
    public int Id { get; init; }

    // StudentId is stored in plain text so UPSERT can locate rows without decrypting.
    public required string StudentId { get; init; }

    public required byte[] FullNameEnc { get; init; }
    public required byte[] MathEnc { get; init; }
    public required byte[] LiteratureEnc { get; init; }
    public required byte[] EnglishEnc { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
