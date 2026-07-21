param(
    [Parameter(Mandatory = $true)]
    [string]$WebsitePath,

    [Parameter(Mandatory = $true)]
    [string]$PublishPath,

    [Parameter(Mandatory = $true)]
    [string]$BackupRoot,

    [Parameter(Mandatory = $true)]
    [string]$ReleaseRoot,

    [Parameter(Mandatory = $true)]
    [string]$HealthCheckUrl,

    [int]$KeepReleases = 10
)

$ErrorActionPreference = 'Stop'

function Write-Log {
    param([Parameter(Mandatory = $true)][string]$Message)
    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    Write-Host "[$timestamp] $Message"
}

function Invoke-RobocopySafe {
    param(
        [Parameter(Mandatory = $true)] [string]$Source,
        [Parameter(Mandatory = $true)] [string]$Destination
    )

    if (-not (Test-Path $Source)) {
        throw "Source path does not exist: $Source"
    }

    New-Item -ItemType Directory -Force -Path $Destination | Out-Null

    robocopy $Source $Destination /MIR /XF app_offline.htm /NFL /NDL /NJH /NJS /NP /R:2 /W:2 | Out-Host
    $code = $LASTEXITCODE

    if ($code -ge 8) {
        throw "Robocopy failed with exit code $code"
    }

    Write-Log "Robocopy completed with exit code $code"
}

function Backup-CurrentWebsite {
    param(
        [Parameter(Mandatory = $true)] [string]$WebsitePath,
        [Parameter(Mandatory = $true)] [string]$BackupRoot
    )

    if (-not (Test-Path $WebsitePath)) {
        Write-Log "Website path does not exist yet. Skipping backup."
        return $null
    }

    $stamp = Get-Date -Format 'yyyy-MM-dd_HHmmss'
    $backupPath = Join-Path $BackupRoot $stamp
    New-Item -ItemType Directory -Force -Path $backupPath | Out-Null

    Write-Log "Backing up current website to '$backupPath'..."
    robocopy $WebsitePath $backupPath /MIR /NFL /NDL /NJH /NJS /NP /R:2 /W:2 | Out-Host
    $code = $LASTEXITCODE

    if ($code -ge 8) {
        throw "Backup failed with exit code $code"
    }

    Write-Log "Backup created at: $backupPath"
    return $backupPath
}

function Wait-ForFileUnlock {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [int]$TimeoutSeconds = 30
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)

    while ((Get-Date) -lt $deadline) {
        if (-not (Test-Path $Path)) {
            return
        }

        try {
            $stream = [System.IO.File]::Open(
                $Path,
                [System.IO.FileMode]::Open,
                [System.IO.FileAccess]::ReadWrite,
                [System.IO.FileShare]::None
            )
            $stream.Close()
            return
        }
        catch {
            Start-Sleep -Seconds 1
        }
    }

    throw "Timed out waiting for file unlock: $Path"
}

function Copy-ReleaseSnapshot {
    param(
        [Parameter(Mandatory = $true)] [string]$PublishPath,
        [Parameter(Mandatory = $true)] [string]$ReleaseRoot
    )

    New-Item -ItemType Directory -Force -Path $ReleaseRoot | Out-Null

    $stamp = Get-Date -Format 'yyyy-MM-dd_HHmmss'
    $releasePath = Join-Path $ReleaseRoot $stamp
    New-Item -ItemType Directory -Force -Path $releasePath | Out-Null

    Write-Log "Creating release snapshot at '$releasePath'..."
    robocopy $PublishPath $releasePath /MIR /XF app_offline.htm /NFL /NDL /NJH /NJS /NP /R:2 /W:2 | Out-Host
    $code = $LASTEXITCODE

    if ($code -ge 8) {
        throw "Release snapshot failed with exit code $code"
    }

    Write-Log "Release snapshot created at: $releasePath"
    return $releasePath
}

function Cleanup-OldReleases {
    param(
        [Parameter(Mandatory = $true)] [string]$ReleaseRoot,
        [int]$KeepReleases = 10
    )

    if (-not (Test-Path $ReleaseRoot)) {
        return
    }

    $releases = Get-ChildItem -Path $ReleaseRoot -Directory |
        Sort-Object Name -Descending

    $oldReleases = $releases | Select-Object -Skip $KeepReleases

    foreach ($release in $oldReleases) {
        Write-Log "Removing old release: $($release.FullName)"
        Remove-Item $release.FullName -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Test-HealthCheck {
    param([Parameter(Mandatory = $true)] [string]$Url)

    Write-Log "Running health check: $Url"
    $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 20

    if ($response.StatusCode -lt 200 -or $response.StatusCode -ge 300) {
        throw "Health check returned status code $($response.StatusCode)"
    }

    Write-Log "Health check passed with status code $($response.StatusCode)"
}

$appOfflinePath = Join-Path $WebsitePath 'app_offline.htm'
$backupPath = $null
$releasePath = $null

try {
    Write-Log "Starting deployment..."

    New-Item -ItemType Directory -Force -Path $BackupRoot | Out-Null
    New-Item -ItemType Directory -Force -Path $WebsitePath | Out-Null
    New-Item -ItemType Directory -Force -Path $ReleaseRoot | Out-Null

    $backupPath = Backup-CurrentWebsite -WebsitePath $WebsitePath -BackupRoot $BackupRoot
    $releasePath = Copy-ReleaseSnapshot -PublishPath $PublishPath -ReleaseRoot $ReleaseRoot

    Write-Log "Placing app_offline.htm..."
    @"
<html>
<head><title>Site Offline</title></head>
<body><h1>Temporarily offline for deployment.</h1></body>
</html>
"@ | Set-Content -Path $appOfflinePath -Encoding UTF8

    Start-Sleep -Seconds 2

    $mainDll = Join-Path $WebsitePath 'TPGLLC_WebSite.dll'
    if (Test-Path $mainDll) {
        Write-Log "Waiting for application DLL to unlock..."
        Wait-ForFileUnlock -Path $mainDll -TimeoutSeconds 30
    }

    Write-Log "Deploying release '$releasePath' to '$WebsitePath'..."
    Invoke-RobocopySafe -Source $releasePath -Destination $WebsitePath

    if (Test-Path $appOfflinePath) {
        Remove-Item $appOfflinePath -Force -ErrorAction SilentlyContinue
        Write-Log "Removed app_offline.htm"
    }

    Test-HealthCheck -Url $HealthCheckUrl
    Cleanup-OldReleases -ReleaseRoot $ReleaseRoot -KeepReleases $KeepReleases

    Write-Log "Deployment completed successfully."
    exit 0
}
catch {
    Write-Log "Deployment failed: $($_.Exception.Message)"

    if (Test-Path $appOfflinePath) {
        Remove-Item $appOfflinePath -Force -ErrorAction SilentlyContinue
    }

    if ($backupPath -and (Test-Path $backupPath)) {
        try {
            Write-Log "Attempting rollback from '$backupPath'..."
            robocopy $backupPath $WebsitePath /MIR /XF app_offline.htm /NFL /NDL /NJH /NJS /NP /R:2 /W:2 | Out-Host
            $rollbackCode = $LASTEXITCODE
            if ($rollbackCode -ge 8) {
                Write-Log "Rollback failed with exit code $rollbackCode"
            }
            else {
                Write-Log "Rollback completed."
            }
        }
        catch {
            Write-Log "Rollback failed: $($_.Exception.Message)"
        }
    }

    exit 1
}