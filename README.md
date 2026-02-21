# SocketStudentSystem

<div align="center">

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![C#](https://img.shields.io/badge/C%23-12-239120?logo=csharp)
![WPF](https://img.shields.io/badge/UI-WPF%20%2B%20Caliburn.Micro-0078D7?logo=windows)
![Platform](https://img.shields.io/badge/Platform-Windows-0078D7?logo=windows)

**Network Programming Assignment — TCP Socket Client–Server Application**

</div>

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Architecture Diagram](#2-architecture-diagram)
3. [Technology Stack](#3-technology-stack)
4. [Solution Structure](#4-solution-structure)
5. [TCP Communication Design](#5-tcp-communication-design)
6. [Database Design](#6-database-design)
7. [DES Encryption Strategy](#7-des-encryption-strategy)
8. [How to Run](#8-how-to-run)
9. [Demo Flow](#9-demo-flow)
10. [Key Learning Objectives](#10-key-learning-objectives)
11. [Future Improvements](#11-future-improvements)
12. [Author](#12-author)

---

## 1. Project Overview

**SocketStudentSystem** is a client–server application built on raw TCP sockets using C# .NET 8.  
It demonstrates the full lifecycle of a networked data-management system:

- The **client** (WPF desktop app) connects to the server, sends SQL Server credentials, submits student score records, and displays computed results.
- The **server** (console app) accepts multiple concurrent clients, establishes its own SQL Server connection, encrypts sensitive data with **DES** before persisting it, and returns decrypted, computed results on demand.

| Aspect      | Detail                                                  |
| :---------- | :------------------------------------------------------ |
| Transport   | TCP/IP raw sockets (`TcpListener` / `TcpClient`)        |
| Framing     | 4-byte length-prefix + UTF-8 JSON payload               |
| Storage     | Microsoft SQL Server                                    |
| Encryption  | DES (Data Encryption Standard) — symmetric, per-record  |
| UI Pattern  | MVVM via Caliburn.Micro                                 |
| Language    | C# 12 / .NET 8                                          |

---

## 2. Architecture Diagram

```
  ┌─────────────────────────────────────────────────────────────────┐
  │  CLIENT MACHINE                                                 │
  │                                                                 │
  │  ┌───────────────────────────────────────────────────────────┐  │
  │  │  StudentClient.Wpf   (WPF · Caliburn.Micro MVVM)          │  │
  │  │                                                           │  │
  │  │  ShellView  ──►  ShellViewModel  ──►  ConnectionService   │  │
  │  │                                              │            │  │
  │  │                              ┌───────────────┴─────────┐  │  │
  │  │                              │  LengthPrefixedJson     │  │  │
  │  │                              │  MessageWriter / Reader │  │  │
  │  │                              └─────────────────────────┘  │  │
  │  └───────────────────────────────────────────────────────────┘  │
  └──────────────────────────────────────┬──────────────────────────┘
                                         │
                              ┌──────────┴───────────┐
                              │    TCP (port 9000)   │
                              │  4-byte · UTF-8 JSON │
                              └──────────┬───────────┘
                                         │
  ┌──────────────────────────────────────┴──────────────────────────┐
  │  SERVER MACHINE                                                 │
  │                                                                 │
  │  ┌───────────────────────────────────────────────────────────┐  │
  │  │  StudentServer.Console   (TCP Server)                     │  │
  │  │                                                           │  │
  │  │  TcpListener  ──►  AcceptTcpClientAsync                   │  │
  │  │                              │                            │  │
  │  │                     ClientSession  (Task / client)        │  │
  │  │                              │                            │  │
  │  │               ┌──────────────┴──────────────┐             │  │
  │  │               ▼                             ▼             │  │
  │  │      MessageDispatcher              DesEncryptor          │  │
  │  │               │                             │             │  │
  │  │               └──────────────┬──────────────┘             │  │
  │  │                              ▼                            │  │
  │  │                       StudentService                      │  │
  │  │                              │                            │  │
  │  │                       StudentRepository                   │  │
  │  │                       (ADO.NET · SqlClient)               │  │
  │  └──────────────────────────────┬────────────────────────────┘  │
  └─────────────────────────────────┴───────────────────────────────┘
                                    │
              ┌─────────────────────▼──────────────────────┐
              │         SQL Server  (StudentDb)            │
              │                                            │
              │   ┌─────────────────────────────────────┐  │
              │   │             Students                │  │
              │   ├─────────────────────────────────────┤  │
              │   │  StudentId    NVARCHAR(50)          │  │
              │   │  FullName     VARBINARY(256)  [DES] │  │
              │   │  Math         VARBINARY(64)   [DES] │  │
              │   │  Literature   VARBINARY(64)   [DES] │  │
              │   │  English      VARBINARY(64)   [DES] │  │
              │   │  CreatedAt    DATETIMEOFFSET        │  │
              │   └─────────────────────────────────────┘  │
              └────────────────────────────────────────────┘

  ╔═════════════════════════════════════════════════════════════════╗
  ║                  Student.Shared  (net8.0)                       ║
  ║                                                                 ║
  ║  ┌─────────────────────────────────────────────────────────┐    ║
  ║  │  Enums    /  MessageType                                │    ║
  ║  │  Messages /  MessageEnvelope<T>                         │    ║
  ║  │  DTOs     /  DbConnect  ·  Student  ·  Results          │    ║
  ║  └─────────────────────────────────────────────────────────┘    ║
  ║        Referenced by  StudentServer  and  StudentClient         ║
  ╚═════════════════════════════════════════════════════════════════╝
```

---

## 3. Technology Stack

| Layer            | Technology                                                    |
| :--------------- | :------------------------------------------------------------ |
| Runtime          | .NET 8 (C# 12)                                                |
| Server           | `System.Net.Sockets.TcpListener`                              |
| Client UI        | WPF + Caliburn.Micro 5.x (MVVM)                               |
| Serialization    | `System.Text.Json` (camelCase, enum-as-string)                |
| Database Access  | `Microsoft.Data.SqlClient` 6.x                                |
| Encryption       | `System.Security.Cryptography.DES`                            |
| Async Model      | `Task` / `async-await` + `CancellationToken`                  |
| Build            | .NET SDK 8 · `Directory.Build.props` for solution-wide config |

---

## 4. Solution Structure

```
SocketStudentSystem/
│
├── Directory.Build.props            # Solution-wide: LangVersion=latest, Nullable=enable
├── SocketStudentSystem.sln
│
├── Student.Shared/                  # Class library – shared contracts
│   ├── DTOs/
│   │   ├── DbConnectDtos.cs         # DbConnectRequest / DbConnectResponse
│   │   ├── StudentDtos.cs           # StudentAddRequest / StudentAddResponse
│   │   └── ResultsDtos.cs          # ResultsGetRequest / StudentResultDto
│   ├── Enums/
│   │   └── MessageType.cs           # DbConnect, StudentAdd, ResultsGet …
│   └── Messages/
│       └── MessageEnvelope.cs       # MessageEnvelope<T> + factory helpers
│
├── StudentServer.Console/           # Console app – TCP server
│   ├── Crypto/
│   │   └── DesEncryptor.cs          # DES encrypt / decrypt helpers
│   ├── Data/
│   │   └── StudentRepository.cs     # ADO.NET SQL Server access
│   ├── Networking/
│   │   ├── FramingJsonOptions.cs    # Shared JsonSerializerOptions
│   │   ├── LengthPrefixedJsonMessageReader.cs
│   │   ├── LengthPrefixedJsonMessageWriter.cs
│   │   └── ClientSession.cs         # Per-client async session handler
│   ├── Services/
│   │   └── StudentService.cs        # Business logic: encrypt → store → query
│   └── Program.cs                   # TcpListener accept loop
│
└── StudentClient.Wpf/               # WPF app – desktop client
    ├── Services/
    │   ├── FramingJsonOptions.cs
    │   ├── LengthPrefixedJsonMessageReader.cs
    │   ├── LengthPrefixedJsonMessageWriter.cs
    │   └── ConnectionService.cs     # TcpClient wrapper
    ├── ViewModels/
    │   └── ShellViewModel.cs        # Root Caliburn Screen
    ├── Views/
    │   └── ShellView.xaml           # Connection + student input UI
    ├── Bootstrapper.cs              # Caliburn.Micro IoC bootstrap
    └── App.xaml                     # No StartupUri; bootstrapper as resource
```

---

## 5. TCP Communication Design

### 5.1 Length-Prefix Framing

Every message is wrapped in a two-part frame to handle TCP stream fragmentation:

```
┌─────────────────────────┬──────────────────────────────────────────┐
│  Header (4 bytes)       │  Payload (N bytes)                       │
│  Int32, little-endian   │  UTF-8 encoded JSON                      │
│  value = N              │  (serialized MessageEnvelope<T>)         │
└─────────────────────────┴──────────────────────────────────────────┘
```

The reader loops until all `N` bytes arrive, guarding against partial TCP reads:

```csharp
while (totalRead < buffer.Length)
{
    int n = await stream.ReadAsync(buffer, totalRead, buffer.Length - totalRead, ct);
    if (n == 0) throw new EndOfStreamException("Client disconnected.");
    totalRead += n;
}
```

### 5.2 JSON Message Envelope

```json
{
  "type": "studentAdd",
  "requestId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "timestamp": "2026-02-21T08:00:00+00:00",
  "payload": {
    "fullName": "Nguyen Van A",
    "studentId": "SV001",
    "math": 8.5,
    "literature": 7.0,
    "english": 9.0
  }
}
```

Responses carry the same `requestId` so the client can correlate:

```json
{
  "type": "studentAddOk",
  "requestId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "timestamp": "2026-02-21T08:00:01+00:00",
  "payload": { "success": true, "errorMessage": null }
}
```

### 5.3 Message Types

| `MessageType`    | Direction        | Description                          |
| :--------------- | :--------------- | :----------------------------------- |
| `DbConnect`      | Client → Server  | Send SQL Server credentials          |
| `DbConnectOk`    | Server → Client  | Connection established               |
| `DbConnectFail`  | Server → Client  | Connection failed (includes error)   |
| `StudentAdd`     | Client → Server  | Submit a student record              |
| `StudentAddOk`   | Server → Client  | Record persisted successfully        |
| `StudentAddFail` | Server → Client  | Persistence failed (includes error)  |
| `ResultsGet`     | Client → Server  | Request results (`ALL` or `BY_ID`)   |
| `Results`        | Server → Client  | Returns `List<StudentResultDto>`     |

---

## 6. Database Design

### Connection

SQL Server (any edition — Developer / Express is sufficient).  
The client transmits the connection string components; the server builds it server-side.

### Table: `Students`

```sql
CREATE TABLE Students (
    Id          INT           IDENTITY(1,1) PRIMARY KEY,
    StudentId   NVARCHAR(50)  NOT NULL UNIQUE,
    FullName    VARBINARY(256) NOT NULL,   -- DES-encrypted
    Math        VARBINARY(64)  NOT NULL,   -- DES-encrypted
    Literature  VARBINARY(64)  NOT NULL,   -- DES-encrypted
    English     VARBINARY(64)  NOT NULL,   -- DES-encrypted
    CreatedAt   DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
);
```

> All text/numeric columns that contain student data are stored as `VARBINARY`
> to hold the raw DES cipher bytes. The server decrypts them on the fly when
> computing averages or returning a results list.

---

## 7. DES Encryption Strategy

| Property          | Value                                              |
| :---------------- | :------------------------------------------------- |
| Algorithm         | DES (CBC mode)                                     |
| Key size          | 64-bit (8 bytes)                                   |
| IV                | Random per-record or fixed per-session             |
| Encrypted fields  | `FullName`, `Math`, `Literature`, `English`        |
| Key storage       | Server-side only — never transmitted to the client |

### Encrypt / Decrypt Helper (outline)

```csharp
// Encrypt a UTF-8 string to a base-64 or raw byte[] under a given key+IV
byte[] Encrypt(string plaintext, byte[] key, byte[] iv);

// Decrypt raw cipher bytes back to a UTF-8 string
string Decrypt(byte[] ciphertext, byte[] key, byte[] iv);
```

> **Security note:** DES is used here to satisfy the assignment's cryptography
> requirement. In production, AES-256-GCM is the recommended replacement.

---

## 8. How to Run

### Prerequisites

| Tool                    | Minimum Version               |
| :---------------------- | :---------------------------- |
| .NET SDK                | 8.0                           |
| SQL Server              | 2019 Express or higher        |
| Visual Studio / VS Code | Optional (CLI is sufficient)  |

### 8.1 — Setup SQL Server

```sql
-- Create the application database
CREATE DATABASE StudentDb;
GO

USE StudentDb;
GO

CREATE TABLE Students (
    Id          INT            IDENTITY(1,1) PRIMARY KEY,
    StudentId   NVARCHAR(50)   NOT NULL UNIQUE,
    FullName    VARBINARY(256) NOT NULL,
    Math        VARBINARY(64)  NOT NULL,
    Literature  VARBINARY(64)  NOT NULL,
    English     VARBINARY(64)  NOT NULL,
    CreatedAt   DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
);
GO
```

### 8.2 — Run the Server

```powershell
# From the solution root
dotnet run --project StudentServer.Console               # listens on port 9000

# Custom port
dotnet run --project StudentServer.Console -- 5000
```

Expected console output:

```
[08:00:00.000] Server listening on port 9000. Press Ctrl+C to stop.
[08:00:05.123] Client connected from 127.0.0.1:52341.
[08:00:05.200]   <- [127.0.0.1:52341] Type=DbConnect | RequestId=3fa85f64...
```

### 8.3 — Run the Client

```powershell
dotnet run --project StudentClient.Wpf
```

Or press **F5** inside Visual Studio with `StudentClient.Wpf` set as the startup project.

---

## 9. Demo Flow

```
CLIENT                                           SERVER
  │                                                 │
  │──── TCP Connect (port 9000) ───────────────────►│
  │                                                 │
  │──── DbConnect { host, port, user, pass, db } ──►│
  │◄─── DbConnectOk ────────────────────────────────│  server opens SqlConnection
  │                                                 │
  │──── StudentAdd { SV001, Nguyen Van A, ... } ───►│
  │                                                 │  DES-encrypt fields
  │                                                 │  INSERT INTO Students ...
  │◄─── StudentAddOk ───────────────────────────────│
  │                                                 │
  │──── StudentAdd { SV002, Tran Thi B, ... } ─────►│
  │◄─── StudentAddOk ───────────────────────────────│
  │                                                 │
  │──── ResultsGet { mode: "ALL" } ────────────────►│
  │                                                 │  SELECT * FROM Students
  │                                                 │  DES-decrypt + compute avg
  │◄─── Results [{ SV001, avg: 8.17 }, ...] ────────│
  │                                                 │
  │──── ResultsGet { mode: "BY_ID", "SV001" } ─────►│
  │◄─── Results [{ SV001, avg: 8.17 }] ─────────────│
  │                                                 │
  │  (user closes window)                           │
  │──── TCP FIN ───────────────────────────────────►│
  │                                                 │  session cleaned up
```

---

## 10. Key Learning Objectives

This project covers the following topics from the **Network Programming** university course:

| #  | Objective                                                                                         |
| :- | :------------------------------------------------------------------------------------------------ |
| 1  | **TCP socket lifecycle** — `TcpListener.AcceptTcpClientAsync`, `NetworkStream`, graceful close    |
| 2  | **Custom application-layer protocol** — length-prefix framing, request/response correlation IDs   |
| 3  | **Concurrent client handling** — per-client `Task`, `CancellationToken`, server shutdown drain    |
| 4  | **Serialization over the wire** — `System.Text.Json`, type discriminators, camelCase convention   |
| 5  | **Symmetric encryption** — DES key management, CBC mode, encrypting before DB persistence         |
| 6  | **Database integration** — `Microsoft.Data.SqlClient`, parameterised queries, `VARBINARY` storage |
| 7  | **MVVM pattern in WPF** — Caliburn.Micro conventions, `Screen`, property-change notification      |
| 8  | **Partial-read robustness** — looping `ReadAsync` to reconstruct frames spanning TCP segments     |
| 9  | **Graceful error handling** — per-session isolation, `EndOfStreamException` vs `IOException`      |
| 10 | **.NET 8 project organisation** — multi-project solution, `Directory.Build.props`, shared lib     |

---

## 11. Future Improvements

| Area          | Proposed Improvement                                                                  |
| :------------ | :------------------------------------------------------------------------------------ |
| Security      | Replace DES with **AES-256-GCM**; wrap transport with **TLS** (`SslStream`)          |
| Protocol      | Add a `version` field to `MessageEnvelope` for backward-compatible evolution         |
| Concurrency   | Replace `List<Task>` with `ConcurrentDictionary<string, Task>` keyed by client ID    |
| Performance   | Use `System.IO.Pipelines` (`PipeReader` / `PipeWriter`) for zero-copy framing        |
| Resilience    | Implement reconnect with exponential back-off in `ConnectionService`                 |
| Logging       | Integrate `Microsoft.Extensions.Logging` with a structured sink (e.g. Serilog + Seq) |
| Testing       | Unit-test framing with `MemoryStream` fakes; test DTO validation helpers              |
| Packaging     | Publish server as a **Windows Service** or **Docker container**                       |

---

## 12. Author

| Field        | Detail                               |
| :----------- | :----------------------------------- |
| **Course**   | Network Programming                  |
| **Type**     | TCP Socket Client–Server Application |
| **Platform** | .NET 8 · C# 12 · WPF                 |
| **Year**     | 2026                                 |