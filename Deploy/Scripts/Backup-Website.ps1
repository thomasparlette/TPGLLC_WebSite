[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$WebsitePath,

    [Parameter(Mandatory = $true)]
    [string]$BackupRoot
)

$ErrorActionPreference = 'Stop'

$timestamp = Get-Date -Format 'yyyy-MM-dd_HHmmss'
$backupPath = Join-Path $BackupRoot $timestamp

New-Item -ItemType Directory -Force -Path $backupPath | Out-Null

if (Test-Path $WebsitePath) {
    $null = & robocopy $WebsitePath $backupPath /MIR /R:2 /W:2 /NFL /NDL /NJH /NJS /NP
    if ($LASTEXITCODE -ge 8) {
        throw "Robocopy backup failed with exit code $LASTEXITCODE"
    }
}

$info = @(
    "Source: $WebsitePath"
    "Backup: $backupPath"
    "Created: $(Get-Date -Format o)"
) -join [Environment]::NewLine

Set-Content -Path (Join-Path $backupPath '_backup.info') -Value $info -Encoding UTF8
Write-Host "Backup created at: $backupPath"
