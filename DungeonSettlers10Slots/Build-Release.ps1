#Requires -Version 7.0
[CmdletBinding()]
param(
    [string] $GameDir,
    # Retained for callers; new packages always include the optional demo.
    [switch] $IncludeTestSave = $true,
    # A documentation-only repack keeps the DLL version and old archives intact.
    [ValidateRange(0, 999)]
    [int] $PackageRevision = 0
)

$ErrorActionPreference = 'Stop'
if (-not $IncludeTestSave) { throw 'New releases always include the optional test save; no separate no-save variant.' }
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $PSScriptRoot 'DungeonSettlers10Slots.csproj'
$properties = ([xml](Get-Content -LiteralPath $project -Raw)).Project.PropertyGroup
$version = $properties.Version
$assemblyName = $properties.AssemblyName
if ($assemblyName -ne 'DungeonSettlers10Slots') { throw 'Unerwarteter DLL-Name; keine Paketierung.' }
$suffix = if ($PackageRevision -gt 0) { "-r$PackageRevision" } else { '' }
$destination = Join-Path $PSScriptRoot "dist\Extended-Hotbar-$version$suffix"
$archive = "$destination.zip"
if ((Test-Path -LiteralPath $destination) -or (Test-Path -LiteralPath $archive)) {
    throw 'Release-Ausgabe existiert bereits; vorhandene Pakete werden nicht überschrieben.'
}
if ([string]::IsNullOrWhiteSpace($GameDir) -or -not (Test-Path -LiteralPath $GameDir -PathType Container)) {
    throw 'Pass -GameDir with your own Dungeon Settlers folder. See BUILDING.md or BUILDING.en.md.'
}
$GameDir = (Resolve-Path -LiteralPath $GameDir).Path
$assetName = 'SkillFrame__sharedassets0_mod_4898.png'
$framePath = Join-Path $PSScriptRoot "Assets\$assetName"
if (-not (Test-Path -LiteralPath $framePath -PathType Leaf)) {
    throw 'The frame image is not included in the source repository. Add your authorized local copy under DungeonSettlers10Slots/Assets before packaging. See BUILDING.md or BUILDING.en.md.'
}
$testSave = Join-Path $PSScriptRoot 'TestSave\Saves\10SlotsTestfile.json'
if ($IncludeTestSave) {
    if (-not (Test-Path -LiteralPath $testSave -PathType Leaf)) {
        throw 'The demo save is not included in the source repository. New packages require the prepared local snapshot under TestSave/Saves; obtain it from the matching release package.'
    }
    # Package only the explicitly staged snapshot, never scan a live profile.
    $saveData = Get-Content -LiteralPath $testSave -Raw | ConvertFrom-Json -Depth 100
    if ($saveData.CampaignSaveHeader.SaveName -ne '10SlotsTestfile' -or
        $saveData.DungeonSettlers10Slots_Items.Version -ne 1) {
        throw 'Unerwarteter Testspielstand oder Erweiterungsversion; keine Paketierung.'
    }
}
dotnet build $project -c Release "-p:GameDir=$GameDir"
if ($LASTEXITCODE -ne 0) { throw 'Hotbar-Build fehlgeschlagen. Keine Paketierung.' }
$tests = Join-Path $PSScriptRoot '..\tests\Hotbar.Tests\Hotbar.Tests.csproj'
$testArguments = @('--project', $tests, '-c', 'Release')
if ($IncludeTestSave) { $testArguments += @('--', $testSave) }
dotnet run @testArguments
if ($LASTEXITCODE -ne 0) { throw 'Hotbar-Regressionstests fehlgeschlagen. Keine Paketierung.' }

