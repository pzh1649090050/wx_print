param([switch]$FrameworkDependent)
$ErrorActionPreference='Stop'
Push-Location $PSScriptRoot
try {
    dotnet run --project tests/WeShot.Tests.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw '测试未通过' }
    $selfContained = if ($FrameworkDependent) { 'false' } else { 'true' }
    dotnet publish app/WeShot.csproj -c Release -r win-x64 --self-contained $selfContained -o dist -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=None
    if ($LASTEXITCODE -ne 0) { throw '发布失败' }
    Copy-Item README.md dist/使用说明.md -Force
    Write-Host '已生成 dist/WeShot.exe'
} finally { Pop-Location }
