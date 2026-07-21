[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SourcePath,

    [Parameter(Mandatory = $true)]
    [string]$WebsitePath
)

$ErrorActionPreference = 'Stop'

New-Item -ItemType Directory -Force -Path $WebsitePath | Out-Null

$appOffline = Join-Path $WebsitePath 'app_offline.htm'
$offlineHtml = @'
<!doctype html>
<html>
<head><meta charset="utf-8"><title>Site Temporarily Offline</title></head>
<body>
  <h1>Site temporarily offline</h1>
  <p>Deployment in progress.</p>
</body>
</html>
'@

Set-Content -Path $appOffline -Value $offlineHtml -Encoding UTF8
Start-Sleep -Seconds 2

try {
    $null = & robocopy $SourcePath $WebsitePath /MIR /R:2 /W:2 /NFL /NDL /NJH /NJS /NP /XF app_offline.htm
    if ($LASTEXITCODE -ge 8) {
        throw "Robocopy deploy failed with exit code $LASTEXITCODE"
    }
}
finally {
    Remove-Item -Path $appOffline -Force -ErrorAction SilentlyContinue
}

Write-Host "Deployment completed to: $WebsitePath"
