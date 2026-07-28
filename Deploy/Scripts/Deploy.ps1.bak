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
    [string]$LogRoot,

    [Parameter(Mandatory = $true)]
    [string]$HealthCheckUrl,

    [Parameter(Mandatory = $true)]
    [string]$CommitSha,

    [Parameter(Mandatory = $true)]
    [string]$BranchName,

    [string]$RunnerName = $env:COMPUTERNAME,

    [string]$SiteName = 'TPGLLC',
    [string]$AppPoolName = 'TPGLLC',

    [int]$KeepReleases = 10,
    [int]$LogRetentionDays = 30,
    [int]$BackupRetentionDays = 30,
    [int]$HealthCheckMaxAttempts = 12,
    [int]$HealthCheckDelaySeconds = 5,
    [int]$FileUnlockTimeoutSeconds = 60
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Ensure-Directory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    New-Item -ItemType Directory -Force -Path $Path | Out-Null
}

Ensure-Directory -Path $LogRoot
$LogFile = Join-Path $LogRoot ("Deploy_{0}.log" -f (Get-Date -Format 'yyyy-MM-dd_HHmmss'))

function Write-Log {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message,

        [ValidateSet('Info', 'Warn', 'Error')]
        [string]$Level = 'Info'
    )

    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $line = "[$timestamp] [$Level] $Message"

    switch ($Level) {
        'Warn'  { Write-Host $line -ForegroundColor Yellow }
        'Error' { Write-Host $line -ForegroundColor Red }
        default { Write-Host $line }
    }

    Add-Content -Path $LogFile -Value $line
}

function Invoke-RobocopySafe {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Source,

        [Parameter(Mandatory = $true)]
        [string]$Destination,

        [string[]]$ExcludeFiles = @('app_offline.htm')
    )

    if (-not (Test-Path $Source)) {
        throw "Source path does not exist: $Source"
    }

    Ensure-Directory -Path $Destination

    $args = @(
        $Source,
        $Destination,
        '/MIR',
        '/NFL',
        '/NDL',
        '/NJH',
        '/NJS',
        '/NP',
        '/R:2',
        '/W:2'
    )

    foreach ($file in $ExcludeFiles) {
        $args += '/XF'
        $args += $file
    }

    Write-Log "Running robocopy from '$Source' to '$Destination'..."
    & robocopy @args 2>&1 | Tee-Object -FilePath $LogFile -Append | Out-Host

    $code = $LASTEXITCODE
    if ($code -ge 8) {
        throw "Robocopy failed with exit code $code"
    }

    Write-Log "Robocopy completed with exit code $code"
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

function Import-WebAdministrationModule {
    try {
        Import-Module WebAdministration -ErrorAction Stop
        return $true
    }
    catch {
        Write-Log "WebAdministration module unavailable: $($_.Exception.Message)" 'Warn'
        return $false
    }
}

function Stop-IisHosting {
    param(
        [string]$SiteName,
        [string]$AppPoolName
    )

    if (-not (Import-WebAdministrationModule)) {
        return
    }

    try {
        if (Get-Website -Name $SiteName -ErrorAction SilentlyContinue) {
            Write-Log "Stopping IIS site '$SiteName'..."
            Stop-Website -Name $SiteName -ErrorAction SilentlyContinue
        }
    }
    catch {
        Write-Log "Failed to stop IIS site '$SiteName': $($_.Exception.Message)" 'Warn'
    }

    try {
        if (Get-WebAppPoolState -Name $AppPoolName -ErrorAction SilentlyContinue) {
            Write-Log "Stopping IIS app pool '$AppPoolName'..."
            Stop-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
        }
    }
    catch {
        Write-Log "Failed to stop IIS app pool '$AppPoolName': $($_.Exception.Message)" 'Warn'
    }
}

