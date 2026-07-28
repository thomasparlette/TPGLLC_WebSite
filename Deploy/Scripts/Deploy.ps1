[CmdletBinding()]
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

    [Parameter(Mandatory = $false)]
    [string]$CommitSha = "",
 
    [Parameter(Mandatory = $false)]
    [string]$BranchName = "",

    [Parameter(Mandatory = $false)]
    [string]$RunnerName = $env:COMPUTERNAME,

    [Parameter(Mandatory = $false)]
    [string]$SiteName = "TPGLLC",

    [Parameter(Mandatory = $false)]
    [string]$AppPoolName = "TPGLLC",

    [switch]$RunMigrations,
    [switch]$RunBootstrapper,
    [switch]$RunVehicleImporter,
    [switch]$ForceVehicleImporter,
    [switch]$RestartIIS = $true,
    [switch]$SkipHealthCheck
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# -----------------------------
# Helpers
# -----------------------------

function New-DeploymentStamp {
    return (Get-Date).ToString('yyyyMMdd-HHmmss')
}

function Ensure-Directory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function Write-Log {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message,

        [ValidateSet('INFO', 'WARN', 'ERROR')]
        [string]$Level = 'INFO'
    )

    $timestamp = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
    $line = "[{0}] [{1}] {2}" -f $timestamp, $Level, $Message

    Write-Host $line

    if ($script:LogFilePath) {
        Add-Content -Path $script:LogFilePath -Value $line
    }
}

function Invoke-RobocopyMirror {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Source,

        [Parameter(Mandatory = $true)]
        [string]$Destination,

        [string[]]$ExcludeFiles = @(),

        [string]$Description = 'copy'
    )

    if (-not (Test-Path -LiteralPath $Source)) {
        throw "Robocopy $Description source does not exist: $Source"
    }

    Ensure-Directory -Path $Destination

    $args = @(
        $Source,
        $Destination,
        '/MIR',
        '/R:2',
        '/W:2',
        '/NFL',
        '/NDL',
        '/NP',
        '/NJH',
        '/NJS',
        '/XJ'
    )

    if ($ExcludeFiles.Count -gt 0) {
        $args += '/XF'
        $args += $ExcludeFiles
    }

    Write-Log "Starting robocopy $Description from '$Source' to '$Destination'."

    $output = & robocopy @args 2>&1
    foreach ($line in $output) {
        if ($line) {
            Write-Log $line
        }
    }

    $exitCode = $LASTEXITCODE

    if ($exitCode -ge 8) {
        throw "Robocopy $Description failed with exit code $exitCode."
    }

    Write-Log "Robocopy $Description completed with exit code $exitCode."
}

function Import-WebAdministrationModule {
    if (Get-Module -ListAvailable -Name WebAdministration) {
        Import-Module WebAdministration -ErrorAction SilentlyContinue
        return $true
    }

    return $false
}

function Stop-IisTarget {
    param(
        [string]$TargetSiteName,
        [string]$TargetAppPoolName,
        [string]$OfflinePath
    )

    $hasWebAdmin = Import-WebAdministrationModule

    if ($hasWebAdmin) {
        try {
            if (Get-Website -Name $TargetSiteName -ErrorAction SilentlyContinue) {
                Write-Log "Stopping IIS site '$TargetSiteName'."
                Stop-Website -Name $TargetSiteName -ErrorAction SilentlyContinue
            }
        }
        catch {
            Write-Log "Failed to stop site '$TargetSiteName': $($_.Exception.Message)" 'WARN'
        }

        try {
            if (Get-WebAppPoolState -Name $TargetAppPoolName -ErrorAction SilentlyContinue) {
                Write-Log "Stopping app pool '$TargetAppPoolName'."
                Stop-WebAppPool -Name $TargetAppPoolName -ErrorAction SilentlyContinue
            }
        }
        catch {
            Write-Log "Failed to stop app pool '$TargetAppPoolName': $($_.Exception.Message)" 'WARN'
        }
    }
    else {
        Write-Log "WebAdministration module not available. Falling back to app_offline.htm only." 'WARN'
    }

    $offlineHtml = @"
<!doctype html>
<html>
<head>
  <meta charset="utf-8">
  <title>Maintenance</title>
</head>
<body>
  <h1>Maintenance in progress</h1>
  <p>The site is temporarily offline for deployment.</p>
</body>
</html>
"@

    Set-Content -Path $OfflinePath -Value $offlineHtml -Encoding UTF8
    Write-Log "Wrote app_offline.htm."
    Start-Sleep -Seconds 2
}

