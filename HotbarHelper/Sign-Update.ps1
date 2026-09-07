[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string] $PackagePath,
    [string] $KeyPath = (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'ExtendedHotbarHelper\PublisherKeys\release-signing.dpapi'),
    [string] $PublicKeyPath = (Join-Path $PSScriptRoot 'UpdateTrust.xml')
)
$ErrorActionPreference = 'Stop'
$taskPackage = [IO.Path]::GetFullPath($PackagePath)
$taskName = [IO.Path]::GetFileName($taskPackage)
if ($taskName -notmatch '^Extended-Hotbar-Helper-((?:0|[1-9][0-9]{0,4})\.(?:0|[1-9][0-9]{0,4})\.(?:0|[1-9][0-9]{0,4}))(-test)?\.zip$') { throw 'Unsupported update package filename.' }
$taskVersion = $Matches[1]; $taskTest = $Matches[2] -eq '-test'
$taskEnvelope = $taskPackage + '.update.json'
if (Test-Path -LiteralPath $taskEnvelope) { throw 'Signature metadata exists; publish a new version instead of overwriting it.' }
$taskNow = [DateTime]::UtcNow
$taskPayload = [ordered]@{
    purpose = 'ExtendedHotbarHelper.Update.v1'; repository = 'Fre-yin/DNST-Extended-Hotbar'; helperVersion = $taskVersion
    fileName = $taskName; size = (Get-Item -LiteralPath $taskPackage).Length
    sha256 = (Get-FileHash -LiteralPath $taskPackage -Algorithm SHA256).Hash
    prerelease = $taskTest; issuedUtc = $taskNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    expiresUtc = $taskNow.AddDays(180).ToString('yyyy-MM-ddTHH:mm:ssZ')
}
$taskBytes = [Text.Encoding]::UTF8.GetBytes(($taskPayload | ConvertTo-Json -Compress))
$taskPrivateBytes = [Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes([IO.Path]::GetFullPath($KeyPath)), $null, [Security.Cryptography.DataProtectionScope]::CurrentUser)
$taskRsa = [Security.Cryptography.RSACryptoServiceProvider]::new([Security.Cryptography.CspParameters]::new(24))
$taskRsa.PersistKeyInCsp = $false
try {
    $taskRsa.FromXmlString([Text.Encoding]::UTF8.GetString($taskPrivateBytes))
    if ($taskRsa.KeySize -ne 4096 -or $taskRsa.ToXmlString($false) -ne [IO.File]::ReadAllText([IO.Path]::GetFullPath($PublicKeyPath))) { throw 'Publisher key does not match the pinned RSA-4096 public key.' }
    $taskSignature = $taskRsa.SignData($taskBytes, [Security.Cryptography.CryptoConfig]::MapNameToOID('SHA256'))
    if (-not $taskRsa.VerifyData($taskBytes, [Security.Cryptography.CryptoConfig]::MapNameToOID('SHA256'), $taskSignature)) { throw 'Local signature verification failed.' }
    $taskJson = [ordered]@{ format = 1; payload = [Convert]::ToBase64String($taskBytes); signature = [Convert]::ToBase64String($taskSignature) } | ConvertTo-Json -Compress
    $taskOutput = [Text.Encoding]::UTF8.GetBytes($taskJson)
    $taskStream = [IO.File]::Open($taskEnvelope, 'CreateNew', 'Write', 'None')
    try { $taskStream.Write($taskOutput, 0, $taskOutput.Length); $taskStream.Flush($true) } finally { $taskStream.Dispose() }
    Get-FileHash -LiteralPath $taskEnvelope -Algorithm SHA256
} finally { [Array]::Clear($taskPrivateBytes, 0, $taskPrivateBytes.Length); $taskRsa.Dispose() }
