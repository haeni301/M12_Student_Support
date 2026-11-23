/*
 M12 v2 SQL Server permissions demo. RUN AS ADMIN ONLY AFTER FIRST SCHEMA CREATION.
 Example password below is a PLACEHOLDER. Supply your own unique password using SSMS;
 never copy it into source-controlled appsettings. Do NOT use sa / db_owner for app.
 Review your application's deployment architecture before adopting this for production.
*/
USE master;
GO
IF SUSER_ID(N'm12_app_login') IS NULL
    CREATE LOGIN m12_app_login WITH PASSWORD = 'REPLACE-WITH-UNIQUE-LAB-SECRET-2026!!', CHECK_POLICY = ON;
GO
USE M12_StudentSupport;
GO
IF USER_ID(N'm12_app_user') IS NULL CREATE USER m12_app_user FOR LOGIN m12_app_login;
IF DATABASE_PRINCIPAL_ID(N'm12_app_role') IS NULL CREATE ROLE m12_app_role;
IF NOT EXISTS(SELECT 1 FROM sys.database_role_members rm
              JOIN sys.database_principals r ON r.principal_id=rm.role_principal_id
              JOIN sys.database_principals u ON u.principal_id=rm.member_principal_id
              WHERE r.name=N'm12_app_role' AND u.name=N'm12_app_user')
    ALTER ROLE m12_app_role ADD MEMBER m12_app_user;
GO
GRANT SELECT ON dbo.Users TO m12_app_role;
GRANT UPDATE (FailedAttempts,LockoutUntilUtc,PasswordHash) ON dbo.Users TO m12_app_role;
DENY UPDATE (Role,UserName,FullName,DepartmentId,StudentCode,IsActive) ON dbo.Users TO m12_app_role;
DENY DELETE ON dbo.Users TO m12_app_role;
GRANT SELECT ON dbo.StudentProfiles TO m12_app_role;
GRANT SELECT ON dbo.Departments TO m12_app_role;
GRANT SELECT, INSERT, UPDATE ON dbo.Tickets TO m12_app_role;
DENY DELETE ON dbo.Tickets TO m12_app_role;
GRANT SELECT, INSERT ON dbo.Comments TO m12_app_role;
GRANT SELECT, INSERT ON dbo.TicketHistories TO m12_app_role;
GRANT SELECT, INSERT ON dbo.TicketAttachments TO m12_app_role;
GRANT SELECT, INSERT ON dbo.Audits TO m12_app_role;
DENY ALTER ON SCHEMA::dbo TO m12_app_role;
GO
/* CHECK permissions - expected deny on destructive operations. */
EXECUTE AS USER='m12_app_user';
SELECT TOP(1) Id, Title, Status FROM dbo.Tickets;
BEGIN TRY DELETE FROM dbo.Tickets WHERE Id=-1; END TRY
BEGIN CATCH SELECT ERROR_NUMBER() AS ExpectedDenyCode, ERROR_MESSAGE() AS ExpectedDenyMessage; END CATCH;
BEGIN TRY UPDATE dbo.Users SET Role='Admin' WHERE Id=-1; END TRY
BEGIN CATCH SELECT ERROR_NUMBER() AS ExpectedDenyCode, ERROR_MESSAGE() AS ExpectedDenyMessage; END CATCH;
REVERT;
GO
/*
 SECURITY LIMITS:
 - A shared SQL app login still has broad row visibility. Business ownership is enforced in ASP.NET.
 - Users.PasswordHash is readable to that login for authentication. Production can isolate credential
   verification in a narrowly scoped procedure / separate identity service.
 - Do not enable the app SQL login for DDL/EnsureCreated; initialize schema as administrator first.
 - Recommended future work: SQL Server Row-Level Security with trusted per-request session context,
   dedicated append-only audit pipeline and separate DB principals by service.
*/
