<#
.SYNOPSIS
    Renders a scene of the Unity project to a PNG image in batch mode, without opening the editor window.

.DESCRIPTION
    Game: five bots play until the requested round, then the table is rendered (1920 x 1080); with -Human, the first
    seat is a person's and the table is rendered on their turn, controls offered, after the market or (-Phase Market)
    during it; -Phase Aim shows an attack aimed at an opponent, with the engine's preview; -Phase Pause the pause menu;
    -Phase End the end of game panel (the game is played to its end); -Phase Log the open game log; -Phase Decision (with
    -Human) the person's first decision, answered on the table; -Phase Effects the animations frozen mid-way (-EffectTime s after the shot); -TurnSeconds N times
    the turns (the timer shows on the person's turn). Menu: the home screen, or -Phase Local, Dev or
    Options. Gallery: every card and ship. A way
    to check a layout, a new illustration or a new model in context. The editor must be closed.

.EXAMPLE
    ./tools/Capture-Unity.ps1 -Scene Game -Round 6 -Out captures/table.png
    ./tools/Capture-Unity.ps1 -Scene Game -Round 3 -Human -Out captures/my-turn.png
    ./tools/Capture-Unity.ps1 -Scene Game -Round 3 -Human -Phase Aim -Out captures/aim.png
    ./tools/Capture-Unity.ps1 -Scene Gallery -Out captures/gallery.png
    ./tools/Capture-Unity.ps1 -Scene Menu -Phase Local -Out captures/local-game.png
#>
param(
    [ValidateSet('Game', 'Gallery', 'Menu')]
    [string] $Scene = 'Game',
    [Parameter(Mandatory)]
    [string] $Out,
    [ValidateRange(1, 30)]
    [int] $Round = 4,
    [switch] $Human,
    [ValidateSet('Actions', 'Market', 'Aim', 'Pause', 'End', 'Log', 'Decision', 'Effects', 'Home', 'Local', 'Dev', 'Options')]
    [string] $Phase = 'Actions',
    [ValidateRange(0, 600)]
    [int] $TurnSeconds = 0,
    [ValidateRange(0.0, 5.0)]
    [double] $EffectTime = 0.35
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
$env:VORTEX_HUMAN = if ($Human) { '1' } else { '0' }
$env:VORTEX_PHASE = $Phase.ToLowerInvariant()
$env:VORTEX_TURN_SECONDS = "$TurnSeconds"
$env:VORTEX_EFFECT_TIME = $EffectTime.ToString([System.Globalization.CultureInfo]::InvariantCulture)
$log = Join-Path $env:TEMP "vortex-unity-capture.log"
$code = Invoke-VortexUnityMethod -Method "Vortex.Editor.SceneCapture.$Scene" -LogFile $log
if ($code -ne 0 -or -not (Test-Path $target)) {
    Select-String -Path $log -Pattern 'error CS|Exception' | Select-Object -First 5 | ForEach-Object { $_.Line }
    Write-Error "No capture (exit code $code). See the log: $log"
    exit 1
}

Write-Output "Captured $Scene to $target"
exit 0
