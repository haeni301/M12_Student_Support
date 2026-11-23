# M12 V2 SQL Express – Fix for `Invalid object name StudentProfiles`

## Why the crash happens

`DemoSeeder.Seed()` first calls `db.Database.EnsureCreatedAsync()` and then queries `StudentProfiles`. `EnsureCreatedAsync()` does not add missing tables to a database which already has unrelated or old tables. The default project settings previously used `Server=localhost;Database=M12_StudentSupport`, which can be different from the SQL Express instance visible in SSMS.

## Recommended (do not delete the old database)

1. Stop running web app with Ctrl+C.
2. Extract this ZIP. In PowerShell, enter the `M12_StudentSupport` directory.
3. Run `powershell -ExecutionPolicy Bypass -File .\RUN_M12_DEMO_SQLEXPRESS.ps1` (or run the commands below manually).
4. The app uses `Server=.\SQLEXPRESS;Database=M12_StudentSupport_V2_Demo_20261118` and `DEMO_SEED=true` in Development mode.
5. The app should create the brand-new database and all model tables, provided your Windows login can create the database and tables. Open https://localhost:50900 and log in with sv001 / Demo@12345 (classroom demo only).
6. In SSMS, connect to the same .\SQLEXPRESS instance and execute:

```sql
USE M12_StudentSupport_V2_Demo_20261118;
SELECT DB_NAME() AS CurrentDB, @@SERVERNAME AS CurrentInstance;
SELECT SCHEMA_NAME(schema_id) AS SchemaName, name FROM sys.tables ORDER BY name;
SELECT OBJECT_ID(N'dbo.StudentProfiles',N'U') AS StudentProfilesId;
```

If that database already has unrelated tables, choose an unused NEW database name in BOTH `appsettings.json` and the launcher; do not drop the existing one.

## Run without the launcher

Open PowerShell in the project folder:

```powershell
$env:ConnectionStrings__Default = 'Server=.\SQLEXPRESS;Database=M12_StudentSupport_V2_Demo_20261118;Trusted_Connection=True;TrustServerCertificate=True;'
dotnet run --launch-profile M12-HTTPS-Demo
```

**Note:** Environment variable `ConnectionStrings__Default` overrides `appsettings.json` during that PowerShell session. If you want to switch back to configuration file settings, remove that variable: `Remove-Item Env:ConnectionStrings__Default -ErrorAction SilentlyContinue`.

## If it still fails

- `CREATE DATABASE permission denied`: in SSMS, create the new database with your administrator connection, grant the application's Windows identity appropriate create-table permissions for demo (not production), then rerun.
- `A network-related ... instance-specific error`: verify SQL Server (SQLEXPRESS) service is running and the instance matches the SSMS server.
- `Invalid object name StudentProfiles`: ensure this specific *new* database was actually targeted. Check PowerShell `Get-ChildItem Env:ConnectionStrings*`, any User Secrets, and the effective instance; if the new database has *any* tables already, `EnsureCreatedAsync()` will not fill missing ones.
- Any other exception: send the first `SqlException` error line with the command and current appsettings.

This ZIP does not add Entity Framework migrations and is only for Development demonstration. It does not modify the existing v1 upgrade script. Do not run schema upgrade intended for v1 on an empty database.
