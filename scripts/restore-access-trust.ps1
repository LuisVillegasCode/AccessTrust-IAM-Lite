$ErrorActionPreference = "Stop"

# ==========================================
# Restore MongoDB - AccessTrust IAM Lite
# ==========================================

$SourceDatabaseName = "AccessTrustIAMLite"
$TargetDatabaseName = "AccessTrustIAMLite_RestoreTest"
$AuthDatabase = "admin"

$Username = "backup_user"
$Password = "Backup123*"

$HostName = "localhost"
$Port = "27017"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$BackupRoot = Join-Path $ProjectRoot "backups"

$LatestBackup = Get-ChildItem -Path $BackupRoot -Directory |
    Where-Object { $_.Name -like "$SourceDatabaseName`_*" } |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($null -eq $LatestBackup) {
    throw "No se encontró ningún backup en la carpeta: $BackupRoot"
}

$SourceBackupPath = Join-Path $LatestBackup.FullName $SourceDatabaseName

if (-not (Test-Path $SourceBackupPath)) {
    throw "No se encontró la carpeta interna del backup: $SourceBackupPath"
}

Write-Host "=========================================="
Write-Host " Restore MongoDB - AccessTrust IAM Lite"
Write-Host "=========================================="
Write-Host "Backup origen: $SourceBackupPath"
Write-Host "Base destino: $TargetDatabaseName"
Write-Host "Usuario restore: $Username"
Write-Host ""

mongorestore `
    --host $HostName `
    --port $Port `
    --username $Username `
    --password $Password `
    --authenticationDatabase $AuthDatabase `
    --drop `
    --db $TargetDatabaseName `
    $SourceBackupPath

Write-Host ""
Write-Host "=========================================="
Write-Host " Restore completado correctamente"
Write-Host "=========================================="
Write-Host "Base restaurada:"
Write-Host $TargetDatabaseName