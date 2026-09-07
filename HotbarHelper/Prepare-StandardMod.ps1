[CmdletBinding()]
param(
    [string] $SourcePackage = (Join-Path $PSScriptRoot '..\DungeonSettlers10Slots\dist\Extended-Hotbar-0.3.7-mit-Testspielstand.zip'),
    [string] $OutputPath = (Join-Path $PSScriptRoot '..\DungeonSettlers10Slots\dist\standard-0.3.7\Extended-Hotbar-0.3.7.zip')
)
# Documentation-only repack: preserve the tested DLL, artwork, demo and licenses.
$ErrorActionPreference = 'Stop'
$taskSource = [IO.Path]::GetFullPath($SourcePackage)
$taskTarget = [IO.Path]::GetFullPath($OutputPath)
if (Test-Path -LiteralPath $taskTarget) { throw 'Existing packages are never overwritten.' }
if ((Get-FileHash -LiteralPath $taskSource -Algorithm SHA256).Hash -ne '43D09DC064455F127DA53E1B01F76D510211F97E311CBA1DAE30D6F5804866AD') { throw 'Unexpected source package.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$taskGuides = @{
    'BITTE ZUERST LESEN.txt' = (Join-Path $PSScriptRoot '..\DungeonSettlers10Slots\UserGuide\BITTE ZUERST LESEN.txt')
    'READ FIRST - ENGLISH.txt' = (Join-Path $PSScriptRoot '..\DungeonSettlers10Slots\UserGuide\READ FIRST - ENGLISH.txt')
}
$taskExpected = @{}
New-Item -ItemType Directory -Path (Split-Path -Parent $taskTarget) -Force | Out-Null
$taskInput = [IO.Compression.ZipFile]::OpenRead($taskSource)
try {
    if ($taskInput.Entries.Count -ne 15) { throw 'Unexpected source layout.' }
    $taskFile = [IO.File]::Open($taskTarget, 'CreateNew', 'Write', 'None')
    $taskOutput = [IO.Compression.ZipArchive]::new($taskFile, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($taskEntry in $taskInput.Entries) {
            $taskRead = if ($taskGuides.ContainsKey($taskEntry.FullName)) { [IO.File]::OpenRead($taskGuides[$taskEntry.FullName]) } else { $taskEntry.Open() }
            try {
                $taskMemory = [IO.MemoryStream]::new()
                try { $taskRead.CopyTo($taskMemory); $taskBytes = $taskMemory.ToArray() } finally { $taskMemory.Dispose() }
            } finally { $taskRead.Dispose() }
            $taskHash = [Security.Cryptography.SHA256]::Create()
            try { $taskExpected[$taskEntry.FullName] = [BitConverter]::ToString($taskHash.ComputeHash($taskBytes)) } finally { $taskHash.Dispose() }
            $taskNew = $taskOutput.CreateEntry($taskEntry.FullName, [IO.Compression.CompressionLevel]::Optimal)
            $taskNew.LastWriteTime = $taskEntry.LastWriteTime
            $taskWrite = $taskNew.Open()
            try { $taskWrite.Write($taskBytes, 0, $taskBytes.Length) } finally { $taskWrite.Dispose() }
        }
    } finally { $taskOutput.Dispose(); $taskFile.Dispose() }
} finally { $taskInput.Dispose() }
$taskVerify = [IO.Compression.ZipFile]::OpenRead($taskTarget)
try {
    if ($taskVerify.Entries.Count -ne $taskExpected.Count) { throw 'Repack layout mismatch.' }
    foreach ($taskEntry in $taskVerify.Entries) {
        $taskRead = $taskEntry.Open(); $taskHash = [Security.Cryptography.SHA256]::Create()
        try { if ([BitConverter]::ToString($taskHash.ComputeHash($taskRead)) -ne $taskExpected[$taskEntry.FullName]) { throw 'Repack verification failed.' } }
        finally { $taskRead.Dispose(); $taskHash.Dispose() }
    }
} finally { $taskVerify.Dispose() }
Get-FileHash -LiteralPath $taskTarget -Algorithm SHA256
