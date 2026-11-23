/* Optional SQL Server Audit demonstration. Requires appropriate SQL edition/permissions.
   BEFORE RUN: create C:\M12_Audit\ and grant SQL Server service account write permission.
   Audit files may contain security-sensitive metadata: secure the folder against normal users.
   RUN IN SSMS AS SQL SERVER ADMIN. Do not run as app login.
*/
USE master;
GO
IF NOT EXISTS (SELECT 1 FROM sys.server_audits WHERE name=N'M12_DatabaseAudit')
    EXEC(N'CREATE SERVER AUDIT M12_DatabaseAudit TO FILE (FILEPATH = ''C:\M12_Audit\'', MAXSIZE = 50 MB, MAX_ROLLOVER_FILES = 5) WITH (ON_FAILURE = CONTINUE)');
GO
IF NOT EXISTS (SELECT 1 FROM sys.server_audit_specifications WHERE name=N'M12_FailedLoginAudit')
    EXEC(N'CREATE SERVER AUDIT SPECIFICATION M12_FailedLoginAudit FOR SERVER AUDIT M12_DatabaseAudit ADD (FAILED_LOGIN_GROUP) WITH (STATE = ON)');
GO
ALTER SERVER AUDIT M12_DatabaseAudit WITH (STATE = ON);
GO
USE M12_StudentSupport;
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_audit_specifications WHERE name=N'M12_SensitiveTableAudit')
    EXEC(N'CREATE DATABASE AUDIT SPECIFICATION M12_SensitiveTableAudit FOR SERVER AUDIT M12_DatabaseAudit ADD (SELECT ON OBJECT::dbo.Users BY public), ADD (UPDATE ON OBJECT::dbo.Users BY public) WITH (STATE = ON)');
GO
-- After running a controlled test, show recorded database/server events:
SELECT TOP(60) event_time, action_id, succeeded, server_principal_name, database_name, object_name
FROM sys.fn_get_audit_file('C:\M12_Audit\*.sqlaudit', DEFAULT, DEFAULT)
ORDER BY event_time DESC;
GO
