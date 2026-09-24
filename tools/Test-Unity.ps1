<#
.SYNOPSIS
    Runs the Unity project's tests in batch mode and prints a summary.

.DESCRIPTION
    Finds the editor matching unity/ProjectSettings/ProjectVersion.txt (or the one given by the
    UNITY_EDITOR environment variable), runs the tests of the requested platform, then reads the
    NUnit result file. Exit code: 0 when every test passed, 1 otherwise, 2 on a setup problem.

.EXAMPLE
    ./tools/Test-Unity.ps1
    ./tools/Test-Unity.ps1 -Platform PlayMode
#>
param(
    [ValidateSet('EditMode', 'PlayMode')]
    [string] $Platform = 'EditMode'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'unity'
$versionLine = Select-String -Path (Join-Path $project 'ProjectSettings/ProjectVersion.txt') -Pattern '^m_EditorVersion: (.+)$'
$version = $versionLine.Matches[0].Groups[1].Value.Trim()

$candidates = @(
    $env:UNITY_EDITOR,
    (Join-Path $env:ProgramFiles "Unity\Hub\Editor\$version\Editor\Unity.exe"),
    "C:\Unity\$version\Editor\Unity.exe"
) | Where-Object { $_ -and (Test-Path $_) }
if (-not $candidates) {
    Write-Error "Unity $version not found. Install it with Unity Hub, or set UNITY_EDITOR to its Unity.exe."
    exit 2
}

$unity = @($candidates)[0]
$results = Join-Path $env:TEMP "vortex-unity-$Platform-results.xml"
$log = Join-Path $env:TEMP "vortex-unity-$Platform.log"
Remove-Item -Force -ErrorAction SilentlyContinue $results, $log

Write-Output "Unity $version, $Platform tests..."
$arguments = @('-batchmode', '-projectPath', "`"$project`"", '-runTests', '-testPlatform', $Platform, '-testResults', "`"$results`"", '-logFile', "`"$log`"")
$process = Start-Process -FilePath $unity -ArgumentList $arguments -Wait -PassThru -NoNewWindow

if (-not (Test-Path $results)) {
    Write-Error "No test results (exit code $($process.ExitCode)). See the log: $log"
    exit 2
}

[xml] $xml = Get-Content -Raw $results
$run = $xml.'test-run'
Write-Output ("{0} tests: {1} passed, {2} failed, {3} skipped." -f $run.total, $run.passed, $run.failed, $run.skipped)
foreach ($case in $xml.SelectNodes("//test-case[@result='Failed']")) {
    Write-Output ("FAILED {0}: {1}" -f $case.fullname, $case.failure.message.'#cdata-section')
}

if ([int] $run.failed -gt 0) { exit 1 }
exit 0
