namespace Student.Shared.Enums;

public enum MessageType : byte
{
    // Connection handshake
    DbConnect = 1,
    DbConnectOk = 2,
    DbConnectFail = 3,

    // Student operations
    StudentAdd = 10,
    StudentAddOk = 11,
    StudentAddFail = 12,

    // Results queries
    ResultsGet = 20,
    Results = 21,
    ResultsFail = 22,
}