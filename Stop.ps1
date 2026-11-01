$guardRoot=$PSScriptRoot
Set-Content -LiteralPath (Join-Path $guardRoot 'stop.request') -Value 'stop' -Encoding ascii
