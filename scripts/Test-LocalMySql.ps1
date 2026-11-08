param([int]$Port = 33308, [switch]$IncludeHttp)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root
$server = $null
$previousConnection = $env:LIMPAFACIL_TEST_MYSQL
$testExit = 1
try {
    $mysqlBase = Join-Path $root 'tools/mysql-server'
    $executable = Join-Path $mysqlBase 'bin/mysqld.exe'
    if (-not (Test-Path -LiteralPath $executable)) { throw 'MySQL portátil não encontrado. Use um MySQL local isolado com LIMPAFACIL_TEST_MYSQL, conforme o README.' }
    $testData = Join-Path $root 'outputs/mysql-validation'
    New-Item -ItemType Directory -Force -Path $testData | Out-Null
    if (-not (Test-Path (Join-Path $testData 'auto.cnf'))) {
        & $executable --no-defaults --initialize-insecure "--basedir=$mysqlBase" "--datadir=$testData" --console
        if ($LASTEXITCODE -ne 0) { throw 'Falha na inicialização do MySQL de teste.' }
    }
    $arguments = @('--no-defaults', ('"--basedir=' + $mysqlBase + '"'), ('"--datadir=' + $testData + '"'), "--port=$Port", '--bind-address=127.0.0.1', '--mysqlx=OFF', '--console')
    $server = Start-Process -FilePath $executable -ArgumentList $arguments -WindowStyle Hidden -PassThru -RedirectStandardOutput 'outputs/mysql-validation.stdout.log' -RedirectStandardError 'outputs/mysql-validation.stderr.log'
    $ready = $false
    for ($attempt = 0; $attempt -lt 40; $attempt++) {
        if ($server.HasExited) { throw 'O MySQL de teste encerrou; confira outputs/mysql-validation.stderr.log.' }
        $tcp = [System.Net.Sockets.TcpClient]::new()
        try { if ($tcp.ConnectAsync('127.0.0.1', $Port).Wait(250)) { $ready = $true; break } }
        catch { }
        finally { $tcp.Dispose() }
        Start-Sleep -Milliseconds 250
    }
    if (-not $ready) { throw 'O MySQL de teste não ficou disponível.' }
    $env:LIMPAFACIL_TEST_MYSQL = "Server=127.0.0.1;Port=$Port;User=root;SslMode=None;AllowPublicKeyRetrieval=True"
    dotnet test tests/LimpaFacil.IntegrationTests/LimpaFacil.IntegrationTests.csproj -m:1 -p:UseSharedCompilation=false --logger 'trx;LogFileName=mysql-tests.trx' --results-directory outputs/tests
    $testExit = $LASTEXITCODE
    if ($testExit -eq 0 -and $IncludeHttp) {
        dotnet build src/LimpaFacil.Web/LimpaFacil.Web.csproj -m:1 -p:UseSharedCompilation=false
        if ($LASTEXITCODE -ne 0) { throw 'Falha na compilação do painel.' }
        & (Join-Path $PSScriptRoot 'Test-Http.ps1') -MySqlPort $Port
    }
}
finally {
    if ($server -and -not $server.HasExited) { Stop-Process -Id $server.Id }
    $env:LIMPAFACIL_TEST_MYSQL = $previousConnection
    Pop-Location
}
exit $testExit
