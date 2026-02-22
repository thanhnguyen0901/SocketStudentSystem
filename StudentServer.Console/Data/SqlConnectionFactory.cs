using Microsoft.Data.SqlClient;
using Student.Shared.DTOs;

namespace StudentServer.Console.Data;

internal static class SqlConnectionFactory
{
    internal static async Task<SqlConnection> OpenAsync(DbConnectRequest request, CancellationToken ct = default)
    {
        var connection = new SqlConnection(BuildConnectionString(request));
        await connection.OpenAsync(ct);
        return connection;
    }

    private static string BuildConnectionString(DbConnectRequest request)
    {
        var builder = new SqlConnectionStringBuilder
        {
            // SQL Server driver uses "host,port" (comma-separated, not colon).
            DataSource = $"{request.SqlHost},{request.SqlPort}",
            InitialCatalog = request.Database,
            UserID = request.Username,
            Password = request.Password,
            Encrypt = true,
            TrustServerCertificate = true, // dev convenience; tighten for production
            ConnectTimeout = 10,
        };

        return builder.ConnectionString;
    }
}
