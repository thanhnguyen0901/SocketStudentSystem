using Microsoft.Data.SqlClient;
using Student.Shared.DTOs;
using Student.Shared.Enums;
using Student.Shared.Helpers;
using Student.Shared.Messages;
using StudentServer.Console.Crypto;
using StudentServer.Console.Data;
using System.Globalization;
using System.Net.Sockets;
using System.Text.Json;

namespace StudentServer.Console.Networking;

internal sealed class ClientSession
{
    private readonly TcpClient _client;
    private readonly DesCryptoService _crypto;
    private readonly string _endpoint;

    // Null until a successful DbConnect message is received.
    private SqlConnection? _db;

    public ClientSession(TcpClient client, DesCryptoService crypto)
    {
        _client = client;
        _crypto = crypto;
        _endpoint = client.Client.RemoteEndPoint?.ToString() ?? "<unknown>";
    }

    public async Task RunAsync(CancellationToken ct)
    {
        Log($"Client connected from {_endpoint}.");

        try
        {
            await using NetworkStream stream = _client.GetStream();

            while (!ct.IsCancellationRequested)
                await HandleNextMessageAsync(stream, ct);
        }
        catch (IOException)
        {
            Log($"Client {_endpoint} disconnected.");
        }
        catch (OperationCanceledException)
        {
            Log($"Session with {_endpoint} cancelled (server shutdown).");
        }
        catch (Exception ex)
        {
            Log($"[Unhandled] {_endpoint}: {ex}");
        }
        finally
        {
            _db?.Dispose();
            _client.Close();
            Log($"Connection to {_endpoint} closed.");
        }
    }

    // Reads one framed message and routes it to the correct handler.
    private async Task HandleNextMessageAsync(NetworkStream stream, CancellationToken ct)
    {
        var raw = await LengthPrefixedJsonProtocol.ReadAsync<RawEnvelope>(stream, ct);
        Log($"  <- [{_endpoint}] Type={raw.Type} | ReqId={raw.RequestId}");

        switch (raw.Type)
        {
            case MessageType.DbConnect:
                await HandleDbConnectAsync(stream, raw, ct);
                break;

            case MessageType.StudentAdd:
                await HandleStudentAddAsync(stream, raw, ct);
                break;

            case MessageType.ResultsGet:
                await HandleResultsGetAsync(stream, raw, ct);
                break;

            default:
                Log($"  [Unknown type {raw.Type}] ignored.");
                break;
        }
    }

    private async Task HandleDbConnectAsync(
        NetworkStream stream, RawEnvelope raw, CancellationToken ct)
    {
        DbConnectRequest? req;
        try { req = raw.Payload.Deserialize<DbConnectRequest>(JsonDefaults.Options); }
        catch { req = null; }

        if (req is null || !req.IsValid())
        {
            await SendAsync(stream, MessageType.DbConnectFail,
                new DbConnectResponse(false, "Malformed or incomplete DbConnectRequest."),
                raw.RequestId, ct);
            return;
        }

        try
        {
            _db?.Dispose();
            _db = null;

            _db = await SqlConnectionFactory.OpenAsync(req, ct);
            await StudentRepository.EnsureSchemaAsync(_db, ct);

            Log($"  DB connected: {req.SqlHost}:{req.SqlPort}/{req.Database}");

            await SendAsync(stream, MessageType.DbConnectOk,
                new DbConnectResponse(true, null), raw.RequestId, ct);
        }
        catch (Exception ex)
        {
            _db?.Dispose();
            _db = null;
            Log($"  [DbConnect Error] {ex.Message}");
            await SendAsync(stream, MessageType.DbConnectFail,
                new DbConnectResponse(false, ex.Message), raw.RequestId, ct);
        }
    }

