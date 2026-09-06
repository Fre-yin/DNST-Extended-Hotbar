[CmdletBinding()]
param(
    [string] $OutputDirectory = (Join-Path $PSScriptRoot 'bin'),
    [string] $PackagePath = (Join-Path $PSScriptRoot '..\DungeonSettlers10Slots\dist\Extended-Hotbar-0.3.7.zip'),
    [string] $TestSavePath = (Join-Path $PSScriptRoot 'SyntheticSave.json'),
    [string] $ReadOnlyGameCheck,
    [switch] $SkipTests
)
$ErrorActionPreference = 'Stop'
$taskFramework = Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8'
if (-not (Test-Path -LiteralPath $taskFramework)) { throw '.NET Framework 4.8 Developer Pack required to build (not to run).' }
$taskSdkLine = & dotnet --list-sdks | Select-Object -Last 1
if ($taskSdkLine -notmatch '^([^ ]+) \[(.+)\]$') { throw 'Cannot locate the installed SDK.' }
$taskCompiler = Join-Path $Matches[2] ($Matches[1] + '\Roslyn\bincore\csc.dll')
if (-not (Test-Path -LiteralPath $taskCompiler)) { throw 'Roslyn compiler not found.' }
$taskOut = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $taskOut -Force | Out-Null
$taskPackage = [IO.Path]::GetFullPath($PackagePath)
if (-not (Test-Path -LiteralPath $taskPackage)) { throw 'Download the unchanged Extended-Hotbar-0.3.7.zip release and pass its location with -PackagePath.' }
if ((Get-FileHash -LiteralPath $taskPackage -Algorithm SHA256).Hash -ne 'BCAF47FFD620288F530F6BB386F35CCE02AC458775ECD40A260BFA82B92FD916') { throw 'Unexpected mod package; no helper build.' }
$taskRefs = @('mscorlib','System','System.Core','System.Drawing','System.Windows.Forms','System.Web.Extensions','System.IO.Compression','System.IO.Compression.FileSystem') | ForEach-Object { '/reference:' + (Join-Path $taskFramework ($_ + '.dll')) }
$taskCommon = @('/nologo','/noconfig','/nostdlib+','/langversion:latest','/warnaserror+','/optimize+','/platform:x64') + $taskRefs
$taskCore = @('LosslessJson.cs','Profiles.cs','Operations.cs','LoadStatus.cs','UiText.cs') | ForEach-Object { Join-Path $PSScriptRoot $_ }
$taskLanguages = @('en','ko','fr','de','ru','zh-Hans','zh-Hant','ja','es','pt-BR') | ForEach-Object { '/resource:' + (Join-Path $PSScriptRoot ('Languages\' + $_ + '.json')) + ',Language.' + $_ + '.json' }
$taskExe = Join-Path $taskOut 'Extended-Hotbar-Helper.exe'
& dotnet $taskCompiler @taskCommon @taskLanguages '/target:winexe' ('/out:' + $taskExe) ('/win32manifest:' + (Join-Path $PSScriptRoot 'app.manifest')) ('/resource:' + $taskPackage + ',HotbarPackage.zip') @taskCore (Join-Path $PSScriptRoot 'MainForm.cs') (Join-Path $PSScriptRoot 'RemovalDialog.cs')
if ($LASTEXITCODE -ne 0) { throw 'Helper build failed.' }
if (-not $SkipTests) {
    $taskTests = Join-Path $taskOut 'Helper.Tests.exe'
    & dotnet $taskCompiler @taskCommon @taskLanguages '/target:exe' ('/out:' + $taskTests) ('/resource:' + $taskPackage + ',HotbarPackage.zip') @taskCore (Join-Path $PSScriptRoot 'Tests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Test build failed.' }
    & $taskTests ([IO.Path]::GetFullPath($TestSavePath)) (Join-Path $taskOut 'test-runs') $ReadOnlyGameCheck
    if ($LASTEXITCODE -ne 0) { throw 'Helper tests failed.' }
}
Get-FileHash -LiteralPath $taskExe -Algorithm SHA256
