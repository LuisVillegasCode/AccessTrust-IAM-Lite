$ErrorActionPreference = "Stop"

# ==========================================
# Backup MongoDB - AccessTrust IAM Lite
# ==========================================

$DatabaseName = "AccessTrustIAMLite"
$AuthDatabase = "admin"

$Username = "backup_user"
$Password = "Backup123*"

$HostName = "localhost"
$Port = "27017"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$BackupRoot = Join-Path $ProjectRoot "backups"

$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$OutputPath = Join-Path $BackupRoot "$DatabaseName`_$Timestamp"

if (-not (Test-Path $BackupRoot)) {
    New-Item -ItemType Directory -Path $BackupRoot | Out-Null
}

Write-Host "=========================================="
Write-Host " Backup MongoDB - AccessTrust IAM Lite"
Write-Host "=========================================="
Write-Host "Base de datos: $DatabaseName"
Write-Host "Usuario backup: $Username"
Write-Host "Destino: $OutputPath"
Write-Host ""

mongodump `
    --host $HostName `
    --port $Port `
    --username $Username `
    --password $Password `
    --authenticationDatabase $AuthDatabase `
    --db $DatabaseName `
    --out $OutputPath

Write-Host ""
Write-Host "=========================================="
Write-Host " Backup completado correctamente"
Write-Host "=========================================="
Write-Host "Ruta generada:"
Write-Host $OutputPath