    private async Task HandleStudentAddAsync(
        NetworkStream stream, RawEnvelope raw, CancellationToken ct)
    {
        if (_db is null)
        {
            await SendAsync(stream, MessageType.StudentAddFail,
                SimpleResponse.Fail("No active database connection. Send DbConnect first."),
                raw.RequestId, ct);
            return;
        }

        StudentAddRequest? req;
        try { req = raw.Payload.Deserialize<StudentAddRequest>(JsonDefaults.Options); }
        catch { req = null; }

        if (req is null || !req.IsValid())
        {
            await SendAsync(stream, MessageType.StudentAddFail,
                SimpleResponse.Fail("Invalid StudentAddRequest: check fields and score range [0, 10]."),
                raw.RequestId, ct);
            return;
        }

        try
        {
            // Doubles use InvariantCulture so they round-trip correctly on any server locale.
            var row = new EncryptedStudentRow
            {
                StudentId = req.StudentId,
                FullNameEnc = _crypto.EncryptString(req.FullName),
                MathEnc = _crypto.EncryptString(
                                    req.Math.ToString(CultureInfo.InvariantCulture)),
                LiteratureEnc = _crypto.EncryptString(
                                    req.Literature.ToString(CultureInfo.InvariantCulture)),
                EnglishEnc = _crypto.EncryptString(
                                    req.English.ToString(CultureInfo.InvariantCulture)),
            };

            await StudentRepository.UpsertEncryptedAsync(_db, row, ct);

            Log($"  StudentAdd OK: {req.StudentId} - {req.FullName}");
            await SendAsync(stream, MessageType.StudentAddOk,
                SimpleResponse.Ok(), raw.RequestId, ct);
        }
        catch (Exception ex)
        {
            Log($"  [StudentAdd Error] {ex.Message}");
            await SendAsync(stream, MessageType.StudentAddFail,
                SimpleResponse.Fail(ex.Message), raw.RequestId, ct);
        }
    }

    private async Task HandleResultsGetAsync(
        NetworkStream stream, RawEnvelope raw, CancellationToken ct)
    {
        if (_db is null)
        {
            Log("  [ResultsGet] No DB connection - returning empty list.");
            await SendAsync(stream, MessageType.Results,
                new List<StudentResultDto>(), raw.RequestId, ct);
            return;
        }

        ResultsGetRequest? req;
        try { req = raw.Payload.Deserialize<ResultsGetRequest>(JsonDefaults.Options); }
        catch { req = null; }

        if (req is null || !req.IsValid())
        {
            Log("  [ResultsGet] Malformed request - returning empty list.");
            await SendAsync(stream, MessageType.Results,
                new List<StudentResultDto>(), raw.RequestId, ct);
            return;
        }

        try
        {
            List<EncryptedStudentRow> rows;

            if (req.Mode == ResultsMode.All)
            {
                rows = await StudentRepository.GetAllAsync(_db, ct);
            }
            else // BY_ID
            {
                var single = await StudentRepository.GetByStudentIdAsync(
                    _db, req.StudentId!, ct);
                rows = single is null ? [] : [single];
            }

            var results = rows.ConvertAll(DecryptRow);

            Log($"  ResultsGet OK: {results.Count} row(s) returned.");
            await SendAsync(stream, MessageType.Results, results, raw.RequestId, ct);
        }
        catch (Exception ex)
        {
            Log($"  [ResultsGet Error] {ex.Message}");
            await SendAsync(stream, MessageType.Results,
                new List<StudentResultDto>(), raw.RequestId, ct);
        }
    }

    private StudentResultDto DecryptRow(EncryptedStudentRow row)
    {
        string fullName = _crypto.DecryptToString(row.FullNameEnc);
        double math = double.Parse(
                                _crypto.DecryptToString(row.MathEnc),
                                CultureInfo.InvariantCulture);
        double literature = double.Parse(
                                _crypto.DecryptToString(row.LiteratureEnc),
                                CultureInfo.InvariantCulture);
        double english = double.Parse(
                                _crypto.DecryptToString(row.EnglishEnc),
                                CultureInfo.InvariantCulture);

        return StudentResultDto.Create(fullName, row.StudentId, math, literature, english);
    }

    private static Task SendAsync<T>(
        NetworkStream stream,
        MessageType type,
        T payload,
        string requestId,
        CancellationToken ct)
    {
        var envelope = MessageEnvelope.CreateResponse(type, payload, requestId);
        return LengthPrefixedJsonProtocol.WriteAsync(stream, envelope, ct);
    }

    private static void Log(string message)
        => System.Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss.fff}] {message}");
}
