[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Url,

    [string]$ExpectedText = 'Tom Parlette Garage LLC'
)

$ErrorActionPreference = 'Stop'

try {
    $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 30
} catch {
    throw "Health check request failed for $Url. $($_.Exception.Message)"
}

if ($response.StatusCode -ne 200) {
    throw "Health check failed. Expected HTTP 200 but got $($response.StatusCode)."
}

if ($ExpectedText -and ($response.Content -notmatch [regex]::Escape($ExpectedText))) {
    throw "Health check succeeded but page content did not contain expected text: $ExpectedText"
}

Write-Host "Health check succeeded for $Url"