function Start-IisHosting {
    param(
        [string]$SiteName,
        [string]$AppPoolName
    )

    if (-not (Import-WebAdministrationModule)) {
        return
    }

    try {
        if (Get-WebAppPoolState -Name $AppPoolName -ErrorAction SilentlyContinue) {
            Write-Log "Starting IIS app pool '$AppPoolName'..."
            Start-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
        }
    }
    catch {
        Write-Log "Failed to start IIS app pool '$AppPoolName': $($_.Exception.Message)" 'Warn'
    }

    try {
        if (Get-Website -Name $SiteName -ErrorAction SilentlyContinue) {
            Write-Log "Starting IIS site '$SiteName'..."
            Start-Website -Name $SiteName -ErrorAction SilentlyContinue
        }
    }
    catch {
        Write-Log "Failed to start IIS site '$SiteName': $($_.Exception.Message)" 'Warn'
    }
}

function Backup-CurrentWebsite {
    param(
        [Parameter(Mandatory = $true)]
        [string]$WebsitePath,

        [Parameter(Mandatory = $true)]
        [string]$BackupRoot,

        [Parameter(Mandatory = $true)]
        [string]$CommitSha
    )

    if (-not (Test-Path $WebsitePath)) {
        Write-Log "Website path does not exist yet. Skipping backup."
        return $null
    }

    Ensure-Directory -Path $BackupRoot

    $stamp = Get-Date -Format 'yyyy-MM-dd_HHmmss'
    $shortSha = if ($CommitSha.Length -ge 7) { $CommitSha.Substring(0, 7) } else { $CommitSha }
    $backupPath = Join-Path $BackupRoot ("{0}_{1}" -f $stamp, $shortSha)
    Ensure-Directory -Path $backupPath

    Write-Log "Backing up current website to '$backupPath'..."
    & robocopy $WebsitePath $backupPath /MIR /NFL /NDL /NJH /NJS /NP /R:2 /W:2 2>&1 |
        Tee-Object -FilePath $LogFile -Append |
        Out-Host

    $code = $LASTEXITCODE
    if ($code -ge 8) {
        throw "Backup failed with exit code $code"
    }

    Write-Log "Backup created at: $backupPath"
    return $backupPath
}

function Copy-ReleaseSnapshot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PublishPath,

        [Parameter(Mandatory = $true)]
        [string]$ReleaseRoot,

        [Parameter(Mandatory = $true)]
        [string]$CommitSha
    )

    if (-not (Test-Path $PublishPath)) {
        throw "Publish path does not exist: $PublishPath"
    }

    Ensure-Directory -Path $ReleaseRoot

    $stamp = Get-Date -Format 'yyyy-MM-dd_HHmmss'
    $shortSha = if ($CommitSha.Length -ge 7) { $CommitSha.Substring(0, 7) } else { $CommitSha }
    $releasePath = Join-Path $ReleaseRoot ("{0}_{1}" -f $stamp, $shortSha)
    Ensure-Directory -Path $releasePath

    Write-Log "Creating release snapshot at '$releasePath'..."
    & robocopy $PublishPath $releasePath /MIR /XF app_offline.htm /NFL /NDL /NJH /NJS /NP /R:2 /W:2 2>&1 |
        Tee-Object -FilePath $LogFile -Append |
        Out-Host

    $code = $LASTEXITCODE
    if ($code -ge 8) {
        throw "Release snapshot failed with exit code $code"
    }

    Write-Log "Release snapshot created at: $releasePath"
    return $releasePath
}

function Write-VersionFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$WebsitePath,

        [Parameter(Mandatory = $true)]
        [string]$CommitSha,

        [Parameter(Mandatory = $true)]
        [string]$BranchName,

        [Parameter(Mandatory = $true)]
        [string]$RunnerName
    )

    $shortSha = if ($CommitSha.Length -ge 7) { $CommitSha.Substring(0, 7) } else { $CommitSha }
    $versionValue = "{0}+{1}" -f (Get-Date -Format 'yyyy.MM.dd.HHmmss'), $shortSha

    $versionInfo = [ordered]@{
        version  = $versionValue
        commit   = $CommitSha
        branch   = $BranchName
        deployed = (Get-Date).ToString('o')
        runner   = $RunnerName
    }

    $versionPath = Join-Path $WebsitePath 'version.json'
    $versionInfo | ConvertTo-Json -Depth 5 | Set-Content -Path $versionPath -Encoding UTF8

    Write-Log "Version file written: $versionPath"
}

