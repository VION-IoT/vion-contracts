$ErrorActionPreference = 'Stop'

# --- Resolve flatc ---
$flatc = $env:FLATC
if (-not $flatc -or -not (Test-Path $flatc)) {
  try { $flatcInPath = (& where.exe flatc 2>$null | Select-Object -First 1) } catch { $flatcInPath = $null }
  if ($flatcInPath -and (Test-Path $flatcInPath)) { $flatc = $flatcInPath }
}
while (-not $flatc -or -not (Test-Path $flatc)) {
  $flatc = Read-Host "Enter full path to flatc.exe (e.g. C:\tools\flatc\flatc.exe)"
  if (-not (Test-Path $flatc)) { Write-Host "❌ '$flatc' not found. Try again." -ForegroundColor Red; $flatc = $null }
}
Write-Host "Using flatc: $flatc"

# --- Paths ---
$root = $PSScriptRoot
$out  = Join-Path $PSScriptRoot "..\FlatBuffers.Generated"

# --- Clean output ---
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory -Path $out | Out-Null

# --- Collect all schemas ---
$files = Get-ChildItem -Recurse -Filter *.fbs -Path $root | ForEach-Object { $_.FullName }
if (-not $files -or $files.Count -eq 0) { throw "No .fbs files found under $root" }

Write-Host "Generating FlatBuffers C# from $($files.Count) schema(s)..."
& $flatc --csharp --gen-all -o $out -I $root @files

# --- Flatten: move contents of Vion\Contracts\FlatBuffers up into $out ---
# Move/merge by copying everything, then delete the Vion tree
$anchor = Join-Path $out "Vion\Contracts\FlatBuffers"
if (Test-Path $anchor) {
  Copy-Item -Recurse -Force "$anchor\*" $out
}

Remove-Item -Recurse -Force (Join-Path $out 'Vion') -ErrorAction SilentlyContinue

Write-Host "✅ Generation complete!"
Write-Host "✅ Output: $(Resolve-Path $out)"
