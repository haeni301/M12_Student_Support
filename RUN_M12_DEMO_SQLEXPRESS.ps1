# M12 v2 Windows PowerShell launcher for the user's SQL Server Express instance.
# Uses an isolated database; does not delete, drop, or modify prior M12 databases.
$ErrorActionPreference = 'Stop'
$env:ConnectionStrings__Default = 'Server=.\SQLEXPRESS;Database=M12_StudentSupport_V2_Demo_20261118;Trusted_Connection=True;TrustServerCertificate=True;'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DEMO_SEED = 'true'
Write-Host 'M12 v2: Using .\SQLEXPRESS / M12_StudentSupport_V2_Demo_20261118' -ForegroundColor Cyan
Write-Host 'Original M12_StudentSupport database is not modified by this launcher.' -ForegroundColor Yellow
Push-Location $PSScriptRoot
try {
    dotnet restore
    if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed' }
    dotnet build --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed' }
    dotnet run --no-build --launch-profile M12-HTTPS-Demo
} finally {
    Pop-Location
}
