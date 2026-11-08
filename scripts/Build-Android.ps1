# Auxílio para ambientes Windows restritos que bloqueiam o temp padrão do Java.
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$tempPath = Join-Path $root 'outputs/java-temp'
New-Item -ItemType Directory -Force -Path $tempPath | Out-Null
$previousOptions = $env:JAVA_TOOL_OPTIONS
try {
    $env:JAVA_TOOL_OPTIONS = ($previousOptions + ' "-Djava.io.tmpdir=' + $tempPath + '"').Trim()
    dotnet build (Join-Path $root 'src/LimpaFacil.Mobile/LimpaFacil.Mobile.csproj') -f net10.0-android -m:1
    if ($LASTEXITCODE -ne 0) { throw 'Falha na compilação Android.' }
}
finally { $env:JAVA_TOOL_OPTIONS = $previousOptions }
