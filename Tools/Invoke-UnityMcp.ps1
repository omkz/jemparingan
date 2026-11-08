param([Parameter(Mandatory=$true)][string]$Type, [string]$ParamsJson = '{}', [int]$Port = 0)
$ErrorActionPreference = 'Stop'
if ($Port -eq 0) {
    # The MCP package may move its listener after a Play Mode/domain reload.
    # Read only its per-project port metadata, never a credential descriptor.
    $questAssetsPath = (Resolve-Path (Join-Path $PSScriptRoot '../Assets')).Path.Replace('\','/')
    $questHash = [Convert]::ToHexString([Security.Cryptography.SHA1]::HashData([Text.Encoding]::UTF8.GetBytes($questAssetsPath))).ToLowerInvariant().Substring(0,8)
    $questPortFile = Join-Path $env:USERPROFILE ".unity-mcp/unity-mcp-port-$questHash.json"
    $Port = 6400
    if (Test-Path -LiteralPath $questPortFile) {
        $questPortInfo = Get-Content -LiteralPath $questPortFile -Raw | ConvertFrom-Json
        if ($questPortInfo.project_path.Replace('\','/') -eq $questAssetsPath -and $questPortInfo.unity_port -gt 0) {
            $Port = [int]$questPortInfo.unity_port
        }
    }
}
$client = [System.Net.Sockets.TcpClient]::new('127.0.0.1', $Port)
try {
    $stream = $client.GetStream()
    $stream.ReadTimeout = 45000
    $stream.WriteTimeout = 45000
    while (($b = $stream.ReadByte()) -ne 10) { if ($b -lt 0) { throw 'MCP handshake ended' } }
    $payload = [Text.Encoding]::UTF8.GetBytes((@{type=$Type; params=($ParamsJson | ConvertFrom-Json)} | ConvertTo-Json -Depth 30 -Compress))
    $header = [BitConverter]::GetBytes([uint64]$payload.Length)
    if ([BitConverter]::IsLittleEndian) { [Array]::Reverse($header) }
    $stream.Write($header, 0, 8)
    $stream.Write($payload, 0, $payload.Length)
    $stream.ReadExactly($header, 0, 8)
    if ([BitConverter]::IsLittleEndian) { [Array]::Reverse($header) }
    $length = [BitConverter]::ToUInt64($header, 0)
    if ($length -gt 67108864) { throw 'MCP response exceeds limit' }
    $response = [byte[]]::new([int]$length)
    $stream.ReadExactly($response, 0, $response.Length)
    [Text.Encoding]::UTF8.GetString($response)
} finally { $client.Dispose() }
