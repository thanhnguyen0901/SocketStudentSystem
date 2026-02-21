using System.Net;
using System.Net.Sockets;
using StudentServer.Console.Networking;

// ── Parse listen port from command-line args (default: 9000) ─────────────────
int port = 9000;
if (args.Length > 0 && int.TryParse(args[0], out int parsed) && parsed is > 0 and <= 65535)
    port = parsed;

// ── Cancellation: honour Ctrl+C / SIGTERM for graceful shutdown ───────────────
using var cts = new CancellationTokenSource();
System.Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;    // prevent the process from being killed immediately
    Log("Shutdown requested – stopping accept loop…");
    cts.Cancel();
};

// ── Start TcpListener on all network interfaces ───────────────────────────────
var listener = new TcpListener(IPAddress.Any, port);
listener.Start();
Log($"Server listening on port {port}. Press Ctrl+C to stop.");

// Track active session tasks so we can await them on shutdown.
var sessions = new List<Task>();

try
{
    while (!cts.Token.IsCancellationRequested)
    {
        TcpClient client;

        try
        {
            // AcceptTcpClientAsync does not accept a CancellationToken directly before
            // .NET 5 overloads were added; use the token-aware overload available in .NET 8.
            client = await listener.AcceptTcpClientAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Shutdown was requested – exit the accept loop cleanly.
            break;
        }

        // Configure TCP keep-alive so stale half-open connections are detected.
        client.Client.SetSocketOption(
            SocketOptionLevel.Socket,
            SocketOptionName.KeepAlive,
            true);

        // Fire-and-forget: each session runs independently on the thread pool.
        // Exceptions are handled inside ClientSession.RunAsync so they never
        // propagate back here.
        var session = new ClientSession(client);
        var sessionTask = Task.Run(() => session.RunAsync(cts.Token), cts.Token);

        sessions.Add(sessionTask);

        // Prune completed tasks to keep the list from growing indefinitely.
        sessions.RemoveAll(t => t.IsCompleted);
    }
}
finally
{
    // Stop accepting new connections.
    listener.Stop();
    Log("Accept loop stopped. Waiting for active sessions to finish…");

    // Give in-flight sessions a short grace period before exiting.
    if (sessions.Count > 0)
        await Task.WhenAll(sessions).WaitAsync(TimeSpan.FromSeconds(5));

    Log("Server stopped.");
}

// ── Local helper ──────────────────────────────────────────────────────────────
static void Log(string message)
    => System.Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss.fff}] {message}");
