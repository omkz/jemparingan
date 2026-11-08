$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $PSScriptRoot '../Assets/Scripts/JemparinganQuestGestureGate.cs')
$rows = [System.Collections.Generic.List[object]]::new()
function Check([string]$name, [bool]$actual, [bool]$expected) {
    $rows.Add([pscustomobject]@{ check=$name; pass=($actual -eq $expected); accepted=$actual; expected=$expected })
}
$gate = [JemparinganQuestGestureGate]::new()
Check 'Startup with held bow/string is blocked' ($gate.Accept($true,$true,$true)) $false
Check 'Opening string before neutral remains blocked' ($gate.Accept($true,$true,$false)) $false
Check 'Both hands neutral arms input' ($gate.Accept($true,$false,$false)) $true
Check 'Left bow pinch is accepted' ($gate.Accept($true,$true,$false)) $true
Check 'Right string pinch is accepted' ($gate.Accept($true,$true,$true)) $true
Check 'Normal string release is accepted' ($gate.Accept($true,$true,$false)) $true
Check 'Invalid tracking sample is rejected' ($gate.Accept($false,$true,$true)) $false
Check 'Reacquired held draw remains blocked' ($gate.Accept($true,$true,$true)) $false
Check 'Reacquired release remains blocked' ($gate.Accept($true,$true,$false)) $false
Check 'Neutral after tracking loss is accepted' ($gate.Accept($true,$false,$false)) $true
Check 'Fresh draw after recovery is accepted' ($gate.Accept($true,$true,$true)) $true
Check 'Dropping bow while string held cancels' ($gate.Accept($true,$false,$true)) $false
Check 'Regripping bow with held string remains blocked' ($gate.Accept($true,$true,$true)) $false
Check 'Neutral after dropped bow is accepted' ($gate.Accept($true,$false,$false)) $true
$gate.Interrupt()
Check 'Explicit focus/pause/disable interruption blocks held input' ($gate.Accept($true,$true,$true)) $false
Check 'Explicit interruption requires both hands neutral' ($gate.Accept($true,$false,$true)) $false
Check 'Both neutral rearm after explicit interruption' ($gate.Accept($true,$false,$false)) $true
$report = [pscustomobject]@{
    method='Actual production gesture gate compiled with Add-Type; synthetic samples, no headset or Unity interaction claim'
    pass=(@($rows | Where-Object { !$_.pass }).Count -eq 0)
    checks=$rows
}
$report | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $PSScriptRoot '../docs/validation/quest-vr-v1/gesture-gate.json')
$report | ConvertTo-Json -Depth 5
if (!$report.pass) { throw 'Quest gesture gate regression failed' }
