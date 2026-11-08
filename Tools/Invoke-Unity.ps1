param([Parameter(Mandatory=$true)][string]$CommandLine, [int]$TimeoutSeconds = 45)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$descriptor = Get-Content (Join-Path $projectRoot 'Library/Pipeline/.unity-pipeline-port') -Raw | ConvertFrom-Json
$headers = @{ Authorization = "Bearer $($descriptor.evalToken)" }
$body = @{ commandLine = $CommandLine } | ConvertTo-Json -Compress
Invoke-RestMethod "http://127.0.0.1:$($descriptor.port)/api/exec" -Headers $headers -Method Post -ContentType 'application/json' -Body $body -TimeoutSec $TimeoutSeconds | ConvertTo-Json -Depth 40