function Start-IisTarget {
    param(
        [string]$TargetSiteName,
        [string]$TargetAppPoolName,
        [string]$OfflinePath
    )

    if (Test-Path -LiteralPath $OfflinePath) {
        Remove-Item -LiteralPath $OfflinePath -Force -ErrorAction SilentlyContinue
        Write-Log "Removed app_offline.htm."
    }

    $hasWebAdmin = Import-WebAdministrationModule

    if ($hasWebAdmin) {
        try {
            if (Get-WebAppPoolState -Name $TargetAppPoolName -ErrorAction SilentlyContinue) {
                Write-Log "Starting app pool '$TargetAppPoolName'."
                Start-WebAppPool -Name $TargetAppPoolName -ErrorAction SilentlyContinue
            }
        }
        catch {
            Write-Log "Failed to start app pool '$TargetAppPoolName': $($_.Exception.Message)" 'WARN'
        }

        try {
            if (Get-Website -Name $TargetSiteName -ErrorAction SilentlyContinue) {
                Write-Log "Starting IIS site '$TargetSiteName'."
                Start-Website -Name $TargetSiteName -ErrorAction SilentlyContinue
            }
        }
        catch {
            Write-Log "Failed to start site '$TargetSiteName': $($_.Exception.Message)" 'WARN'
        }
    }

    Write-Log "IIS start sequence complete."
}

function Write-DeploymentMetadata {
    param(
        [Parameter(Mandatory = $true)]
        [string]$DestinationPath,

        [Parameter(Mandatory = $true)]
        [string]$Stamp
    )

    $metadata = [ordered]@{
        Version       = $Stamp
        GitCommit     = $CommitSha
        Branch        = $BranchName
        Runner        = $RunnerName
        DeployedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
        WebsitePath   = $WebsitePath
        PublishPath   = $PublishPath
    }

    $json = $metadata | ConvertTo-Json -Depth 5
    Set-Content -Path (Join-Path $DestinationPath 'version.json') -Value $json -Encoding UTF8
    Set-Content -Path (Join-Path $DestinationPath 'deployment.json') -Value $json -Encoding UTF8

    Write-Log "Wrote deployment metadata to '$DestinationPath'."
}

function Test-DeploymentHealth {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Url,

        [int]$MaxAttempts = 12,

        [int]$DelaySeconds = 5
    )

    Write-Log "Starting health check against '$Url'."

    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 30
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300) {
                Write-Log "Health check succeeded on attempt $attempt."
                return
            }

            Write-Log "Health check returned status code $($response.StatusCode) on attempt $attempt." 'WARN'
        }
        catch {
            Write-Log "Health check attempt $attempt failed: $($_.Exception.Message)" 'WARN'
        }

        if ($attempt -lt $MaxAttempts) {
            Start-Sleep -Seconds $DelaySeconds
        }
    }

    throw "Health check failed after $MaxAttempts attempts."
}

function Restore-Backup {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BackupPath,

        [Parameter(Mandatory = $true)]
        [string]$DestinationPath
    )

    if (-not (Test-Path -LiteralPath $BackupPath)) {
        throw "Rollback requested, but backup path '$BackupPath' does not exist."
    }

    Write-Log "Restoring website from backup '$BackupPath' to '$DestinationPath'."
    Invoke-RobocopyMirror -Source $BackupPath -Destination $DestinationPath -ExcludeFiles @('app_offline.htm') -Description 'rollback restore'
}

function Get-RepoRoot {
    if ($env:GITHUB_WORKSPACE -and (Test-Path -LiteralPath $env:GITHUB_WORKSPACE)) {
        return (Resolve-Path -LiteralPath $env:GITHUB_WORKSPACE).Path
    }

    return (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..' '..')).Path
}

function Get-ProjectPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    return (Join-Path (Get-RepoRoot) $RelativePath)
}

function Invoke-DotNetCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,

        [Parameter(Mandatory = $true)]
        [string]$Description,

        [Parameter(Mandatory = $false)]
        [hashtable]$Environment = @{} ,

        [Parameter(Mandatory = $false)]
        [string]$WorkingDirectory = (Get-RepoRoot)
    )

    $savedEnvironment = @{}

    foreach ($key in $Environment.Keys) {
        $savedEnvironment[$key] = [Environment]::GetEnvironmentVariable($key, 'Process')
        [Environment]::SetEnvironmentVariable($key, [string]$Environment[$key], 'Process')
    }

    Push-Location $WorkingDirectory
    try {
        Write-Log "Running $Description..."
        & dotnet @Arguments

        if ($LASTEXITCODE -ne 0) {
            throw "$Description failed with exit code $LASTEXITCODE."
        }

        Write-Log "$Description completed successfully."
    }
    finally {
        Pop-Location

        foreach ($key in $Environment.Keys) {
            [Environment]::SetEnvironmentVariable($key, $savedEnvironment[$key], 'Process')
        }
    }
}

