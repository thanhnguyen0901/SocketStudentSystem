using Microsoft.Data.SqlClient;
using Student.Shared.DTOs;
using Student.Shared.Enums;
using Student.Shared.Helpers;
using Student.Shared.Messages;
using StudentServer.Console.Crypto;
using StudentServer.Console.Data;
using StudentServer.Console.Logging;
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
        Logger.Info("Session", "Client connected.", conn: _endpoint);

        try
        {
            await using NetworkStream stream = _client.GetStream();

            while (!ct.IsCancellationRequested)
                await HandleNextMessageAsync(stream, ct);
        }
        catch (IOException)
        {
            Logger.Info("Session", "Client disconnected (EOF/reset).", conn: _endpoint);
        }
        catch (OperationCanceledException)
        {
            Logger.Info("Session", "Session cancelled (server shutdown).", conn: _endpoint);
        }
        catch (Exception ex)
        {
            Logger.Error("Session", "Unhandled exception in session.", conn: _endpoint, ex: ex);
        }
        finally
        {
            _db?.Dispose();
            _client.Close();
            Logger.Info("Session", "Connection closed.", conn: _endpoint);
        }
    }

    // Reads one framed message and routes it to the correct handler.
    private async Task HandleNextMessageAsync(NetworkStream stream, CancellationToken ct)
    {
        var raw = await LengthPrefixedJsonProtocol.ReadAsync<RawEnvelope>(stream, ct);
        Logger.Debug("MessageRouter", $"<< Type={raw.Type}", conn: _endpoint, reqId: raw.RequestId);

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
                Logger.Warn("MessageRouter", $"Unknown message type '{raw.Type}' — ignored.", conn: _endpoint, reqId: raw.RequestId);
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

            Logger.Info("Db", $"Connected to {req.SqlHost}:{req.SqlPort}/{req.Database}.", conn: _endpoint, reqId: raw.RequestId);

            await SendAsync(stream, MessageType.DbConnectOk,
                new DbConnectResponse(true, null), raw.RequestId, ct);
        }
        catch (Exception ex)
        {
            _db?.Dispose();
            _db = null;
            // Log connection context (host/db only) — never log username or password.
            Logger.Error("Db", $"DbConnect failed for {req.SqlHost}:{req.SqlPort}/{req.Database}.", conn: _endpoint, reqId: raw.RequestId, ex: ex);
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

            Logger.Info("Session", $"StudentAdd OK: id={req.StudentId}.", conn: _endpoint, reqId: raw.RequestId);
            await SendAsync(stream, MessageType.StudentAddOk,
                SimpleResponse.Ok(), raw.RequestId, ct);
        }
        catch (Exception ex)
        {
            Logger.Error("Session", "StudentAdd failed.", conn: _endpoint, reqId: raw.RequestId, ex: ex);
            await SendAsync(stream, MessageType.StudentAddFail,
                SimpleResponse.Fail(ex.Message), raw.RequestId, ct);
        }
    }

    private async Task HandleResultsGetAsync(
        NetworkStream stream, RawEnvelope raw, CancellationToken ct)
    {
        // Guard: database must be connected before any results query.
        if (_db is null)
        {
            Logger.Warn("Session", "ResultsGet: no active DB connection.", conn: _endpoint, reqId: raw.RequestId);
            await SendAsync(stream, MessageType.ResultsFail,
                new ResultsGetError(
                    ErrorCode: "NO_DB_CONNECTION",
                    Message: "No active database connection. Send DbConnect first."),
                raw.RequestId, ct);
            return;
        }

        ResultsGetRequest? req;
        try { req = raw.Payload.Deserialize<ResultsGetRequest>(JsonDefaults.Options); }
        catch { req = null; }

        // Guard: request must parse correctly and satisfy IsValid() (mode + studentId check).
        if (req is null || !req.IsValid())
        {
            Logger.Warn("Session", $"ResultsGet: invalid request — mode='{req?.Mode}' studentId='{req?.StudentId}'.", conn: _endpoint, reqId: raw.RequestId);
            await SendAsync(stream, MessageType.ResultsFail,
                new ResultsGetError(
                    ErrorCode: "INVALID_REQUEST",
                    Message: req?.Mode == ResultsMode.ById && string.IsNullOrWhiteSpace(req.StudentId)
                        ? "Mode BY_ID requires a non-empty StudentId."
                        : "Malformed ResultsGetRequest: check Mode and StudentId fields."),
                raw.RequestId, ct);
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

            Logger.Info("Session", $"ResultsGet OK: {results.Count} row(s).", conn: _endpoint, reqId: raw.RequestId);
            await SendAsync(stream, MessageType.Results, results, raw.RequestId, ct);
        }
        catch (Exception ex)
        {
            Logger.Error("Session", "ResultsGet failed.", conn: _endpoint, reqId: raw.RequestId, ex: ex);
            await SendAsync(stream, MessageType.ResultsFail,
                new ResultsGetError(
                    ErrorCode: "INTERNAL_ERROR",
                    Message: ex.Message),
                raw.RequestId, ct);
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

}
