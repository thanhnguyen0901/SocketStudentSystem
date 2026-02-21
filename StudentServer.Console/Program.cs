using StudentServer.Console.Crypto;
using StudentServer.Console.Networking;
using System.Net;
using System.Net.Sockets;
using System.Text;

// Key and IV are 8 bytes each (DES requirement). Hard-coded for this assignment.
var desKey = Encoding.ASCII.GetBytes("SV@K3y!8");
var desIv = Encoding.ASCII.GetBytes("IV#2026!");

var crypto = new DesCryptoService(desKey, desIv);
crypto.SelfTest();
Log("DES self-test passed.");

int port = 9000;
if (args.Length > 0 && int.TryParse(args[0], out int parsed) && parsed is > 0 and <= 65535)
    port = parsed;

using var cts = new CancellationTokenSource();
System.Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    Log("Shutdown requested - stopping accept loop...");
    cts.Cancel();
};

var listener = new TcpListener(IPAddress.Any, port);
listener.Start();
Log($"Server listening on port {port}. Press Ctrl+C to stop.");

var sessions = new List<Task>();

try
{
    while (!cts.Token.IsCancellationRequested)
    {
        TcpClient client;

        try
        {
            client = await listener.AcceptTcpClientAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            break;
        }

        client.Client.SetSocketOption(
            SocketOptionLevel.Socket,
            SocketOptionName.KeepAlive,
            true);

        var session = new ClientSession(client, crypto);
        var sessionTask = Task.Run(() => session.RunAsync(cts.Token), cts.Token);

        sessions.Add(sessionTask);
        sessions.RemoveAll(t => t.IsCompleted);
    }
}
finally
{
    listener.Stop();
    Log("Accept loop stopped. Waiting for active sessions to finish...");

    if (sessions.Count > 0)
        await Task.WhenAll(sessions).WaitAsync(TimeSpan.FromSeconds(5));

    Log("Server stopped.");
}

static void Log(string message)
    => System.Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss.fff}] {message}");
