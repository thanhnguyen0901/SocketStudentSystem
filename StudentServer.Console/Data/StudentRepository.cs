using Microsoft.Data.SqlClient;

namespace StudentServer.Console.Data;

internal static class StudentRepository
{
    // Creates dbo.StudentsEncrypted if it does not exist.
    internal static async Task EnsureSchemaAsync(SqlConnection conn, CancellationToken ct = default)
    {
        const string sql = """
            IF NOT EXISTS (
                SELECT 1
                FROM   sys.objects
                WHERE  object_id = OBJECT_ID(N'dbo.StudentsEncrypted')
                  AND  type      = N'U'
            )
            BEGIN
                CREATE TABLE dbo.StudentsEncrypted
                (
                    Id              INT            IDENTITY(1,1)  NOT NULL,
                    StudentId       NVARCHAR(50)                  NOT NULL,
                    FullNameEnc     VARBINARY(MAX)                NOT NULL,
                    MathEnc         VARBINARY(MAX)                NOT NULL,
                    LiteratureEnc   VARBINARY(MAX)                NOT NULL,
                    EnglishEnc      VARBINARY(MAX)                NOT NULL,
                    CreatedAt       DATETIME2      NOT NULL  DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT PK_StudentsEncrypted       PRIMARY KEY CLUSTERED (Id),
                    CONSTRAINT UQ_StudentsEncrypted_StdId UNIQUE (StudentId)
                );
            END
            """;

        await using var cmd = new SqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    // MERGE provides an atomic UPSERT; CreatedAt is preserved on UPDATE.
    internal static async Task UpsertEncryptedAsync(SqlConnection conn, EncryptedStudentRow row, CancellationToken ct = default)
    {
        const string sql = """
            MERGE dbo.StudentsEncrypted AS target
            USING (SELECT @StudentId AS StudentId) AS source
                ON target.StudentId = source.StudentId
            WHEN MATCHED THEN
                UPDATE SET
                    FullNameEnc   = @FullNameEnc,
                    MathEnc       = @MathEnc,
                    LiteratureEnc = @LiteratureEnc,
                    EnglishEnc    = @EnglishEnc
            WHEN NOT MATCHED THEN
                INSERT (StudentId, FullNameEnc, MathEnc, LiteratureEnc, EnglishEnc)
                VALUES (@StudentId, @FullNameEnc, @MathEnc, @LiteratureEnc, @EnglishEnc);
            """;

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@StudentId", row.StudentId);
        cmd.Parameters.AddWithValue("@FullNameEnc", row.FullNameEnc);
        cmd.Parameters.AddWithValue("@MathEnc", row.MathEnc);
        cmd.Parameters.AddWithValue("@LiteratureEnc", row.LiteratureEnc);
        cmd.Parameters.AddWithValue("@EnglishEnc", row.EnglishEnc);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    internal static async Task<List<EncryptedStudentRow>> GetAllAsync(SqlConnection conn, CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id, StudentId, FullNameEnc, MathEnc, LiteratureEnc, EnglishEnc, CreatedAt
            FROM   dbo.StudentsEncrypted
            ORDER  BY CreatedAt ASC, Id ASC;
            """;

        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var rows = new List<EncryptedStudentRow>();
        while (await reader.ReadAsync(ct))
            rows.Add(MapRow(reader));

        return rows;
    }

    internal static async Task<EncryptedStudentRow?> GetByStudentIdAsync(SqlConnection conn, string studentId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT TOP 1
                   Id, StudentId, FullNameEnc, MathEnc, LiteratureEnc, EnglishEnc, CreatedAt
            FROM   dbo.StudentsEncrypted
            WHERE  StudentId = @StudentId;
            """;

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@StudentId", studentId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        if (await reader.ReadAsync(ct))
        {
            return MapRow(reader);
        }

        return null;
    }

    private static EncryptedStudentRow MapRow(SqlDataReader reader)
        => new()
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            StudentId = reader.GetString(reader.GetOrdinal("StudentId")),
            FullNameEnc = (byte[])reader["FullNameEnc"],
            MathEnc = (byte[])reader["MathEnc"],
            LiteratureEnc = (byte[])reader["LiteratureEnc"],
            EnglishEnc = (byte[])reader["EnglishEnc"],
            CreatedAt = reader.GetDateTimeOffset(reader.GetOrdinal("CreatedAt")),
        };
}
