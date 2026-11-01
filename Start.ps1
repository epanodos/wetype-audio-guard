[CmdletBinding()]
param([switch]$Foreground, [switch]$MonitorOnly)

$ErrorActionPreference = 'Stop'
$guardPath = Join-Path $PSScriptRoot 'Guard.ps1'
if ($Foreground) {
    & $guardPath -MonitorOnly:$MonitorOnly
    exit
}

# Keep the legacy mutex name so old and renamed versions cannot run together.
$existing = $null
try {
    $existing = [Threading.Mutex]::OpenExisting('Local\WeTypeAudioFix-Guard-v1')
    Write-Host 'WeType Audio Guard is already running (possibly from another folder).'
    exit
} catch [Threading.WaitHandleCannotBeOpenedException] {
    # No instance is running.
} finally {
    if ($null -ne $existing) { $existing.Dispose() }
}

$engine = (Get-Process -Id $PID).Path
$arguments = '-NoProfile -WindowStyle Hidden -File "' + $guardPath + '"'
if ($MonitorOnly) { $arguments += ' -MonitorOnly' }
$child = Start-Process -FilePath $engine -ArgumentList $arguments `
    -WorkingDirectory $PSScriptRoot -WindowStyle Hidden -PassThru `
    -RedirectStandardError (Join-Path $PSScriptRoot 'error.log')
if ($child.WaitForExit(2000)) {
    throw 'WeType Audio Guard exited during startup. Check error.log and guard.log in this folder.'
}
Write-Host ('WeType Audio Guard started. PID: ' + $child.Id)
Write-Host 'Use Stop.cmd in this folder to stop it. No startup task was installed.'
