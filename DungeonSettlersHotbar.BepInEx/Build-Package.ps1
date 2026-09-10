#Requires -Version 7.0
[CmdletBinding()]
param([Parameter(Mandatory)][string]$GameDir)
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'DungeonSettlersHotbar.BepInEx.csproj'
$version = ([xml](Get-Content -LiteralPath $project -Raw)).Project.PropertyGroup.Version
$name = "Extended-Hotbar-BepInEx-$version"
$destination = Join-Path $PSScriptRoot "dist\$name"
$archive = "$destination.zip"
if ((Test-Path -LiteralPath $destination) -or (Test-Path -LiteralPath $archive)) {
    throw 'Output exists. Keep previous packages; use a new version for a new package.'
}
dotnet build $project -c Release "-p:GameDir=$GameDir" -p:TreatWarningsAsErrors=true
if ($LASTEXITCODE -ne 0) { throw 'BepInEx build failed; no package created.' }
dotnet run --project (Join-Path $PSScriptRoot '..\tests\Hotbar.Tests\Hotbar.Tests.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw 'Regression tests failed; no package created.' }
$dll = Join-Path $PSScriptRoot 'bin\Release\net6.0\DungeonSettlersHotbar.BepInEx.dll'
if ([Reflection.AssemblyName]::GetAssemblyName($dll).Name -ne 'DungeonSettlersHotbar.BepInEx' -or
    [Diagnostics.FileVersionInfo]::GetVersionInfo($dll).ProductVersion -ne $version) {
    throw 'Unexpected plugin identity/version.'
}
$shared = Join-Path $PSScriptRoot '..\DungeonSettlers10Slots'
$plugin = 'BepInEx/plugins/ExtendedHotbar'
$files = [ordered]@{
    "$plugin/DungeonSettlersHotbar.BepInEx.dll" = $dll
    "$plugin/DungeonSettlers10SlotsAssets/SkillFrame__sharedassets0_mod_4898.png" = (Join-Path $shared 'Assets\SkillFrame__sharedassets0_mod_4898.png')
    "$plugin/DungeonSettlers10SlotsAssets/NOTICE.txt" = (Join-Path $PSScriptRoot '..\Assets\NOTICE.txt')
    'README.md' = (Join-Path $PSScriptRoot 'README.md')
    'LICENSE' = (Join-Path $PSScriptRoot '..\LICENSE')
    'LICENSING.txt' = (Join-Path $PSScriptRoot 'LICENSING.txt')
    'THIRD_PARTY_NOTICES.txt' = (Join-Path $PSScriptRoot 'THIRD_PARTY_NOTICES.txt')
}
foreach ($source in $files.Values) {
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Missing package input: $source" }
}
foreach ($file in $files.GetEnumerator()) {
    $target = Join-Path $destination $file.Key
    New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
    Copy-Item -LiteralPath $file.Value -Destination $target
}
[IO.Compression.ZipFile]::CreateFromDirectory($destination, $archive)
$zip = [IO.Compression.ZipFile]::OpenRead($archive)
try {
    if (Compare-Object @($files.Keys | Sort-Object) @($zip.Entries.FullName | Sort-Object)) { throw 'Unexpected ZIP contents.' }
    foreach ($file in $files.GetEnumerator()) {
        $stream = $zip.GetEntry($file.Key).Open()
        try { $actual = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream)) }
        finally { $stream.Dispose() }
        if ($actual -ne (Get-FileHash -LiteralPath $file.Value -Algorithm SHA256).Hash) { throw "ZIP hash mismatch: $($file.Key)" }
    }
} finally { $zip.Dispose() }
"$((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash)  $name.zip" |
    Set-Content -LiteralPath "$archive.sha256.txt" -Encoding ascii
Write-Output $archive
