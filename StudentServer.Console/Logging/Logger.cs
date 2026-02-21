using System.Text;

namespace StudentServer.Console.Logging;

// Minimum log level, controlled via the LOG_LEVEL environment variable (default: INFO).
// Valid values (case-insensitive): DEBUG, INFO, WARN, ERROR
public enum LogLevel
{
    DEBUG = 0,
    INFO = 1,
    WARN = 2,
    ERROR = 3,
}

/// <summary>
/// Centralised, thread-safe console logger for the server process.
/// Output format: [HH:mm:ss.fff] [LEVEL] [Category] [Conn=ip:port] [ReqId=id] message
/// </summary>
public static class Logger
{
    // Resolved once at startup; hot-path reads are lock-free.
    private static readonly LogLevel _minLevel = ResolveMinLevel();

    // ------------------- Public API -------------------

    public static void Info(
        string category,
        string message,
        string? conn = null,
        string? reqId = null)
        => Write(LogLevel.INFO, category, message, conn, reqId, null);

    public static void Warn(
        string category,
        string message,
        string? conn = null,
        string? reqId = null)
        => Write(LogLevel.WARN, category, message, conn, reqId, null);

    public static void Error(
        string category,
        string message,
        string? conn = null,
        string? reqId = null,
        Exception? ex = null)
        => Write(LogLevel.ERROR, category, message, conn, reqId, ex);

    public static void Debug(
        string category,
        string message,
        string? conn = null,
        string? reqId = null)
        => Write(LogLevel.DEBUG, category, message, conn, reqId, null);

    // ------------------- Core -------------------

    // Build the full log line as a single string before calling Console.WriteLine so
    // that concurrent writes from multiple sessions are never interleaved.
    private static void Write(
        LogLevel level,
        string category,
        string message,
        string? conn,
        string? reqId,
        Exception? ex)
    {
        if (level < _minLevel) return;

        var sb = new StringBuilder(128);
        sb.Append($"[{DateTimeOffset.Now:HH:mm:ss.fff}]");
        sb.Append($" [{LevelTag(level)}]");
        sb.Append($" [{category}]");

        if (conn is not null) sb.Append($" [Conn={conn}]");
        if (reqId is not null) sb.Append($" [ReqId={reqId}]");

        sb.Append(' ');
        sb.Append(message);

        if (ex is not null)
        {
            sb.AppendLine();
            sb.Append(ex);
        }

        // Console.WriteLine is itself thread-safe, and building the string first
        // guarantees a single atomic write per log entry.
        System.Console.WriteLine(sb.ToString());
    }

    // Fixed-width 5-char tags make columns align in terminal output.
    private static string LevelTag(LogLevel level) => level switch
    {
        LogLevel.DEBUG => "DEBUG",
        LogLevel.INFO => "INFO ",
        LogLevel.WARN => "WARN ",
        LogLevel.ERROR => "ERROR",
        _ => level.ToString(),
    };

    // Read LOG_LEVEL env var set before the process starts (e.g. LOG_LEVEL=DEBUG).
    private static LogLevel ResolveMinLevel()
    {
        var raw = Environment.GetEnvironmentVariable("LOG_LEVEL");
        if (raw is not null
            && Enum.TryParse<LogLevel>(raw, ignoreCase: true, out var parsed))
        {
            return parsed;
        }
        return LogLevel.INFO;
    }
}
