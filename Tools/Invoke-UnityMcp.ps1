param([Parameter(Mandatory=$true)][string]$Type, [string]$ParamsJson = '{}', [int]$Port = 6400)
$ErrorActionPreference = 'Stop'
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
