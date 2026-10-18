param([switch]$Live)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'The .NET Framework x64 C# compiler is not available.' }
$testOutput = Join-Path $projectRoot '.local\test-bin'
New-Item -ItemType Directory -Path $testOutput -Force | Out-Null
Push-Location -LiteralPath $projectRoot
try {
    $core = Join-Path $testOutput 'CoreTests.exe'
    & $compiler /nologo /codepage:65001 /target:exe /platform:x64 "/out:$core" src\Memory.cs tests\CoreTests.cs
    if ($LASTEXITCODE -ne 0) { throw 'Core tests compilation failed' }
    & $core
    if ($LASTEXITCODE -ne 0) { throw 'Core tests failed' }
    if ($Live) {
        $engine = Join-Path $testOutput 'UnrealTests.exe'
        & $compiler /nologo /codepage:65001 /target:exe /platform:x64 "/out:$engine" src\Memory.cs src\Unreal.cs tests\UnrealTests.cs
        if ($LASTEXITCODE -ne 0) { throw 'Engine tests compilation failed' }
        & $engine
        if ($LASTEXITCODE -ne 0) { throw 'Engine tests failed' }
        $live = Join-Path $testOutput 'LiveTests.exe'
        & $compiler /nologo /codepage:65001 /target:exe /platform:x64 "/out:$live" src\Memory.cs src\Unreal.cs src\Trainer.cs tests\LiveTests.cs
        if ($LASTEXITCODE -ne 0) { throw 'Live tests compilation failed' }
        & $live
        if ($LASTEXITCODE -ne 0) { throw 'Live tests failed' }
    }
} finally { Pop-Location }
