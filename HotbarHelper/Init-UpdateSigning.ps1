[CmdletBinding()]
param([string] $PublicKeyPath = (Join-Path $PSScriptRoot 'UpdateTrust.xml'))
$ErrorActionPreference = 'Stop'
$taskKeyFolder = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'ExtendedHotbarHelper\PublisherKeys'
$taskKeyPath = Join-Path $taskKeyFolder 'release-signing.dpapi'
if ((Test-Path -LiteralPath $taskKeyFolder) -or (Test-Path -LiteralPath $PublicKeyPath)) { throw 'Signing key folder or public key already exists. Never overwrite a release key.' }
New-Item -ItemType Directory -Path $taskKeyFolder | Out-Null
$taskAcl = New-Object Security.AccessControl.DirectorySecurity
$taskAcl.SetAccessRuleProtection($true, $false)
foreach ($taskSid in @([Security.Principal.WindowsIdentity]::GetCurrent().User, [Security.Principal.SecurityIdentifier]::new('S-1-5-18'))) {
    $taskAcl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new($taskSid, 'FullControl', 'ContainerInherit,ObjectInherit', 'None', 'Allow'))
}
Set-Acl -LiteralPath $taskKeyFolder -AclObject $taskAcl
$taskRsa = [Security.Cryptography.RSACryptoServiceProvider]::new(4096, [Security.Cryptography.CspParameters]::new(24))
$taskRsa.PersistKeyInCsp = $false
$taskPrivateBytes = $null
try {
    $taskPrivateBytes = [Text.Encoding]::UTF8.GetBytes($taskRsa.ToXmlString($true))
    $taskEncrypted = [Security.Cryptography.ProtectedData]::Protect($taskPrivateBytes, $null, [Security.Cryptography.DataProtectionScope]::CurrentUser)
    $taskStream = [IO.File]::Open($taskKeyPath, 'CreateNew', 'Write', 'None')
    try { $taskStream.Write($taskEncrypted, 0, $taskEncrypted.Length); $taskStream.Flush($true) } finally { $taskStream.Dispose() }
    $taskPublicBytes = [Text.Encoding]::UTF8.GetBytes($taskRsa.ToXmlString($false))
    $taskPublicStream = [IO.File]::Open([IO.Path]::GetFullPath($PublicKeyPath), 'CreateNew', 'Write', 'None')
    try { $taskPublicStream.Write($taskPublicBytes, 0, $taskPublicBytes.Length); $taskPublicStream.Flush($true) } finally { $taskPublicStream.Dispose() }
    Write-Output ('Public verification key: ' + [IO.Path]::GetFullPath($PublicKeyPath))
    Write-Output ('Encrypted publisher key outside repository: ' + $taskKeyPath)
} finally { if ($null -ne $taskPrivateBytes) { [Array]::Clear($taskPrivateBytes, 0, $taskPrivateBytes.Length) }; $taskRsa.Dispose() }
