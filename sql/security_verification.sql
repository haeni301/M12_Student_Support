/* READ-ONLY checks: screenshot results in SSMS and include in report. */
USE M12_StudentSupport;
GO
SELECT name AS TableName FROM sys.tables
WHERE name IN ('Users','StudentProfiles','Tickets','TicketHistories','TicketAttachments','Audits');
SELECT p.StudentCode, p.FullName, p.ClassCode, u.UserName
FROM dbo.StudentProfiles p LEFT JOIN dbo.Users u ON u.StudentCode = p.StudentCode;
SELECT TOP(20) t.Id, t.StudentCode, t.Status, t.Priority, d.Name AS Department,
       t.AssignedStaffId, t.CreatedUtc
FROM dbo.Tickets t LEFT JOIN dbo.Departments d ON d.Id = t.DepartmentId
ORDER BY t.Id DESC;
SELECT TOP(50) TicketId, [Event], FromStatus, ToStatus, FromDepartmentId,
       ToDepartmentId, ActorId, CreatedUtc FROM dbo.TicketHistories ORDER BY Id DESC;
SELECT TOP(50) ActorId, Action, TicketId, Success, CreatedUtc FROM dbo.Audits ORDER BY Id DESC;
SELECT u.name AS DbUser, r.name AS DbRole FROM sys.database_role_members rm
JOIN sys.database_principals u ON u.principal_id = rm.member_principal_id
JOIN sys.database_principals r ON r.principal_id = rm.role_principal_id
WHERE u.name LIKE N'm12%';
SELECT pr.name, pr.type_desc, pe.permission_name, pe.state_desc,
       OBJECT_NAME(pe.major_id) AS ObjectName FROM sys.database_permissions pe
JOIN sys.database_principals pr ON pr.principal_id = pe.grantee_principal_id
WHERE pr.name = N'm12_app_role';
GO
