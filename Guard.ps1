param([switch]$MonitorOnly)
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$guardLock=[Threading.Mutex]::new($false,'Local\WeTypeAudioFix-Guard-v1')
try { $acquired=$guardLock.WaitOne(0) } catch [Threading.AbandonedMutexException] { $acquired=$true }
if(-not $acquired){$guardLock.Dispose();exit}
try {
 Add-Type -Path "$root\CoreAudio.cs"
 $stopFile=Join-Path $root 'stop.request'
 if(Test-Path -LiteralPath $stopFile){Remove-Item -LiteralPath $stopFile}
 function Write-GuardLog([string]$Message){
  $file=Join-Path $root 'guard.log'
  if((Test-Path -LiteralPath $file) -and (Get-Item -LiteralPath $file).Length -gt 1048576){Set-Content -LiteralPath $file -Value '' -Encoding utf8}
  Add-Content -LiteralPath $file -Value ((Get-Date).ToString('o')+' '+$Message) -Encoding utf8
 }
 $PID | Set-Content -LiteralPath "$root\guard.pid"
 Write-GuardLog 'START: only Chrome/Edge; restore once at WeType capture start; no volume changes'
 $history=[Collections.Generic.List[object]]::new()
 $wasRecording=$null
 $armed=@{}
 $restored=@{}
 $started=[datetime]::MinValue
 $errors=0
 while(-not (Test-Path -LiteralPath $stopFile)) {
  try {
   $now=[datetime]::UtcNow
   $sessions=@([AudioProbe.Core]::Snapshot())
   $capture=@($sessions | Where-Object { $_.Flow -eq 'Capture' -and $_.State -eq 1 -and $_.Process -in @('wetype_update','wetype_server','wetype_renderer') })
   $recording=$capture.Count -gt 0
   $render=@($sessions | Where-Object { $_.Flow -eq 'Render' -and $_.Process -in @('chrome','msedge') })
   if($recording -and $wasRecording -eq $false) {
    $armed=@{}; $restored=@{}; $started=$now
    $baseline=@($history | Where-Object {$_.Time -le $now.AddMilliseconds(-300)} | Select-Object -Last 1)
    if($baseline.Count -eq 0){$baseline=@($history | Select-Object -First 1)}
    if($baseline.Count -gt 0){foreach($s in $baseline[0].Sessions){if(-not $s.Mute -and $s.Volume -gt 0 -and $s.State -eq 1){$armed[$s.Key]=$true}}}
    Write-GuardLog ('CAPTURE_START process='+($capture.Process -join ',')+' eligible='+$armed.Count)
   }
   if($recording -and ($now-$started).TotalSeconds -lt 2) {
    foreach($s in $render){
     if($armed.ContainsKey($s.Key) -and -not $restored.ContainsKey($s.Key) -and $s.Mute){
      if(-not $MonitorOnly){
       $readback=@([AudioProbe.Core]::SetMute($s.Key,$false) | Where-Object {$_.Key -eq $s.Key})
       if($readback.Count -eq 0 -or $readback[0].Mute){throw 'Unmute readback failed'}
      }
      $restored[$s.Key]=$true
      Write-GuardLog ('UNMUTE process='+$s.Process+' pid='+$s.Pid+' observedElapsedMs='+[int]([datetime]::UtcNow-$started).TotalMilliseconds+' volume='+$s.Volume+' verified='+(-not $MonitorOnly))
     }
    }
   }
   if(-not $recording){
    if($wasRecording -eq $true){Write-GuardLog 'CAPTURE_END';$history.Clear();$armed=@{};$restored=@{}}
    $history.Add([pscustomobject]@{Time=$now;Sessions=$render})
    while($history.Count -gt 1 -and $history[0].Time -lt $now.AddSeconds(-2)){$history.RemoveAt(0)}
   }
   $wasRecording=$recording
   $errors=0
  } catch {
   $errors++; Write-GuardLog ('ERROR '+$_.Exception.Message)
   if($errors -ge 10){throw}
  }
  Start-Sleep -Milliseconds 100
 }
 Write-GuardLog 'STOP requested'
} finally {
 $guardLock.ReleaseMutex();$guardLock.Dispose()
}
