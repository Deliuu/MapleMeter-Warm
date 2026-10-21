$ErrorActionPreference = 'Stop'
$warmRoot = $PSScriptRoot
$outputDir = Join-Path $warmRoot 'release\MapleMeter-Warm'

$frameworkDir = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
foreach ($folder in @('runtime','templates','fonts','THIRD-PARTY')) { if (!(Test-Path (Join-Path $outputDir $folder))) { throw 'Extract the full release ZIP into release/MapleMeter-Warm before building (see README.md).' } }
New-Item -ItemType Directory -Force $outputDir | Out-Null
$sources = Get-ChildItem (Join-Path $warmRoot 'src') -Filter '*.cs' | ForEach-Object FullName
& "$frameworkDir\csc.exe" /nologo /utf8output /target:winexe /platform:x64 /optimize+ /reference:System.dll /reference:System.Core.dll /reference:System.Xml.dll /reference:System.Xaml.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll "/reference:$frameworkDir\WPF\WindowsBase.dll" "/reference:$frameworkDir\WPF\PresentationCore.dll" "/reference:$frameworkDir\WPF\PresentationFramework.dll" "/win32icon:$warmRoot\assets\icon.ico" "/out:$outputDir\MapleMeter-Warm.exe" $sources
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed' }

Copy-Item (Join-Path $warmRoot 'assets\banner.png'),(Join-Path $warmRoot 'assets\icon.png'),(Join-Path $warmRoot 'assets\icon.ico') $outputDir -Force
if(Test-Path (Join-Path $warmRoot 'assets\fonts')){Copy-Item (Join-Path $warmRoot 'assets\fonts\*') (Join-Path $outputDir 'fonts') -Force}
Copy-Item (Join-Path $warmRoot 'THIRD-PARTY.txt') $outputDir -Force
if(Test-Path (Join-Path $warmRoot 'README.txt')){Copy-Item (Join-Path $warmRoot 'README.txt') $outputDir -Force}
Write-Output "Built: $outputDir\MapleMeter-Warm.exe"

Copy-Item (Join-Path $warmRoot "assets\experience-table-source.txt") $outputDir -Force

Copy-Item (Join-Path $warmRoot "assets\level-templates\*.png") (Join-Path $outputDir "templates") -Force
