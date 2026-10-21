$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$source = Join-Path $root 'release\MapleMeter-Warm'
$output = Join-Path $root 'MapleMeter-Warm-Windows-x64.zip'
$stage = Join-Path ([IO.Path]::GetTempPath()) ('maplemeter-package-' + [guid]::NewGuid().ToString('N'))
$payload = Join-Path $stage 'MapleMeter-Warm'
New-Item -ItemType Directory -Path $payload -Force | Out-Null
# Explicit shipping inputs: never recursively copy a working release directory.
foreach ($name in @('MapleMeter-Warm.exe','banner.png','icon.png','icon.ico','README.txt','THIRD-PARTY.txt','experience-table-source.txt')) {
    Copy-Item -LiteralPath (Join-Path $source $name) -Destination $payload
}
foreach ($folder in @('runtime','runtime\tessdata','templates','fonts','THIRD-PARTY\tesseract')) {
    New-Item -ItemType Directory -Path (Join-Path $payload $folder) -Force | Out-Null
}
Get-ChildItem (Join-Path $source 'runtime') -Filter '*.dll' -File | Copy-Item -Destination (Join-Path $payload 'runtime')
Copy-Item -LiteralPath (Join-Path $source 'runtime\tessdata\eng.traineddata') -Destination (Join-Path $payload 'runtime\tessdata')
$templates = @('anchor-exp','anchor-lv','anchor-map','digit-comma','digit-dot','digit-percent','level-2','level-7','level-8','level-9') + (0..9 | ForEach-Object { "digit-$_" })
foreach ($name in $templates) { Copy-Item -LiteralPath (Join-Path $source "templates\$name.png") -Destination (Join-Path $payload 'templates') }
Get-ChildItem (Join-Path $source 'fonts') -Filter '*.ttf' -File | Copy-Item -Destination (Join-Path $payload 'fonts')
foreach ($name in @('NOTICE.txt','OFL-Inter.txt','OFL-SourceHanSans.txt','OFL-WenKai.txt')) { Copy-Item -LiteralPath (Join-Path $source "fonts\$name") -Destination (Join-Path $payload 'fonts') }
foreach ($name in @('AUTHORS','LICENSE','README.md')) { Copy-Item -LiteralPath (Join-Path $source "THIRD-PARTY\tesseract\$name") -Destination (Join-Path $payload 'THIRD-PARTY\tesseract') }
Compress-Archive -LiteralPath $payload -DestinationPath $output -Force
Get-FileHash -LiteralPath $output -Algorithm SHA256
# Keep the isolated staging folder for inspection; it contains no user settings.
Write-Output "Staged package: $payload"