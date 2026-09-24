# Starts the throwaway SQL Server 2022 container for the MeterForge SQL Server lab.
# Port 14337 (not 1433) so it never collides with a real local SQL Server.
# The SA password is a lab-only throwaway value; never reuse it anywhere else.
param(
    [string]$Name = "meterforge-training-mssql",
    [int]$Port = 14337,
    [string]$Password = "MeterForge#Lab2026"
)

$existing = docker ps -a --filter "name=^$Name$" --format "{{.Names}}"
if ($existing) {
    docker start $Name | Out-Null
} else {
    docker run -d --name $Name `
        -e ACCEPT_EULA=Y -e "MSSQL_SA_PASSWORD=$Password" -e MSSQL_PID=Developer -e MSSQL_MEMORY_LIMIT_MB=2048 `
        -p "${Port}:1433" mcr.microsoft.com/mssql/server:2022-latest | Out-Null
}

Write-Host "waiting for SQL Server to accept connections..."
for ($i = 0; $i -lt 60; $i++) {
    docker exec $Name /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P $Password -Q "SELECT 1" -b 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) { break }
    Start-Sleep -Seconds 2
}

# The production-like database: compatibility level 150 (SQL Server 2019 semantics), the level most
# migrated Azure SQL databases still run at. Level 160 turns on Parameter Sensitive Plan optimization,
# which changes how incident 001 behaves; the answer file explains the difference.
docker exec $Name /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P $Password -b -Q @"
IF DB_ID('MeterForge') IS NULL CREATE DATABASE MeterForge;
ALTER DATABASE MeterForge SET COMPATIBILITY_LEVEL = 150;
"@

$env:METERFORGE_SQLSERVER = "Server=localhost,$Port;Database=MeterForge;User Id=sa;Password=$Password;TrustServerCertificate=True"
$env:METERFORGE_SQLSERVER_TESTS = "Server=localhost,$Port;User Id=sa;Password=$Password;TrustServerCertificate=True"
Write-Host "ready. METERFORGE_SQLSERVER and METERFORGE_SQLSERVER_TESTS are set for this PowerShell session."
Write-Host "sqlcmd:  docker exec -it $Name /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P '$Password' -d MeterForge"