function Cleanup-OldReleases {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ReleaseRoot,

        [int]$KeepReleases = 10
    )

    if (-not (Test-Path $ReleaseRoot)) {
        return
    }

    $releases = Get-ChildItem -Path $ReleaseRoot -Directory | Sort-Object Name -Descending
    $oldReleases = $releases | Select-Object -Skip $KeepReleases

    foreach ($release in $oldReleases) {
        Write-Log "Removing old release: $($release.FullName)"
        Remove-Item $release.FullName -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Cleanup-OldLogs {
    param(
        [Parameter(Mandatory = $true)]
        [string]$LogRoot,

        [int]$RetentionDays = 30
    )

    if (-not (Test-Path $LogRoot)) {
        return
    }

    $cutoff = (Get-Date).AddDays(-$RetentionDays)

    Get-ChildItem -Path $LogRoot -File |
        Where-Object { $_.LastWriteTime -lt $cutoff } |
        ForEach-Object {
            Write-Log "Removing old log: $($_.FullName)"
            Remove-Item $_.FullName -Force -ErrorAction SilentlyContinue
        }
}

function Cleanup-OldBackups {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BackupRoot,

        [int]$RetentionDays = 30
    )

    if (-not (Test-Path $BackupRoot)) {
        return
    }

    $cutoff = (Get-Date).AddDays(-$RetentionDays)

    Get-ChildItem -Path $BackupRoot -Directory |
        Where-Object { $_.LastWriteTime -lt $cutoff } |
        ForEach-Object {
            Write-Log "Removing old backup: $($_.FullName)"
            Remove-Item $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
        }
}

function Test-HealthCheck {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Url
    )

    Write-Log "Running health check: $Url"
    $response = Invoke-WebRequest -Uri $Url -TimeoutSec 20 -UseBasicParsing

    if ($response.StatusCode -lt 200 -or $response.StatusCode -ge 300) {
        throw "Health check returned status code $($response.StatusCode)"
    }

    Write-Log "Health check passed with status code $($response.StatusCode)"
}

function Invoke-DeploymentRollback {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BackupPath,

        [Parameter(Mandatory = $true)]
        [string]$WebsitePath,

        [Parameter(Mandatory = $true)]
        [string]$AppOfflinePath,

        [Parameter(Mandatory = $true)]
        [string]$SiteName,

        [Parameter(Mandatory = $true)]
        [string]$AppPoolName,

        [int]$WaitSeconds = 30
    )

    if (-not (Test-Path $BackupPath)) {
        Write-Log "Rollback skipped; backup path not found: $BackupPath" 'Warn'
        return
    }

    Write-Log "Preparing rollback from '$BackupPath'..."

    Stop-IisHosting -SiteName $SiteName -AppPoolName $AppPoolName

    $dllPath = Join-Path $WebsitePath 'TPGLLC.Web.dll'
    if (Test-Path $dllPath) {
        try {
            Write-Log "Waiting for application DLL to unlock before rollback..."
            Wait-ForFileUnlock -Path $dllPath -TimeoutSeconds $WaitSeconds
        }
        catch {
            Write-Log "Timed out waiting for unlock during rollback: $($_.Exception.Message)" 'Warn'
        }
    }

    if (-not (Test-Path $AppOfflinePath)) {
        @"
<html>
<head><title>Site Offline</title></head>
<body><h1>Temporarily offline for rollback.</h1></body>
</html>
"@ | Set-Content -Path $AppOfflinePath -Encoding UTF8
    }

    Write-Log "Restoring website from backup..."
    & robocopy $BackupPath $WebsitePath /MIR /XF app_offline.htm /NFL /NDL /NJH /NJS /NP /R:2 /W:2 2>&1 |
        Tee-Object -FilePath $LogFile -Append |
        Out-Host

    $rollbackCode = $LASTEXITCODE
    if ($rollbackCode -ge 8) {
        throw "Rollback failed with exit code $rollbackCode"
    }

    Remove-Item $AppOfflinePath -Force -ErrorAction SilentlyContinue
    Start-IisHosting -SiteName $SiteName -AppPoolName $AppPoolName

    $attempt = 1
    while ($attempt -le $HealthCheckMaxAttempts) {
        try {
            Test-HealthCheck -Url $HealthCheckUrl
            Write-Log "Rollback verification passed."
            return
        }
        catch {
            Write-Log "Rollback health check attempt $attempt failed: $($_.Exception.Message)" 'Warn'
            if ($attempt -ge $HealthCheckMaxAttempts) {
                throw
            }
            Start-Sleep -Seconds $HealthCheckDelaySeconds
            $attempt++
        }
    }
}