$builtDll = Join-Path $PSScriptRoot "bin\Release\net6.0\$assemblyName.dll"
$builtIdentity = [Reflection.AssemblyName]::GetAssemblyName($builtDll)
if ($builtIdentity.Name -ne $assemblyName -or $builtIdentity.Version.ToString(3) -ne $version) {
    throw 'DLL-Identität oder Version weicht vom Projekt ab.'
}
$builtFileInfo = [Diagnostics.FileVersionInfo]::GetVersionInfo($builtDll)
if ($builtFileInfo.ProductName -ne 'Extended Hotbar' -or $builtFileInfo.FileDescription -ne 'Extended Hotbar') {
    throw 'Sichtbarer DLL-Name ist nicht Extended Hotbar; keine Paketierung.'
}
$packageFiles = [ordered]@{
    "Mods/$assemblyName.dll" = $builtDll
    "Mods/DungeonSettlers10SlotsAssets/$assetName" = $framePath
    'BITTE ZUERST LESEN.txt' = (Join-Path $PSScriptRoot 'UserGuide\BITTE ZUERST LESEN.txt')
    'READ FIRST - ENGLISH.txt' = (Join-Path $PSScriptRoot 'UserGuide\READ FIRST - ENGLISH.txt')
    'LICENSE' = (Join-Path $repositoryRoot 'LICENSE')
    'LICENSING.txt' = (Join-Path $repositoryRoot 'LICENSING.txt')
    'THIRD_PARTY_NOTICES.txt' = (Join-Path $repositoryRoot 'THIRD_PARTY_NOTICES.txt')
    'Mods/DungeonSettlers10SlotsAssets/NOTICE.txt' = (Join-Path $repositoryRoot 'Assets\NOTICE.txt')
    'Licenses/MelonLoader-Apache-2.0.txt' = (Join-Path $repositoryRoot 'Licenses\MelonLoader-Apache-2.0.txt')
    'Licenses/Il2CppInterop-LGPL-3.0.txt' = (Join-Path $repositoryRoot 'Licenses\Il2CppInterop-LGPL-3.0.txt')
    'Licenses/HarmonyX-MIT.txt' = (Join-Path $repositoryRoot 'Licenses\HarmonyX-MIT.txt')
    'Licenses/Newtonsoft.Json-MIT.txt' = (Join-Path $repositoryRoot 'Licenses\Newtonsoft.Json-MIT.txt')
}
if ($IncludeTestSave) {
    $packageFiles['Testspielstand/Saves/10SlotsTestfile.json'] = $testSave
    $packageFiles['Testspielstand/BITTE ZUERST LESEN.txt'] = Join-Path $PSScriptRoot 'TestSave\BITTE ZUERST LESEN.txt'
    $packageFiles['Testspielstand/READ FIRST - ENGLISH.txt'] = Join-Path $PSScriptRoot 'TestSave\READ FIRST - ENGLISH.txt'
}
foreach ($source in $packageFiles.Values) {
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Paketdatei fehlt: $source" }
}
foreach ($entry in $packageFiles.GetEnumerator()) {
    $target = Join-Path $destination $entry.Key
    New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
    Copy-Item -LiteralPath $entry.Value -Destination $target
}
$actualPaths = @(Get-ChildItem -LiteralPath $destination -Recurse -File | ForEach-Object {
    [IO.Path]::GetRelativePath($destination, $_.FullName).Replace('\', '/')
})
if (Compare-Object @($packageFiles.Keys | Sort-Object) @($actualPaths | Sort-Object)) {
    throw 'Unerwarteter Paketinhalt.'
}
[IO.Compression.ZipFile]::CreateFromDirectory($destination, $archive)
$opened = [IO.Compression.ZipFile]::OpenRead($archive)
try {
    if (Compare-Object @($packageFiles.Keys | Sort-Object) @($opened.Entries.FullName | Sort-Object)) {
        throw 'Unerwarteter ZIP-Inhalt.'
    }
    foreach ($entry in $packageFiles.GetEnumerator()) {
        $stream = $opened.GetEntry($entry.Key).Open()
        $hash = [Security.Cryptography.SHA256]::Create()
        try { $actualHash = ([BitConverter]::ToString($hash.ComputeHash($stream))).Replace('-', '') }
        finally { $hash.Dispose(); $stream.Dispose() }
        if ($actualHash -ne (Get-FileHash -LiteralPath $entry.Value -Algorithm SHA256).Hash) {
            throw "ZIP-Prüfsumme weicht ab: $($entry.Key)"
        }
    }
} finally { $opened.Dispose() }
Get-FileHash -LiteralPath $archive -Algorithm SHA256
Write-Output "Extended Hotbar user package: $archive ($($packageFiles.Count)/$($packageFiles.Count) ZIP entries verified; no QA, legacy DLL or developer documents)"
