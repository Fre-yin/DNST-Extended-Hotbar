[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string] $PackagePath,
    [Parameter(Mandatory=$true)][string] $OutputPath,
    [ValidateSet('MelonLoader','BepInEx')][string] $Loader = 'MelonLoader',
    [string] $KeyPath = (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'ExtendedHotbarHelper\PublisherKeys\release-signing.dpapi'),
    [string] $PublicKeyPath = (Join-Path $PSScriptRoot 'UpdateTrust.xml')
)
# Publisher-only packaging step. Never included in the end-user ZIP, never online.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$taskInput = [IO.Path]::GetFullPath($PackagePath)
$taskOutput = [IO.Path]::GetFullPath($OutputPath)
$taskPrefix = if ($Loader -eq 'BepInEx') { 'Extended-Hotbar-BepInEx-' } else { 'Extended-Hotbar-' }
$taskSuffix = if ($Loader -eq 'BepInEx') { '-bepinex\.[1-9][0-9]*' } else { '(?:-mit-Testspielstand)?' }
if ([IO.Path]::GetFileName($taskInput) -notmatch ('^' + $taskPrefix + '((?:0|[1-9][0-9]{0,4})\.(?:0|[1-9][0-9]{0,4})\.(?:0|[1-9][0-9]{0,4}))' + $taskSuffix + '\.zip$')) { throw 'Use an original versioned mod ZIP, not a helper or source archive.' }
$taskVersion = $Matches[1]
if ([IO.Path]::GetFileName($taskOutput) -cne [IO.Path]::GetFileName($taskInput)) { throw 'Keep the mod ZIP filename; choose another existing output folder.' }
if (Test-Path -LiteralPath $taskOutput) { throw 'Output exists. Never overwrite a release archive.' }
if (-not [IO.Directory]::Exists([IO.Path]::GetDirectoryName($taskOutput))) { throw 'Create a separate output folder first.' }
if ((Get-Item -LiteralPath $taskInput).Length -gt 16777216) { throw 'Mod ZIP exceeds 16 MiB.' }
$taskRoot = if ($Loader -eq 'BepInEx') { 'BepInEx/plugins/ExtendedHotbar' } else { 'Mods' }
$taskAssembly = if ($Loader -eq 'BepInEx') { 'DungeonSettlersHotbar.BepInEx' } else { 'DungeonSettlers10Slots' }
$taskPurpose = if ($Loader -eq 'BepInEx') { 'ExtendedHotbar.Mod.BepInEx.v1' } else { 'ExtendedHotbar.Mod.v1' }
$taskOwned = @("$taskRoot/$taskAssembly.dll", "$taskRoot/DungeonSettlers10SlotsAssets/SkillFrame__sharedassets0_mod_4898.png", "$taskRoot/DungeonSettlers10SlotsAssets/NOTICE.txt")
$taskArchive = [IO.Compression.ZipFile]::OpenRead($taskInput)
$taskPrivate = $null; $taskRsa = $null
try {
    $taskNames = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $taskFiles = [ordered]@{}; [long]$taskTotal = 0
    if ($taskArchive.Entries.Count -gt 127 -or $null -ne $taskArchive.GetEntry('ExtendedHotbar.update.json')) { throw 'Too many files or manifest already present.' }
    foreach ($taskEntry in $taskArchive.Entries) {
        if (-not $taskNames.Add($taskEntry.FullName) -or $taskEntry.FullName -match '(^/|\\|:|(^|/)\.\.?(/|$)|[. ](/|$)|//)' -or $taskEntry.Length -gt 4194304) { throw 'Unsafe or oversized ZIP entry.' }
        $taskTotal += $taskEntry.Length
        if ($taskTotal -gt 33554432) { throw 'Expanded package too large.' }
        if ($taskOwned -ccontains $taskEntry.FullName) {
            $taskStream = $taskEntry.Open(); $taskSha = [Security.Cryptography.SHA256]::Create()
            try { $taskFiles[$taskEntry.FullName] = [BitConverter]::ToString($taskSha.ComputeHash($taskStream)).Replace('-','') }
            finally { $taskSha.Dispose(); $taskStream.Dispose() }
        }
    }
    if ($taskFiles.Count -ne 3) { throw 'Expected three installable hotbar files.' }
    # These are reviewed helper build inputs, not values supplied by downloaded data.
    $taskSource = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'ReleaseInfo.cs'))
    $taskGameHash = [regex]::Match($taskSource, 'const string GameHash = "([A-F0-9]{64})"').Groups[1].Value
    $taskMetadataHash = [regex]::Match($taskSource, 'const string MetadataHash = "([A-F0-9]{64})"').Groups[1].Value
    $taskHelperSource = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Updates.cs'))
    $taskMinimum = [regex]::Match($taskHelperSource, 'const string HelperVersion = "([0-9]+\.[0-9]+\.[0-9]+)"').Groups[1].Value
    if ($taskGameHash.Length -ne 64 -or $taskMetadataHash.Length -ne 64 -or $taskMinimum.Length -eq 0) { throw 'Missing reviewed compatibility metadata.' }
    $taskPayload = [ordered]@{ purpose=$taskPurpose; repository='Fre-yin/DNST-Extended-Hotbar'; modVersion=$taskVersion; minimumHelper=$taskMinimum; gameHash=$taskGameHash; metadataHash=$taskMetadataHash; files=$taskFiles }
    $taskBytes = [Text.Encoding]::UTF8.GetBytes(($taskPayload | ConvertTo-Json -Depth 5 -Compress))
    $taskPrivate = [Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes([IO.Path]::GetFullPath($KeyPath)), $null, [Security.Cryptography.DataProtectionScope]::CurrentUser)
    $taskRsa = [Security.Cryptography.RSACryptoServiceProvider]::new([Security.Cryptography.CspParameters]::new(24)); $taskRsa.PersistKeyInCsp = $false
    $taskRsa.FromXmlString([Text.Encoding]::UTF8.GetString($taskPrivate))
    if ($taskRsa.KeySize -ne 4096 -or $taskRsa.ToXmlString($false) -cne [IO.File]::ReadAllText([IO.Path]::GetFullPath($PublicKeyPath))) { throw 'Publisher key does not match pinned public key.' }
    $taskSignature = $taskRsa.SignData($taskBytes, [Security.Cryptography.CryptoConfig]::MapNameToOID('SHA256'))
    if (-not $taskRsa.VerifyData($taskBytes, [Security.Cryptography.CryptoConfig]::MapNameToOID('SHA256'), $taskSignature)) { throw 'Signature self-check failed.' }
    $taskEnvelope = [Text.Encoding]::UTF8.GetBytes(([ordered]@{ format=1; payload=[Convert]::ToBase64String($taskBytes); signature=[Convert]::ToBase64String($taskSignature) } | ConvertTo-Json -Compress))
    $taskFile = [IO.File]::Open($taskOutput, 'CreateNew', 'Write', 'None')
    try {
        $taskZip = [IO.Compression.ZipArchive]::new($taskFile, [IO.Compression.ZipArchiveMode]::Create, $true)
        try {
            foreach ($taskEntry in $taskArchive.Entries) {
                $taskFrom = $taskEntry.Open(); $taskTo = $taskZip.CreateEntry($taskEntry.FullName).Open()
                try { $taskFrom.CopyTo($taskTo) } finally { $taskTo.Dispose(); $taskFrom.Dispose() }
            }
            $taskTo = $taskZip.CreateEntry('ExtendedHotbar.update.json').Open()
            try { $taskTo.Write($taskEnvelope,0,$taskEnvelope.Length) } finally { $taskTo.Dispose() }
        } finally { $taskZip.Dispose() }
        $taskFile.Flush($true)
    } finally { $taskFile.Dispose() }
    Get-FileHash -LiteralPath $taskOutput -Algorithm SHA256
} finally {
    $taskArchive.Dispose()
    if ($null -ne $taskPrivate) { [Array]::Clear($taskPrivate,0,$taskPrivate.Length) }
    if ($null -ne $taskRsa) { $taskRsa.Dispose() }
}
