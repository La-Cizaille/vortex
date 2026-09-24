<#
.SYNOPSIS
    Creates the Unity project's missing base assets in batch mode (theme, catalogs, prefabs, scenes, texts...).

.DESCRIPTION
    Runs Vortex.Editor.ProjectAssets.EnsureAll, the same as the editor menu
    "Vortex > Développement > Créer les assets de base manquants". Existing assets are never overwritten: to rebuild one
    from code, delete it first. Adds the missing interface texts to the text table. The editor must be closed.
    Exit code: 0 on success, 1 otherwise.

.EXAMPLE
    ./tools/Update-UnityAssets.ps1
#>
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'UnityCommon.ps1')

$log = Join-Path $env:TEMP 'vortex-unity-assets.log'
$editor = Get-VortexUnityEditor
Write-Output "Unity $($editor.Version), creating the missing assets..."
$code = Invoke-VortexUnityMethod -Method 'Vortex.Editor.ProjectAssets.EnsureAll' -LogFile $log
Select-String -Path $log -Pattern 'Created |not created|error CS|Exception' | ForEach-Object { $_.Line }
if ($code -ne 0) {
    Write-Error "Unity exited with code $code. See the log: $log"
    exit 1
}

Write-Output 'Done.'
exit 0
