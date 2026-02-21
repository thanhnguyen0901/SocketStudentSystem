-- =============================================================================
-- create_db.sql
-- Idempotent setup script for SocketStudentSystemDb.
--
-- PURPOSE
--   Creates the application database and schema required by StudentServer.
--   Safe to run multiple times - all CREATE statements are guarded.
--
-- HOW TO RUN
--   sqlcmd -S <host>,<port> -U <user> -P <password> -i create_db.sql
--   or open in SSMS and execute (F5).
--
-- REQUIRED PERMISSIONS
--   Section 1 (CREATE DATABASE) : sysadmin or dbcreator role on master.
--   Sections 2-4 (schema setup) : db_owner or ddl_admin on SocketStudentSystemDb.
--
-- AZURE SQL NOTE
--   CREATE DATABASE on Azure SQL must run as a separate connection against master,
--   and must use the portal / az cli / T-SQL from master with appropriate edition.
--   If the database already exists on Azure SQL, comment out Section 1 entirely
--   and connect directly to SocketStudentSystemDb before running Sections 2-4.
--
-- COMPATIBILITY
--   SQL Server 2016+ (14.x+), Azure SQL Database (with note above).
-- =============================================================================

-- =============================================================================
-- SECTION 1: Create the database
-- Must run in the master context.
-- CREATE DATABASE cannot be wrapped in a transaction - this is a SQL Server
-- limitation.  It is guarded with an existence check instead.
-- =============================================================================

USE master;
GO

IF NOT EXISTS (
    SELECT 1
    FROM   sys.databases
    WHERE  name = N'SocketStudentSystemDb'
)
BEGIN
    CREATE DATABASE SocketStudentSystemDb;
    PRINT 'Database SocketStudentSystemDb created.';
END
ELSE
    PRINT 'Database SocketStudentSystemDb already exists - skipped.';
GO

-- =============================================================================
-- SECTION 2: Switch context to the application database
-- =============================================================================

USE SocketStudentSystemDb;
GO

-- =============================================================================
-- SECTION 3: Session options
-- SET statements outside a transaction so they apply for the whole session.
--   NOCOUNT     : suppresses "(N row(s) affected)" noise in logs.
--   XACT_ABORT  : automatically rolls back the open transaction on any runtime
--                 error (works in concert with TRY/CATCH in Section 4).
--   ANSI_NULLS / QUOTED_IDENTIFIER : required ON for all objects that may be
--                 indexed or published; matches SQL Server defaults.
-- =============================================================================

SET NOCOUNT          ON;
SET XACT_ABORT       ON;
SET ANSI_NULLS       ON;
SET QUOTED_IDENTIFIER ON;
GO

-- =============================================================================
-- SECTION 4: Schema objects
-- All DDL runs inside a single transaction wrapped in TRY/CATCH.
-- XACT_ABORT ON means any sub-statement error immediately marks the transaction
-- as uncommittable; the CATCH block checks XACT_STATE() before rolling back.
-- =============================================================================

