-- =============================================================================
-- create_db.sql
-- Idempotent setup script for SocketStudentSystemDb.
-- Run once against any SQL Server instance before starting the server.
-- Compatible with SQL Server 2016+ and Azure SQL.
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1. Create the database (skip if it already exists).
-- -----------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1
    FROM   sys.databases
    WHERE  name = N'SocketStudentSystemDb'
)
BEGIN
    CREATE DATABASE SocketStudentSystemDb;
END
GO

USE SocketStudentSystemDb;
GO

-- -----------------------------------------------------------------------------
-- 2. Create the encrypted student records table (skip if it already exists).
-- -----------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1
    FROM   sys.objects
    WHERE  object_id = OBJECT_ID(N'dbo.StudentsEncrypted')
      AND  type      = N'U'          -- 'U' = user table
)
BEGIN
    CREATE TABLE dbo.StudentsEncrypted
    (
        -- Surrogate primary key; auto-incremented by SQL Server.
        Id              INT            IDENTITY(1,1)  NOT NULL,

        -- Plain-text student identifier kept as the de-duplication key so
        -- the server can perform UPSERT without decrypting every row first.
        StudentId       NVARCHAR(50)                  NOT NULL,

        -- DES-encrypted ciphertext columns.  VARBINARY(MAX) is used because
        -- DES output length depends on padding and is not known at design time.
        FullNameEnc     VARBINARY(MAX)                NOT NULL,
        MathEnc         VARBINARY(MAX)                NOT NULL,
        LiteratureEnc   VARBINARY(MAX)                NOT NULL,
        EnglishEnc      VARBINARY(MAX)                NOT NULL,

        -- Audit timestamp; stored in UTC so it is time-zone-agnostic.
        CreatedAt       DATETIME2      NOT NULL  DEFAULT SYSUTCDATETIME(),

        -- Constraints
        CONSTRAINT PK_StudentsEncrypted        PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_StudentsEncrypted_StdId  UNIQUE (StudentId)
    );
END
GO
