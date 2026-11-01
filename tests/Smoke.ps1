# Read-only smoke test: no audio recording, muting or unmuting.
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
foreach ($name in @('Guard.ps1', 'Start.ps1', 'Stop.ps1')) {
    $tokens = $null
    $parseErrors = $null
    $null = [Management.Automation.Language.Parser]::ParseFile(
        (Join-Path $project $name), [ref]$tokens, [ref]$parseErrors)
    if ($parseErrors.Count -gt 0) { throw ($parseErrors | Out-String) }
}
Add-Type -Path (Join-Path $project 'CoreAudio.cs')
$sessions = @([AudioProbe.Core]::Snapshot())
foreach ($session in $sessions) {
    if ($session.Volume -lt 0 -or $session.Volume -gt 1) { throw 'Invalid volume' }
    if ($session.Flow -notin @('Render', 'Capture')) { throw 'Invalid direction' }
}
Write-Output ('PASS: scripts parse; CoreAudio compiles; enumerated ' + $sessions.Count + ' sessions without changing audio.')