$appOfflinePath = Join-Path $WebsitePath 'app_offline.htm'
$backupPath = $null
$releasePath = $null

try {
    Write-Log "Starting deployment..."
    Write-Log "Repository branch: $BranchName"
    Write-Log "Commit SHA: $CommitSha"
    Write-Log "Runner: $RunnerName"
    Write-Log "Site name: $SiteName"
    Write-Log "App pool name: $AppPoolName"

    Ensure-Directory -Path $BackupRoot
    Ensure-Directory -Path $WebsitePath
    Ensure-Directory -Path $ReleaseRoot

    Stop-IisHosting -SiteName $SiteName -AppPoolName $AppPoolName

    $dllPath = Join-Path $WebsitePath 'TPGLLC.Web.dll'
    if (Test-Path $dllPath) {
        Write-Log "Waiting for application DLL to unlock..."
        Wait-ForFileUnlock -Path $dllPath -TimeoutSeconds $FileUnlockTimeoutSeconds
    }

    if (-not (Test-Path $appOfflinePath)) {
        Write-Log "Placing app_offline.htm..."
        @"
<html>
<head><title>Site Offline</title></head>
<body><h1>Temporarily offline for deployment.</h1></body>
</html>
"@ | Set-Content -Path $appOfflinePath -Encoding UTF8
    }

    $backupPath = Backup-CurrentWebsite -WebsitePath $WebsitePath -BackupRoot $BackupRoot -CommitSha $CommitSha
    $releasePath = Copy-ReleaseSnapshot -PublishPath $PublishPath -ReleaseRoot $ReleaseRoot -CommitSha $CommitSha

    Write-Log "Deploying release '$releasePath' to '$WebsitePath'..."
    Invoke-RobocopySafe -Source $releasePath -Destination $WebsitePath

    Write-VersionFile -WebsitePath $WebsitePath -CommitSha $CommitSha -BranchName $BranchName -RunnerName $RunnerName

    Remove-Item $appOfflinePath -Force -ErrorAction SilentlyContinue
    Write-Log "Removed app_offline.htm"

    Start-IisHosting -SiteName $SiteName -AppPoolName $AppPoolName

    $attempt = 1
    while ($attempt -le $HealthCheckMaxAttempts) {
        try {
            Test-HealthCheck -Url $HealthCheckUrl
            break
        }
        catch {
            Write-Log "Health check attempt $attempt of $HealthCheckMaxAttempts failed: $($_.Exception.Message)" 'Warn'
            if ($attempt -ge $HealthCheckMaxAttempts) {
                throw
            }
            Start-Sleep -Seconds $HealthCheckDelaySeconds
            $attempt++
        }
    }

    Cleanup-OldReleases -ReleaseRoot $ReleaseRoot -KeepReleases $KeepReleases
    Cleanup-OldLogs -LogRoot $LogRoot -RetentionDays $LogRetentionDays
    Cleanup-OldBackups -BackupRoot $BackupRoot -RetentionDays $BackupRetentionDays

    Write-Log "Deployment completed successfully."
    exit 0
}
catch {
    Write-Log "Deployment failed: $($_.Exception.Message)" 'Error'

    try {
        if (Test-Path $appOfflinePath) {
            Remove-Item $appOfflinePath -Force -ErrorAction SilentlyContinue
        }
    }
    catch {
        Write-Log "Failed removing app_offline.htm: $($_.Exception.Message)" 'Warn'
    }

    if ($backupPath -and (Test-Path $backupPath)) {
        try {
            Invoke-DeploymentRollback -BackupPath $backupPath -WebsitePath $WebsitePath -AppOfflinePath $appOfflinePath -SiteName $SiteName -AppPoolName $AppPoolName -WaitSeconds $FileUnlockTimeoutSeconds
        }
        catch {
            Write-Log "Rollback failed: $($_.Exception.Message)" 'Error'
        }
    }

    exit 1
}
