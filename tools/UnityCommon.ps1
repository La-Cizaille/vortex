<#
.SYNOPSIS
    Shared helpers of the Unity scripts (dot-sourced): find the project's editor, check that the project is not open
    in the editor, run an editor method in batch mode.
#>

function Get-VortexUnityProject {
    Join-Path (Split-Path -Parent $PSScriptRoot) 'unity'
}

# The editor matching unity/ProjectSettings/ProjectVersion.txt, or the one named by the UNITY_EDITOR variable.
function Get-VortexUnityEditor {
    $project = Get-VortexUnityProject
    $versionLine = Select-String -Path (Join-Path $project 'ProjectSettings/ProjectVersion.txt') -Pattern '^m_EditorVersion: (.+)$'
    $version = $versionLine.Matches[0].Groups[1].Value.Trim()
    $candidates = @(
        $env:UNITY_EDITOR,
        (Join-Path $env:ProgramFiles "Unity\Hub\Editor\$version\Editor\Unity.exe"),
        "C:\Unity\$version\Editor\Unity.exe"
    ) | Where-Object { $_ -and (Test-Path $_) }
    if (-not $candidates) {
        throw "Unity $version not found. Install it with Unity Hub, or set UNITY_EDITOR to its Unity.exe."
    }

    [pscustomobject]@{ Path = @($candidates)[0]; Version = $version; Project = $project }
}

# Batch mode cannot open a project the editor has open. Only whether a command line mentions the project is checked:
# command lines are never printed, as the Hub passes an access token on them.
function Assert-VortexProjectClosed {
    $project = Get-VortexUnityProject
    $open = Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -and $_.CommandLine -notmatch 'AssetImportWorker' -and $_.CommandLine -match [regex]::Escape($project) }
    if ($open) {
        throw "The Unity editor has the project open. Close it, then run this script again."
    }
}

# Runs a static editor method in batch mode; returns the exit code. The log goes to $LogFile.
function Invoke-VortexUnityMethod {
    param(
        [Parameter(Mandatory)] [string] $Method,
        [Parameter(Mandatory)] [string] $LogFile
    )

    $editor = Get-VortexUnityEditor
    Assert-VortexProjectClosed
    Remove-Item -Force -ErrorAction SilentlyContinue $LogFile
    $arguments = @('-batchmode', '-quit', '-projectPath', "`"$($editor.Project)`"", '-executeMethod', $Method, '-logFile', "`"$LogFile`"")
    $process = Start-Process -FilePath $editor.Path -ArgumentList $arguments -Wait -PassThru -NoNewWindow
    return $process.ExitCode
}