BEGIN TRY
    BEGIN TRANSACTION;

    -- -------------------------------------------------------------------------
    -- 4a. Table: dbo.StudentsEncrypted
    --
    --   Id            - surrogate PK, auto-increment.
    --   StudentId     - plain-text student code; kept unencrypted so the server
    --                   can do MERGE-based UPSERT without decrypting every row.
    --   *Enc columns  - DES-encrypted ciphertext; length varies with padding so
    --                   VARBINARY(MAX) is the correct storage type.
    --   CreatedAt     - insert timestamp (UTC); preserved on UPSERT UPDATE.
    --   UpdatedAt     - last-modified timestamp (UTC); maintained by the server
    --                   on each MERGE UPDATE. [OPTIONAL - see note below]
    --
    -- OPTIONAL - UpdatedAt:
    --   Useful for auditing and change-detection but requires the server's MERGE
    --   to SET UpdatedAt = SYSUTCDATETIME() explicitly on the WHEN MATCHED branch.
    --   The column is added here with a DEFAULT so existing rows are back-filled.
    --   If you do not want it, remove the UpdatedAt lines from both the CREATE
    --   TABLE block and the conditional ALTER below.
    -- -------------------------------------------------------------------------

    IF OBJECT_ID(N'dbo.StudentsEncrypted', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.StudentsEncrypted
        (
            -- Identifiers
            Id              INT             IDENTITY(1,1)   NOT NULL,
            StudentId       NVARCHAR(50)                    NOT NULL,

            -- Encrypted payload columns (DES CBC output; length varies with padding)
            FullNameEnc     VARBINARY(MAX)                  NOT NULL,
            MathEnc         VARBINARY(MAX)                  NOT NULL,
            LiteratureEnc   VARBINARY(MAX)                  NOT NULL,
            EnglishEnc      VARBINARY(MAX)                  NOT NULL,

            -- Audit timestamps (stored in UTC)
            CreatedAt       DATETIME2(3)    NOT NULL  DEFAULT SYSUTCDATETIME(),
            UpdatedAt       DATETIME2(3)    NOT NULL  DEFAULT SYSUTCDATETIME(),  -- [OPTIONAL]

            -- Constraints defined inline with the table for a single DDL statement
            CONSTRAINT PK_StudentsEncrypted             PRIMARY KEY CLUSTERED (Id),
            CONSTRAINT UQ_StudentsEncrypted_StudentId   UNIQUE NONCLUSTERED   (StudentId),
            CONSTRAINT CK_StudentsEncrypted_StudentId   CHECK  (LEN(LTRIM(RTRIM(StudentId))) > 0)
        );

        PRINT 'Table dbo.StudentsEncrypted created.';
    END
    ELSE
    BEGIN
        PRINT 'Table dbo.StudentsEncrypted already exists.';

        -- Upgrade path: add UpdatedAt if this is an older schema without it.
        -- COL_LENGTH returns NULL when the column does not exist.  [OPTIONAL]
        IF COL_LENGTH(N'dbo.StudentsEncrypted', N'UpdatedAt') IS NULL
        BEGIN
            ALTER TABLE dbo.StudentsEncrypted
                ADD UpdatedAt DATETIME2(3) NOT NULL
                    CONSTRAINT DF_StudentsEncrypted_UpdatedAt DEFAULT SYSUTCDATETIME();
            PRINT 'Column UpdatedAt added to dbo.StudentsEncrypted (upgrade).';
        END
        ELSE
            PRINT 'Column UpdatedAt already present - skipped.';

        -- Upgrade path: add CHECK constraint if missing from an older schema.
        IF OBJECT_ID(N'dbo.CK_StudentsEncrypted_StudentId', N'C') IS NULL
        BEGIN
            ALTER TABLE dbo.StudentsEncrypted
                ADD CONSTRAINT CK_StudentsEncrypted_StudentId
                    CHECK (LEN(LTRIM(RTRIM(StudentId))) > 0);
            PRINT 'Constraint CK_StudentsEncrypted_StudentId added (upgrade).';
        END
        ELSE
            PRINT 'Constraint CK_StudentsEncrypted_StudentId already present - skipped.';
    END

    -- -------------------------------------------------------------------------
    -- 4b. Additional index
    --
    -- The UNIQUE constraint on StudentId already creates a unique nonclustered
    -- index (UQ_StudentsEncrypted_StudentId), which covers the BY_ID lookup
    -- query.  No separate IX_ index is needed.
    -- -------------------------------------------------------------------------

    COMMIT TRANSACTION;
    PRINT 'Schema setup completed successfully.';

END TRY
BEGIN CATCH

    -- XACT_STATE() = -1 : transaction is uncommittable (must roll back).
    -- XACT_STATE() =  1 : transaction is active and committable (still roll back
    --                      because we are in an error path).
    -- XACT_STATE() =  0 : no open transaction.
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    -- Capture error details before they go out of scope.
    DECLARE @ErrMsg    NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSev    INT            = ERROR_SEVERITY();
    DECLARE @ErrState  INT            = ERROR_STATE();
    DECLARE @ErrLine   INT            = ERROR_LINE();

    PRINT CONCAT(N'ERROR at line ', @ErrLine, N': ', @ErrMsg);

    -- Re-throw as a proper SQL Server error so the calling script / agent job
    -- sees a failure exit code.
    RAISERROR(@ErrMsg, @ErrSev, @ErrState);

END CATCH;
GO

PRINT 'create_db.sql finished.';
GO
