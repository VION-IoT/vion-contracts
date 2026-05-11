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

# --- Patch ValidateVersion calls ---
# flatc emits `FlatBufferConstants.FLATBUFFERS_<MAJOR>_<MINOR>_<PATCH>()` matching its OWN
# version. The Google.FlatBuffers NuGet package only defines the constant for ITS published
# version, which can lag flatc (e.g. flatc 25.12.19 vs NuGet 25.2.10 at time of writing).
# A newer-than-pinned flatc generates code that won't compile against the pinned runtime.
# Post-process all generated .cs files to reference the pinned NuGet runtime constant.
# Rev this string if the Google.FlatBuffers PackageReference is bumped.
$nugetVersionConstant = "FLATBUFFERS_25_2_10"
$generatedFiles = Get-ChildItem -Recurse -Filter *.cs -Path $out
$patchedCount = 0
foreach ($f in $generatedFiles) {
  $content = Get-Content $f.FullName -Raw
  $patched = $content -replace 'FLATBUFFERS_\d+_\d+_\d+', $nugetVersionConstant
  if ($content -ne $patched) {
    Set-Content -Path $f.FullName -Value $patched -NoNewline
    $patchedCount++
  }
}
if ($patchedCount -gt 0) {
  Write-Host "ℹ️  Patched $patchedCount file(s) to use $nugetVersionConstant (matches Google.FlatBuffers NuGet pin)"
}

Write-Host "✅ Generation complete!"
Write-Host "✅ Output: $(Resolve-Path $out)"
