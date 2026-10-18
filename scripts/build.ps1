param([string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'The .NET Framework x64 C# compiler is not available.' }
$icon = Join-Path $projectRoot 'assets\nova.ico'
if (-not (Test-Path -LiteralPath $icon)) { & (Join-Path $PSScriptRoot 'make-icon.ps1') }
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) { $OutputDirectory = Join-Path $projectRoot 'dist' }
elseif (-not [IO.Path]::IsPathRooted($OutputDirectory)) { $OutputDirectory = Join-Path $projectRoot $OutputDirectory }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$sourceFiles = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src') -Filter '*.cs' -File | Sort-Object Name | Select-Object -ExpandProperty FullName)
if ($sourceFiles.Count -eq 0) { throw 'No C# sources were found.' }
$destination = Join-Path $OutputDirectory 'EchoPointNovaTrainer.exe'
$manifest = Join-Path $projectRoot 'assets\app.manifest'
& $compiler /nologo /codepage:65001 /optimize+ /target:winexe /platform:x64 /r:System.Windows.Forms.dll /r:System.Drawing.dll "/win32icon:$icon" "/win32manifest:$manifest" "/out:$destination" $sourceFiles
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
[pscustomobject]@{ Path=$destination; Bytes=(Get-Item -LiteralPath $destination).Length; SHA256=(Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash } | ConvertTo-Json
