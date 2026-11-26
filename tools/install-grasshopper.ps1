# Build Muscle and install it in Grasshopper.
#
# Usage, from the repository root (Rhino closed):
#     powershell -ExecutionPolicy Bypass -File tools\install-grasshopper.ps1
# Options:
#     -NoBuild   install the files already built, without building first
#
# Copies Muscle2.gha, MuscleApp.dll and MuscleCore.dll (with their .pdb) from
# src\Muscle\bin\Debug\net48 to %APPDATA%\Grasshopper\Libraries\Muscle 2.0, and Python.Runtime.dll
# if it is missing there. Refuses to run while Rhino is open: Grasshopper locks the loaded plugin,
# and a half-copied plugin would mix old and new files.
#
# The plugin and musclepy must match (MusclePyVersion.Expected in MuscleCore): after installing,
# check the "musclepy ... loaded from ..." remark on the StartPython.NET component.

param([switch]$NoBuild)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$source = Join-Path $repo 'src\Muscle\bin\Debug\net48'
$target = Join-Path $env:APPDATA 'Grasshopper\Libraries\Muscle 2.0'
$files = @('Muscle2.gha', 'MuscleApp.dll', 'MuscleCore.dll')

# 1) Rhino must be closed
$rhino = Get-Process | Where-Object { $_.ProcessName -match '^Rhino' }
if ($rhino) {
    Write-Host "Rhino is open (process: $(($rhino | ForEach-Object { $_.ProcessName }) -join ', ')). Close Rhino, then run this script again. Nothing was copied." -ForegroundColor Red
    exit 1
}

# 2) Build
if (-not $NoBuild) {
    Write-Host "Building Muscle.sln..."
    & dotnet build (Join-Path $repo 'Muscle.sln') -v q
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed. Nothing was copied." -ForegroundColor Red
        exit 1
    }
}

# 3) Check the build output
foreach ($f in $files) {
    if (-not (Test-Path (Join-Path $source $f))) {
        Write-Host "Missing $f in $source. Build first (run without -NoBuild). Nothing was copied." -ForegroundColor Red
        exit 1
    }
}

# 4) Copy
New-Item -ItemType Directory -Force -Path $target | Out-Null
$toCopy = @()
foreach ($f in $files) {
    $toCopy += $f
    $pdb = [IO.Path]::ChangeExtension($f, '.pdb')
    if (Test-Path (Join-Path $source $pdb)) { $toCopy += $pdb }
}
if (-not (Test-Path (Join-Path $target 'Python.Runtime.dll'))) { $toCopy += 'Python.Runtime.dll' }
foreach ($f in $toCopy) {
    Copy-Item -Path (Join-Path $source $f) -Destination (Join-Path $target $f) -Force
}

# 5) Verify
$ok = $true
foreach ($f in $toCopy) {
    $a = (Get-FileHash (Join-Path $source $f)).Hash
    $b = (Get-FileHash (Join-Path $target $f)).Hash
    if ($a -ne $b) { $ok = $false; Write-Host "  $f differs after copy" -ForegroundColor Red }
    else { Write-Host ("  {0,-20} {1}" -f $f, (Get-Item (Join-Path $target $f)).LastWriteTime) }
}
if (-not $ok) { exit 1 }

$expected = Select-String -Path (Join-Path $repo 'src\MuscleCore\PythonNETInit\MusclePyVersion.cs') -Pattern 'Expected\s*=\s*"([^"]+)"'
Write-Host "Installed in $target." -ForegroundColor Green
Write-Host "This Muscle2.gha expects musclepy $($expected.Matches[0].Groups[1].Value)."
