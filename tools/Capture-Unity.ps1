<#
.SYNOPSIS
    Renders a scene of the Unity project to a PNG image in batch mode, without opening the editor window.

.DESCRIPTION
    Game: five bots play until the requested round, then the table is rendered (1920 x 1080). Gallery: every card and
    ship. A way to check a layout, a new illustration or a new model in context. The editor must be closed.

.EXAMPLE
    ./tools/Capture-Unity.ps1 -Scene Game -Round 6 -Out captures/table.png
    ./tools/Capture-Unity.ps1 -Scene Gallery -Out captures/gallery.png
#>
param(
    [ValidateSet('Game', 'Gallery')]
    [string] $Scene = 'Game',
    [Parameter(Mandatory)]
    [string] $Out,
    [ValidateRange(1, 30)]
    [int] $Round = 4
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'UnityCommon.ps1')

if ([System.IO.Path]::GetExtension($Out) -ne '.png') {
    Write-Error 'The output must be a .png file.'
    exit 1
}

$target = [System.IO.Path]::GetFullPath($Out)
New-Item -ItemType Directory -Force (Split-Path -Parent $target) | Out-Null
$env:VORTEX_CAPTURE = $target
$env:VORTEX_ROUND = "$Round"
$log = Join-Path $env:TEMP "vortex-unity-capture.log"
$code = Invoke-VortexUnityMethod -Method "Vortex.Editor.SceneCapture.$Scene" -LogFile $log
if ($code -ne 0 -or -not (Test-Path $target)) {
    Select-String -Path $log -Pattern 'error CS|Exception' | Select-Object -First 5 | ForEach-Object { $_.Line }
    Write-Error "No capture (exit code $code). See the log: $log"
    exit 1
}

Write-Output "Captured $Scene to $target"
exit 0
