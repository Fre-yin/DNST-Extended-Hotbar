[CmdletBinding()]
param(
    [string] $OutputDirectory = (Join-Path $PSScriptRoot '..\output\Extended-Hotbar-Helper-0.1.11-test'),
    [string] $PackagePath = (Join-Path $PSScriptRoot '..\DungeonSettlers10Slots\dist\standard-0.3.7\Extended-Hotbar-0.3.7.zip'),
    [string] $TestSavePath = (Join-Path $PSScriptRoot 'SyntheticSave.json'),
    [Alias('SignOnlineUpdate')][switch] $SignUpdatePackage
)
$ErrorActionPreference = 'Stop'
$taskStage = [IO.Path]::GetFullPath($OutputDirectory)
$taskZipPath = $taskStage + '.zip'
if ((Test-Path -LiteralPath $taskStage) -or (Test-Path -LiteralPath $taskZipPath)) { throw 'Choose new output paths. Existing packages are never overwritten.' }
& (Join-Path $PSScriptRoot 'Build.ps1') -PackagePath $PackagePath -TestSavePath $TestSavePath
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
    $taskSource = if ($taskName.EndsWith('.exe')) { Join-Path $PSScriptRoot ('bin\' + $taskName) } else { Join-Path $PSScriptRoot $taskName }
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