function Run-Migrations {
    $repoRoot = Get-RepoRoot
    $dataProject = Get-ProjectPath 'TPGLLC.Data\TPGLLC.Data.csproj'
    $apiStartupProject = Get-ProjectPath 'TPGLLC.Web\TPGLLC.Web.csproj'

    Push-Location $repoRoot
    try {
        Write-Log "Restoring dotnet tools..."
        & dotnet tool restore
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet tool restore failed with exit code $LASTEXITCODE."
        }

        $environment = @{
            DOTNET_ENVIRONMENT = 'Production'
            ASPNETCORE_ENVIRONMENT = 'Production'
        }
		
			
        Invoke-DotNetCommand `
            -Description 'EF Core database update' `
            -WorkingDirectory $repoRoot `
            -Environment $environment `
            -Arguments @(
                'ef',
                'database',
                'update',
                '--project', $dataProject,
                '--startup-project', $apiStartupProject,
                '--configuration', 'Release'
            )
    }
    finally {
        Pop-Location
    }
}

function Run-Bootstrapper {
    $repoRoot = Get-RepoRoot
    $bootstrapperProject = Get-ProjectPath 'TPGLLC.Tools.DatabaseBootstrapper\TPGLLC.Tools.DatabaseBootstrapper.csproj'

    $environment = @{
        DOTNET_ENVIRONMENT = 'Production'
    }

    Invoke-DotNetCommand `
        -Description 'Database bootstrapper' `
        -WorkingDirectory $repoRoot `
        -Environment $environment `
        -Arguments @(
            'run',
            '--project', $bootstrapperProject,
            '--configuration', 'Release',
            '--no-launch-profile'
        )
}

function Get-VehicleImporterMarkerPath {
    return (Join-Path $script:StateRoot 'VehicleImporter.completed')
}

function Test-VehicleImporterCompleted {
    $markerPath = Get-VehicleImporterMarkerPath
    return Test-Path -LiteralPath $markerPath
}

function Clear-VehicleImporterCompleted {
    $markerPath = Get-VehicleImporterMarkerPath

    if (Test-Path -LiteralPath $markerPath) {
        Remove-Item -LiteralPath $markerPath -Force -ErrorAction SilentlyContinue
        Write-Log "Removed vehicle importer completion marker."
    }
}

function Set-VehicleImporterCompleted {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Stamp
    )

    $markerPath = Get-VehicleImporterMarkerPath

    $marker = [ordered]@{
        CompletedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
        DeploymentStamp = $Stamp
        CommitSha = $CommitSha
        BranchName = $BranchName
        RunnerName = $RunnerName
    }

    $markerJson = $marker | ConvertTo-Json -Depth 5
    Set-Content -Path $markerPath -Value $markerJson -Encoding UTF8
    Write-Log "Vehicle importer completion marker written to '$markerPath'."
}

function Run-VehicleImporter {
    param(
        [switch]$Force
    )

    if (-not $Force -and (Test-VehicleImporterCompleted)) {
        Write-Log "Vehicle importer already completed previously; skipping."
        return
    }

    if ($Force) {
        Write-Log "Force vehicle importer requested."
        Clear-VehicleImporterCompleted
    }

    $repoRoot = Get-RepoRoot
    $importerProject = Get-ProjectPath 'TPGLLC.Tools.VehicleImporter\TPGLLC.Tools.VehicleImporter.csproj'

    $environment = @{
        DOTNET_ENVIRONMENT = 'Production'
    }

    Invoke-DotNetCommand `
        -Description 'Vehicle importer' `
        -WorkingDirectory $repoRoot `
        -Environment $environment `
        -Arguments @(
            'run',
            '--project', $importerProject,
            '--configuration', 'Release',
            '--no-launch-profile'
        )

    Set-VehicleImporterCompleted -Stamp $script:DeploymentStamp
}

# -----------------------------
# Setup
# -----------------------------

$script:DeploymentStamp = New-DeploymentStamp
$script:LogFilePath = $null

Ensure-Directory -Path $LogRoot
Ensure-Directory -Path $BackupRoot
Ensure-Directory -Path $ReleaseRoot
Ensure-Directory -Path $WebsitePath
Ensure-Directory -Path $PublishPath

