/*
 M12 Student Support v2: preserve existing v1 SQL Server database.
 1. BACK UP M12_StudentSupport before running.
 2. Close/stop ASP.NET app.
 3. Open in SSMS with an account allowed to ALTER schema; select correct instance.
 4. Run once. The checks make rerun mostly idempotent.
 5. Restart app with DEMO_SEED=true IN DEVELOPMENT ONLY.
 This upgrade intentionally does NOT delete old data or forcibly change existing ticket statuses.
*/
USE [M12_StudentSupport];
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    IF OBJECT_ID(N'dbo.StudentProfiles', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.StudentProfiles (
            StudentCode NVARCHAR(20) NOT NULL PRIMARY KEY,
            FullName NVARCHAR(120) NOT NULL,
            ClassCode NVARCHAR(40) NOT NULL,
            Program NVARCHAR(120) NOT NULL,
            IsActive BIT NOT NULL DEFAULT(1)
        );
    END;

    -- Existing database v1 has StudentCode as nvarchar(max), incompatible with FK.
    IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types ty ON ty.user_type_id=c.user_type_id
               WHERE c.object_id=OBJECT_ID(N'dbo.Users') AND c.name=N'StudentCode'
                 AND (c.max_length = -1 OR c.max_length > 40))
        ALTER TABLE dbo.Users ALTER COLUMN StudentCode NVARCHAR(20) NULL;

    INSERT INTO dbo.StudentProfiles(StudentCode, FullName, ClassCode, Program, IsActive)
    SELECT DISTINCT u.StudentCode, u.FullName, N'CHƯA XÁC NHẬN', N'Hồ sơ chuyển từ M12 v1', 1
    FROM dbo.Users u
    WHERE u.StudentCode IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.StudentProfiles p WHERE p.StudentCode = u.StudentCode);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Users_StudentProfiles_StudentCode')
        ALTER TABLE dbo.Users WITH CHECK ADD CONSTRAINT FK_Users_StudentProfiles_StudentCode
        FOREIGN KEY (StudentCode) REFERENCES dbo.StudentProfiles(StudentCode);

    IF COL_LENGTH(N'dbo.Tickets', N'StudentCode') IS NULL
        ALTER TABLE dbo.Tickets ADD StudentCode NVARCHAR(20) NULL;
    IF COL_LENGTH(N'dbo.Tickets', N'ResolutionSummary') IS NULL
        ALTER TABLE dbo.Tickets ADD ResolutionSummary NVARCHAR(1500) NULL;
    IF COL_LENGTH(N'dbo.Tickets', N'ClosedUtc') IS NULL
        ALTER TABLE dbo.Tickets ADD ClosedUtc DATETIME2 NULL;
    IF COL_LENGTH(N'dbo.Tickets', N'RowVersion') IS NULL
        ALTER TABLE dbo.Tickets ADD RowVersion ROWVERSION NOT NULL;

    EXEC(N'UPDATE t SET StudentCode = u.StudentCode
         FROM dbo.Tickets t JOIN dbo.Users u ON u.Id = t.StudentId
         WHERE t.StudentCode IS NULL AND u.StudentCode IS NOT NULL');

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Tickets_StudentProfiles_StudentCode')
        ALTER TABLE dbo.Tickets WITH CHECK ADD CONSTRAINT FK_Tickets_StudentProfiles_StudentCode
        FOREIGN KEY(StudentCode) REFERENCES dbo.StudentProfiles(StudentCode);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Tickets_Departments_DepartmentId')
        ALTER TABLE dbo.Tickets WITH CHECK ADD CONSTRAINT FK_Tickets_Departments_DepartmentId
        FOREIGN KEY(DepartmentId) REFERENCES dbo.Departments(Id);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Tickets_Users_AssignedStaffId')
        ALTER TABLE dbo.Tickets WITH CHECK ADD CONSTRAINT FK_Tickets_Users_AssignedStaffId
        FOREIGN KEY(AssignedStaffId) REFERENCES dbo.Users(Id);

    IF OBJECT_ID(N'dbo.TicketHistories', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.TicketHistories (
            Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            TicketId INT NOT NULL,
            ActorId INT NOT NULL,
            [Event] NVARCHAR(32) NOT NULL,
            FromStatus NVARCHAR(30) NULL,
            ToStatus NVARCHAR(30) NULL,
            FromDepartmentId INT NULL,
            ToDepartmentId INT NULL,
            FromStaffId INT NULL,
            ToStaffId INT NULL,
            Note NVARCHAR(400) NULL,
            CreatedUtc DATETIME2 NOT NULL,
            CONSTRAINT FK_TicketHistories_Tickets_TicketId FOREIGN KEY(TicketId) REFERENCES dbo.Tickets(Id) ON DELETE CASCADE,
            CONSTRAINT FK_TicketHistories_Users_ActorId FOREIGN KEY(ActorId) REFERENCES dbo.Users(Id)
        );
    END;

    IF OBJECT_ID(N'dbo.TicketAttachments', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.TicketAttachments (
            Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            TicketId INT NOT NULL,
            UploadedById INT NOT NULL,
            OriginalName NVARCHAR(180) NOT NULL,
            StoredName NVARCHAR(80) NOT NULL,
            ContentType NVARCHAR(80) NOT NULL,
            FileSize BIGINT NOT NULL,
            CreatedUtc DATETIME2 NOT NULL,
            CONSTRAINT FK_TicketAttachments_Tickets_TicketId FOREIGN KEY(TicketId) REFERENCES dbo.Tickets(Id) ON DELETE CASCADE,
            CONSTRAINT FK_TicketAttachments_Users_UploadedById FOREIGN KEY(UploadedById) REFERENCES dbo.Users(Id)
        );
    END;

    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name = 'IX_Tickets_Status_Priority' AND object_id = OBJECT_ID('dbo.Tickets'))
        CREATE INDEX IX_Tickets_Status_Priority ON dbo.Tickets(Status, Priority);
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name = 'IX_TicketHistories_TicketId' AND object_id = OBJECT_ID('dbo.TicketHistories'))
        CREATE INDEX IX_TicketHistories_TicketId ON dbo.TicketHistories(TicketId);
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name = 'IX_TicketHistories_ActorId' AND object_id = OBJECT_ID('dbo.TicketHistories'))
        CREATE INDEX IX_TicketHistories_ActorId ON dbo.TicketHistories(ActorId);
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name = 'IX_TicketAttachments_TicketId' AND object_id = OBJECT_ID('dbo.TicketAttachments'))
        CREATE INDEX IX_TicketAttachments_TicketId ON dbo.TicketAttachments(TicketId);
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name = 'IX_TicketAttachments_UploadedById' AND object_id = OBJECT_ID('dbo.TicketAttachments'))
        CREATE INDEX IX_TicketAttachments_UploadedById ON dbo.TicketAttachments(UploadedById);

    COMMIT TRANSACTION;
    PRINT 'M12 V2 SCHEMA UPGRADE: COMPLETED';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME IN ('StudentProfiles','TicketHistories','TicketAttachments');
SELECT TOP(5) Id, StudentId, StudentCode, DepartmentId, RowVersion FROM dbo.Tickets ORDER BY Id DESC;
GO
