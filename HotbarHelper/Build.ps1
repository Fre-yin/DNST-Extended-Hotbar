[CmdletBinding()]
param(
    [string] $OutputDirectory = (Join-Path $PSScriptRoot 'bin'),
    [string] $PackagePath = (Join-Path $PSScriptRoot '..\DungeonSettlers10Slots\dist\Extended-Hotbar-0.3.8.zip'),
    [string] $BepInExPackagePath = (Join-Path $PSScriptRoot '..\DungeonSettlersHotbar.BepInEx\dist\Extended-Hotbar-BepInEx-0.3.8-bepinex.1.zip'),
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
if (-not (Test-Path -LiteralPath $taskPackage)) { throw 'Provide the pinned 0.3.8 MelonLoader release including its optional test save with -PackagePath.' }
if ((Get-FileHash -LiteralPath $taskPackage -Algorithm SHA256).Hash -ne 'D7824BD6B31E3760EBC803111261B75EC5ADF60FAE87B7AAC444DF9B2993F544') { throw 'Unexpected mod package; no helper build.' }
$taskBepPackage = [IO.Path]::GetFullPath($BepInExPackagePath)
if ((Get-FileHash -LiteralPath $taskBepPackage -Algorithm SHA256).Hash -ne '77193451CD045D03A50E24A80F2D0582E178F335D90926EDF9AAF9E6B3880547') { throw 'Unexpected BepInEx mod package; no helper build.' }
$taskResources = @(('/resource:' + $taskPackage + ',HotbarPackage.zip'), ('/resource:' + $taskBepPackage + ',BepInExPackage.zip'))
$taskRefs = @('mscorlib','System','System.Core','System.Drawing','System.Windows.Forms','System.Web.Extensions','System.IO.Compression','System.IO.Compression.FileSystem','System.Xml') | ForEach-Object { '/reference:' + (Join-Path $taskFramework ($_ + '.dll')) }
$taskCommon = @('/nologo','/noconfig','/nostdlib+','/langversion:latest','/warnaserror+','/optimize+','/platform:x64') + $taskRefs
$taskCore = @('FrameworkTarget.cs','LosslessJson.cs','Profiles.cs','HotbarKeyProfile.cs','ReleaseInfo.cs','ModLoaders.cs','Operations.cs','LoadStatus.cs','UiText.cs','Updates.cs','UpdateSignature.cs','LocalMods.cs','LocalHelpers.cs','GameDiscovery.cs') | ForEach-Object { Join-Path $PSScriptRoot $_ }
$taskLanguages = @('en','ko','fr','de','ru','zh-Hans','zh-Hant','ja','es','pt-BR') | ForEach-Object { '/resource:' + (Join-Path $PSScriptRoot ('Languages\' + $_ + '.json')) + ',Language.' + $_ + '.json' }
$taskTrustPath = Join-Path $PSScriptRoot 'UpdateTrust.xml'
if (-not (Test-Path -LiteralPath $taskTrustPath)) { throw 'Pinned public update key missing. Initialize publisher signing first.' }
$taskPublicKey = [IO.File]::ReadAllText($taskTrustPath)
if ($taskPublicKey -notmatch '^<RSAKeyValue><Modulus>[A-Za-z0-9+/=]+</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>$') { throw 'Only the public RSA key may be embedded, never private signing material.' }
$taskLanguages += '/resource:' + $taskTrustPath + ',UpdateTrust.xml'
$taskExe = Join-Path $taskOut 'Extended-Hotbar-Helper.exe'
& dotnet $taskCompiler @taskCommon @taskLanguages '/target:winexe' ('/out:' + $taskExe) ('/win32manifest:' + (Join-Path $PSScriptRoot 'app.manifest')) @taskResources @taskCore (Join-Path $PSScriptRoot 'MainForm.cs') (Join-Path $PSScriptRoot 'RemovalDialog.cs')
if ($LASTEXITCODE -ne 0) { throw 'Helper build failed.' }
if (-not $SkipTests) {
    $taskTests = Join-Path $taskOut 'Helper.Tests.exe'
    & dotnet $taskCompiler @taskCommon @taskLanguages '/target:exe' ('/out:' + $taskTests) @taskResources @taskCore (Join-Path $PSScriptRoot 'Tests.cs') (Join-Path $PSScriptRoot 'DualLoaderTests.cs') (Join-Path $PSScriptRoot 'UpdateTests.cs') (Join-Path $PSScriptRoot 'LocalModTests.cs') (Join-Path $PSScriptRoot 'LocalUpdateSmokeTests.cs') (Join-Path $PSScriptRoot 'GameDiscoveryTests.cs') (Join-Path $PSScriptRoot 'LocalOnlyChecks.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Test build failed.' }
    & $taskTests ([IO.Path]::GetFullPath($TestSavePath)) (Join-Path $taskOut 'test-runs') $ReadOnlyGameCheck
    if ($LASTEXITCODE -ne 0) { throw 'Helper tests failed.' }
}
Get-FileHash -LiteralPath $taskExe -Algorithm SHA256
