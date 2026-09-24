<#
.SYNOPSIS
  Refreshes the embedded VID/PID database (Data\usb.ids) from linux-usb.org.
.DESCRIPTION
  Downloads http://www.linux-usb.org/usb.ids, keeps vendor lines ("VID  name")
  and device lines (single-tab "PID  name"), discards comments, C and interface
  lines, and writes Data\usb.ids with a version header. Rebuild afterwards.
#>
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$tmp = Join-Path ([IO.Path]::GetTempPath()) 'usb.ids'
$dst = Join-Path $root 'Data\usb.ids'

Write-Host "Downloading http://www.linux-usb.org/usb.ids ..."
curl.exe -L --max-time 60 -o $tmp http://www.linux-usb.org/usb.ids

$version = ''
$vdate = ''
$kept = New-Object Collections.Generic.List[string]
foreach ($line in [IO.File]::ReadLines($tmp)) {
    if ($line -match '^# Version:\s*(.+)$') { $version = $Matches[1].Trim(); continue }
    if ($line -match '^# Date:\s*(.+)$') { $vdate = $Matches[1].Trim(); continue }
    if ($line -eq '' -or $line.StartsWith('#')) { continue }
    if ($line -match '^[0-9a-fA-F]{4}  \S') { $kept.Add($line.TrimEnd()); continue }
    if ($line -match '^\t[0-9a-fA-F]{4}  \S') { $kept.Add($line.TrimEnd()); continue }
}

$header = @(
    '# USBScope VID/PID database, trimmed from linux-usb.org usb.ids',
    '# (vendor lines + device lines only; comments, C and interface lines removed).',
    '# Source: http://www.linux-usb.org/usb.ids',
    "# Upstream-Version: $version",
    "# Upstream-Date: $vdate",
    "# Generated: $(Get-Date -Format yyyy-MM-dd) (see tools/Update-UsbIds.ps1 to refresh)."
)
$out = $header + $kept
[IO.File]::WriteAllLines($dst, $out, [Text.UTF8Encoding]::new($false))
$vids = ($kept | Where-Object { $_ -notmatch '^\t' }).Count
$pids = $kept.Count - $vids
Write-Host "Wrote $dst ($vids vendors, $pids devices)."
