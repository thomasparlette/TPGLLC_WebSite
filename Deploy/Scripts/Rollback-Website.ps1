[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$WebsitePath,

    [Parameter(Mandatory = $true)]
    [string]$BackupRoot
)

$ErrorActionPreference = 'Stop'

$latestBackup = Get-ChildItem -Path $BackupRoot -Directory -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $latestBackup) {
    Write-Warning 'No backup folder found. Rollback skipped.'
    return
}

New-Item -ItemType Directory -Force -Path $WebsitePath | Out-Null
$appOffline = Join-Path $WebsitePath 'app_offline.htm'
Set-Content -Path $appOffline -Value '<html><body><h1>Rollback in progress</h1></body></html>' -Encoding UTF8
Start-Sleep -Seconds 2

try {
    $null = & robocopy $latestBackup.FullName $WebsitePath /MIR /R:2 /W:2 /NFL /NDL /NJH /NJS /NP /XF app_offline.htm _backup.info
    if ($LASTEXITCODE -ge 8) {
        throw "Robocopy rollback failed with exit code $LASTEXITCODE"
    }
}
finally {
    Remove-Item -Path $appOffline -Force -ErrorAction SilentlyContinue
}

Write-Host "Rollback restored from: $($latestBackup.FullName)"
