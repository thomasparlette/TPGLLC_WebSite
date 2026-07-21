param(
    [Parameter(Mandatory = $true)]
    [string]$WebsitePath,

    [Parameter(Mandatory = $true)]
    [string]$PublishPath,

    [Parameter(Mandatory = $true)]
    [string]$BackupRoot,

    [Parameter(Mandatory = $true)]
    [string]$AppPoolName,

    [Parameter(Mandatory = $true)]
    [string]$HealthCheckUrl
)

$ErrorActionPreference = 'Stop'

function Write-Log {
    param([string]$Message)
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

    Write-Log "Copying files from '$Source' to '$Destination'..."
    robocopy $Source $Destination /MIR /NFL /NDL /NJH /NJS /NP /R:2 /W:2 | Out-Host
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

function Test-HealthCheck {
    param([Parameter(Mandatory = $true)] [string]$Url)

    Write-Log "Running health check: $Url"
    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 20
        if ($response.StatusCode -lt 200 -or $response.StatusCode -ge 300) {
            throw "Health check returned status code $($response.StatusCode)"
        }
        Write-Log "Health check passed with status code $($response.StatusCode)"
    }
    catch {
        throw "Health check failed: $($_.Exception.Message)"
    }
}

try {
    Write-Log "Starting deployment..."

    $publishPath = [System.IO.Path]::GetFullPath($PublishPath)
    $websitePath = [System.IO.Path]::GetFullPath($WebsitePath)
    $backupRoot = [System.IO.Path]::GetFullPath($BackupRoot)

    New-Item -ItemType Directory -Force -Path $backupRoot | Out-Null
    New-Item -ItemType Directory -Force -Path $websitePath | Out-Null

    $backupPath = Backup-CurrentWebsite -WebsitePath $websitePath -BackupRoot $backupRoot

    Write-Log "Stopping IIS app pool '$AppPoolName'..."
    Import-Module WebAdministration
    Stop-WebAppPool -Name $AppPoolName

    try {
        Invoke-RobocopySafe -Source $publishPath -Destination $websitePath
    }
    finally {
        Write-Log "Starting IIS app pool '$AppPoolName'..."
        Start-WebAppPool -Name $AppPoolName
    }

    Test-HealthCheck -Url $HealthCheckUrl

    Write-Log "Deployment completed successfully."
    exit 0
}
catch {
    Write-Log "Deployment failed: $($_.Exception.Message)"

    try {
        Import-Module WebAdministration
        Write-Log "Ensuring IIS app pool '$AppPoolName' is running..."
        Start-WebAppPool -Name $AppPoolName
    }
    catch {
        Write-Log "Could not restart app pool during failure recovery: $($_.Exception.Message)"
    }

    if ($backupPath -and (Test-Path $backupPath)) {
        try {
            Write-Log "Attempting rollback from '$backupPath'..."
            robocopy $backupPath $WebsitePath /MIR /NFL /NDL /NJH /NJS /NP /R:2 /W:2 | Out-Host
            $rollbackCode = $LASTEXITCODE
            if ($rollbackCode -ge 8) {
                Write-Log "Rollback robocopy exit code: $rollbackCode"
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