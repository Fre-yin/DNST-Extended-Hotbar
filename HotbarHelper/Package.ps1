[CmdletBinding()]
param(
    [string] $OutputDirectory = (Join-Path $PSScriptRoot '..\output\Extended-Hotbar-Helper-0.1.12-test'),
    [string] $PackagePath = (Join-Path $PSScriptRoot '..\DungeonSettlers10Slots\dist\Extended-Hotbar-0.3.8.zip'),
    [string] $BepInExPackagePath = (Join-Path $PSScriptRoot '..\DungeonSettlersHotbar.BepInEx\dist\Extended-Hotbar-BepInEx-0.3.8-bepinex.1.zip'),
    [string] $TestSavePath = (Join-Path $PSScriptRoot 'SyntheticSave.json'),
    [Alias('SignOnlineUpdate')][switch] $SignUpdatePackage
)
$ErrorActionPreference = 'Stop'
$taskStage = [IO.Path]::GetFullPath($OutputDirectory)
$taskZipPath = $taskStage + '.zip'
if ((Test-Path -LiteralPath $taskStage) -or (Test-Path -LiteralPath $taskZipPath)) { throw 'Choose new output paths. Existing packages are never overwritten.' }
& (Join-Path $PSScriptRoot 'Build.ps1') -OutputDirectory (Join-Path $PSScriptRoot 'bin-0.1.12') -PackagePath $PackagePath -BepInExPackagePath $BepInExPackagePath -TestSavePath $TestSavePath
Add-Type -AssemblyName System.IO.Compression.FileSystem
$taskManifest = [ordered]@{
    'LICENSE' = 'LICENSE'
    'LICENSING.txt' = 'LICENSING.txt'
    'THIRD_PARTY_NOTICES.txt' = 'THIRD_PARTY_NOTICES.txt'
    'Mods/DungeonSettlers10SlotsAssets/NOTICE.txt' = 'Assets/NOTICE.txt'
    'Licenses/MelonLoader-Apache-2.0.txt' = 'Licenses/MelonLoader-Apache-2.0.txt'
    'Licenses/Il2CppInterop-LGPL-3.0.txt' = 'Licenses/Il2CppInterop-LGPL-3.0.txt'
    'Licenses/HarmonyX-MIT.txt' = 'Licenses/HarmonyX-MIT.txt'
    'Licenses/Newtonsoft.Json-MIT.txt' = 'Licenses/Newtonsoft.Json-MIT.txt'
}
New-Item -ItemType Directory -Path $taskStage | Out-Null
foreach ($taskName in @('Extended-Hotbar-Helper.exe','BITTE ZUERST LESEN.txt','READ FIRST - ENGLISH.txt')) {
    $taskSource = if ($taskName.EndsWith('.exe')) { Join-Path $PSScriptRoot ('bin-0.1.12\' + $taskName) } else { Join-Path $PSScriptRoot $taskName }
    Copy-Item -LiteralPath $taskSource -Destination (Join-Path $taskStage $taskName)
}
$taskRelease = [IO.Compression.ZipFile]::OpenRead([IO.Path]::GetFullPath($PackagePath))
try {
    foreach ($taskEntryName in $taskManifest.Keys) {
        $taskEntry = $taskRelease.GetEntry($taskEntryName)
        if ($null -eq $taskEntry) { throw "Missing legal notice: $taskEntryName" }
        $taskDestination = Join-Path $taskStage $taskManifest[$taskEntryName]
        New-Item -ItemType Directory -Path (Split-Path -Parent $taskDestination) -Force | Out-Null
        [IO.Compression.ZipFileExtensions]::ExtractToFile($taskEntry, $taskDestination, $false)
    }
} finally { $taskRelease.Dispose() }
New-Item -ItemType Directory -Path (Join-Path $taskStage 'Instructions') | Out-Null
foreach ($taskLanguage in @('en','ko','fr','de','ru','zh-Hans','zh-Hant','ja','es','pt-BR')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot ('Instructions\' + $taskLanguage + '.txt')) -Destination (Join-Path $taskStage ('Instructions\' + $taskLanguage + '.txt'))
}
# Retain the 21-file update layout understood by older helpers. Include both
# loader notices in existing legal files, not newly executable/update paths.
foreach ($taskNotice in @('LICENSING.txt','THIRD_PARTY_NOTICES.txt')) {
    $taskBepNotice = [IO.File]::ReadAllText((Join-Path $PSScriptRoot ('..\DungeonSettlersHotbar.BepInEx\' + $taskNotice)))
    [IO.File]::AppendAllText((Join-Path $taskStage $taskNotice), "`r`n`r`nBEPINEX EDITION`r`n`r`n" + $taskBepNotice, [Text.UTF8Encoding]::new($false))
}
[IO.Compression.ZipFile]::CreateFromDirectory($taskStage, $taskZipPath, [IO.Compression.CompressionLevel]::Optimal, $false)
$taskArchive = [IO.Compression.ZipFile]::OpenRead($taskZipPath)
try {
    if ($taskArchive.Entries.Count -ne 21) { throw 'Unexpected package contents.' }
    foreach ($taskEntry in $taskArchive.Entries) {
        $taskInput = $taskEntry.Open()
        $taskSha = [Security.Cryptography.SHA256]::Create()
        try { $taskHash = [BitConverter]::ToString($taskSha.ComputeHash($taskInput)).Replace('-','') }
        finally { $taskInput.Dispose(); $taskSha.Dispose() }
        if ($taskHash -ne (Get-FileHash -LiteralPath (Join-Path $taskStage $taskEntry.FullName) -Algorithm SHA256).Hash) { throw ('Package verification failed: ' + $taskEntry.FullName) }
    }
} finally { $taskArchive.Dispose() }
Get-FileHash -LiteralPath $taskZipPath -Algorithm SHA256 | Format-List Path,Hash
if ($SignUpdatePackage) { & (Join-Path $PSScriptRoot 'Sign-Update.ps1') -PackagePath $taskZipPath }