$script:LogFilePath = Join-Path $LogRoot ("deploy_{0}.log" -f $script:DeploymentStamp)

Write-Log "============================================================"
Write-Log "TPGLLC Deployment Starting"
Write-Log "Stamp       : $script:DeploymentStamp"
Write-Log "Branch      : $BranchName"
Write-Log "Commit      : $CommitSha"
Write-Log "Runner      : $RunnerName"
Write-Log "WebsitePath : $WebsitePath"
Write-Log "PublishPath  : $PublishPath"
Write-Log "BackupRoot   : $BackupRoot"
Write-Log "ReleaseRoot  : $ReleaseRoot"
Write-Log "LogRoot      : $LogRoot"
Write-Log "HealthCheck  : $HealthCheckUrl"
Write-Log "============================================================"

$script:ReleasePath = Join-Path $ReleaseRoot $script:DeploymentStamp
$script:BackupPath  = Join-Path $BackupRoot $script:DeploymentStamp
$script:OfflinePath = Join-Path $WebsitePath 'app_offline.htm'
$script:StateRoot = Join-Path $ReleaseRoot '_state'

Ensure-Directory -Path $script:StateRoot
Ensure-Directory -Path $script:ReleasePath
Ensure-Directory -Path $script:BackupPath

$deploymentSucceeded = $false

try {
    # Stop site and app pool, then back up the current website.
    Stop-IisTarget -TargetSiteName $SiteName -TargetAppPoolName $AppPoolName -OfflinePath $script:OfflinePath

    if (Test-Path -LiteralPath $WebsitePath) {
        Invoke-RobocopyMirror -Source $WebsitePath -Destination $script:BackupPath -ExcludeFiles @('app_offline.htm') -Description 'backup'
        Write-Log "Backup completed at '$script:BackupPath'."
    }
    else {
        Write-Log "Website path '$WebsitePath' does not exist yet; skipping backup." 'WARN'
    }

    # Snapshot the publish output for traceability.
    Invoke-RobocopyMirror -Source $PublishPath -Destination $script:ReleasePath -ExcludeFiles @() -Description 'release snapshot'
    Write-DeploymentMetadata -DestinationPath $script:ReleasePath -Stamp $script:DeploymentStamp

    # Deploy the new build to the website folder.
    Invoke-RobocopyMirror -Source $PublishPath -Destination $WebsitePath -ExcludeFiles @('app_offline.htm') -Description 'website deploy'
    Write-DeploymentMetadata -DestinationPath $WebsitePath -Stamp $script:DeploymentStamp

    # Optional maintenance steps (wired in now, implemented in part 2b).
    if ($RunMigrations) 
	{
		Run-Migrations
	}
	
	if ($RunBootstrapper) 
	{
		Run-Bootstrapper
	}

	if ($RunVehicleImporter) 
	{		
	Run-VehicleImporter -Force:$ForceVehicleImporter
	}

    if ($RestartIIS) {
        Start-IisTarget -TargetSiteName $SiteName -TargetAppPoolName $AppPoolName -OfflinePath $script:OfflinePath
    }
    else {
        Write-Log "RestartIIS is false; leaving IIS offline." 'WARN'
    }

    if (-not $SkipHealthCheck) {
        Test-DeploymentHealth -Url $HealthCheckUrl
    }
    else {
        Write-Log "Health check skipped by request." 'WARN'
    }

    $deploymentSucceeded = $true
    Write-Log "Deployment completed successfully."
}
catch {
    Write-Log "Deployment failed: $($_.Exception.Message)" 'ERROR'

    try {
        Write-Log "Starting rollback..." 'WARN'
        Stop-IisTarget -TargetSiteName $SiteName -TargetAppPoolName $AppPoolName -OfflinePath $script:OfflinePath
        Restore-Backup -BackupPath $script:BackupPath -DestinationPath $WebsitePath
        Write-DeploymentMetadata -DestinationPath $WebsitePath -Stamp $script:DeploymentStamp

        if ($RestartIIS) {
            Start-IisTarget -TargetSiteName $SiteName -TargetAppPoolName $AppPoolName -OfflinePath $script:OfflinePath
        }

        

        Write-Log "Rollback completed." 'WARN'
    }
    catch {
        Write-Log "Rollback failed: $($_.Exception.Message)" 'ERROR'
        throw
        if ($deploymentSucceeded) {
            exit 0
        }
        exit 1
    }

    throw
}
finally {
    Write-Log "Deployment finished. Success = $deploymentSucceeded"
}
if ($deploymentSucceeded) {
    exit 0
}
exit 1