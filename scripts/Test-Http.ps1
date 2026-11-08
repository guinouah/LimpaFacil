param([int]$MySqlPort = 33308)
# Executar somente via Test-LocalMySql.ps1 -IncludeHttp: usa o MySQL descartável local.
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$database = 'lf_http_' + [Guid]::NewGuid().ToString('N').Substring(0, 16)
$apiUrl = 'http://127.0.0.1:55100'
$webUrl = 'http://127.0.0.1:55120'
$mysql = Join-Path $root 'tools/mysql-server/bin/mysql.exe'
$apiDirectory = Join-Path $root 'src/LimpaFacil.Api'
$webDirectory = Join-Path $root 'src/LimpaFacil.Web'
$apiDll = Join-Path $apiDirectory 'bin/Debug/net10.0/LimpaFacil.Api.dll'
$webDll = Join-Path $webDirectory 'bin/Debug/net10.0/LimpaFacil.Web.dll'
$names = @('ASPNETCORE_ENVIRONMENT','ConnectionStrings__MySql','Jwt__Key','Database__ApplyMigrationsOnStartup','Database__SeedDemoData','ApiBaseUrl','LIMPAFACIL_ADMIN_NAME','LIMPAFACIL_ADMIN_EMAIL','LIMPAFACIL_ADMIN_PASSWORD')
$previous = @{}; foreach ($name in $names) { $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
$api = $null; $web = $null
function Assert-True([bool]$Condition, [string]$Description) {
    if (-not $Condition) { throw "Falhou: $Description" }
    Write-Host "PASS: $Description"
}
function Json-Request([string]$Path, [object]$Body, [hashtable]$Headers = @{}, [string]$Method = 'POST') {
    Invoke-RestMethod -Uri "$apiUrl/$Path" -Method $Method -Headers $Headers -ContentType 'application/json' -Body ($Body | ConvertTo-Json -Depth 10) -TimeoutSec 15
}
function Status([string]$Path, [hashtable]$Headers = @{}) {
    (Invoke-WebRequest -Uri "$apiUrl/$Path" -Headers $Headers -SkipHttpErrorCheck -TimeoutSec 10).StatusCode
}
function Wait-Http([string]$Url) {
    for ($attempt=0; $attempt -lt 40; $attempt++) {
        try { if ((Invoke-WebRequest -Uri $Url -TimeoutSec 1).StatusCode -eq 200) { return } } catch { }
        Start-Sleep -Milliseconds 250
    }
    throw "Não iniciou: $Url"
}
try {
    & $mysql '--protocol=TCP' '--host=127.0.0.1' "--port=$MySqlPort" '--user=root' '--ssl-mode=DISABLED' -e "CREATE DATABASE $database CHARACTER SET utf8mb4"
    if ($LASTEXITCODE -ne 0) { throw 'Não foi possível criar o banco HTTP isolado.' }
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ConnectionStrings__MySql = "Server=127.0.0.1;Port=$MySqlPort;Database=$database;User=root;SslMode=None;AllowPublicKeyRetrieval=True"
    $env:Jwt__Key = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
    $env:Database__ApplyMigrationsOnStartup = 'true'; $env:Database__SeedDemoData = 'true'
    $env:LIMPAFACIL_ADMIN_NAME = 'Admin Teste'; $env:LIMPAFACIL_ADMIN_EMAIL = 'admin@http.test'
    $env:LIMPAFACIL_ADMIN_PASSWORD = [Guid]::NewGuid().ToString('N') + '!Test'
    $adminPassword = $env:LIMPAFACIL_ADMIN_PASSWORD
    $bootstrap = Start-Process dotnet -ArgumentList @(('"' + $apiDll + '"'), '--create-admin') -WorkingDirectory $apiDirectory -WindowStyle Hidden -PassThru -Wait -RedirectStandardOutput (Join-Path $root 'outputs/http-bootstrap.log') -RedirectStandardError (Join-Path $root 'outputs/http-bootstrap-error.log')
    if ($bootstrap.ExitCode -ne 0) { throw 'Falha ao criar administrador de teste; confira outputs/http-bootstrap-error.log.' }
    $api = Start-Process dotnet -ArgumentList @(('"' + $apiDll + '"'), '--urls', $apiUrl) -WorkingDirectory $apiDirectory -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $root 'outputs/http-api.log') -RedirectStandardError (Join-Path $root 'outputs/http-api-error.log')
    $env:ApiBaseUrl = "$apiUrl/"
    $web = Start-Process dotnet -ArgumentList @(('"' + $webDll + '"'), '--urls', $webUrl) -WorkingDirectory $webDirectory -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $root 'outputs/http-web.log') -RedirectStandardError (Join-Path $root 'outputs/http-web-error.log')
    Wait-Http "$apiUrl/health"; Wait-Http $webUrl
    Assert-True ((Invoke-WebRequest $webUrl).Content.Contains('Acessar painel')) 'Painel responde com a tela de login'
    Assert-True ((Invoke-WebRequest $webUrl).Content.Contains('href="/cadastro"')) 'Login web oferece link para cadastro'
    $registrationHtml = (Invoke-WebRequest "$webUrl/cadastro").Content
    Assert-True ($registrationHtml.Contains('customer-name') -and $registrationHtml.Contains('customer-email') -and $registrationHtml.Contains('customer-confirm-password')) 'Página de cadastro web contém o formulário completo'
    Assert-True ((Status 'api/admin/products') -eq 401) 'Admin exige autenticação'
    $admin = Json-Request 'api/auth/login' @{email='admin@http.test';password=$adminPassword}
    $adminHeader = @{Authorization="Bearer $($admin.accessToken)"}
    $client = Json-Request 'api/auth/register' @{name='Cliente A';email='clienta@http.test';password='Password-Teste-123!';customerType=0;taxId='12345678909'}
    $other = Json-Request 'api/auth/register' @{name='Cliente B';email='clientb@http.test';password='Password-Teste-123!';customerType=0;taxId='98765432100'}
    $clientHeader = @{Authorization="Bearer $($client.accessToken)"}; $otherHeader = @{Authorization="Bearer $($other.accessToken)"}
    Assert-True (($client.user.role -eq 0) -and (Status 'api/admin/products' $clientHeader) -eq 403) 'Cadastro público cria cliente sem privilégios'
    $category = Json-Request 'api/admin/categories' @{name='Categoria HTTP';description='Teste isolado';isActive=$true} $adminHeader
    $product = Json-Request 'api/admin/products' @{categoryId=$category.id;sku='HTTP-1';name='Produto HTTP';brand='Teste';presentation='1L';description='Descrição';usageInstructions='Diluir';price=10;isActive=$true} $adminHeader
    $product = Json-Request "api/admin/products/$($product.id)/stock-adjustments" @{quantity=3;reason='Carga inicial teste';expectedProductVersion=$product.version} $adminHeader
    Assert-True ($product.stockAvailable -eq 3) 'Cadastro de categoria, produto e ajuste de estoque'
    $checkout = @{idempotencyKey='http-purchase';items=@(@{productId=$product.id;quantity=2;confirmedUnitPrice=10});observation='Pedido HTTP'}
    $order = Json-Request 'api/orders' $checkout $clientHeader
    $retry = Json-Request 'api/orders' $checkout $clientHeader
    Assert-True (($order.total -eq 20) -and ($order.id -eq $retry.id)) 'Checkout calcula total e impede duplicação'
    Assert-True ((Status "api/orders/$($order.id)" $otherHeader) -eq 404) 'Cliente não acessa pedido de outro usuário'
    $cancelled = Json-Request "api/orders/$($order.id)/cancel" @{expectedVersion=$order.version} $clientHeader
    Assert-True ($cancelled.status -eq 4) 'Cliente cancela pedido recebido'
    $products = Invoke-RestMethod "$apiUrl/api/admin/products" -Headers $adminHeader
    Assert-True (($products | Where-Object id -eq $product.id).stockAvailable -eq 3) 'Cancelamento devolve o estoque'
    $checkout.idempotencyKey = 'http-pickup'
    $order = Json-Request 'api/orders' $checkout $clientHeader
    foreach ($nextStatus in @(1,2,3)) {
        $order = Json-Request "api/admin/orders/$($order.id)/status" @{newStatus=$nextStatus;expectedVersion=$order.version;confirmPayment=($nextStatus -eq 3)} $adminHeader 'PUT'
    }
    Assert-True (($order.status -eq 3) -and $order.paymentConfirmed -and $order.history.Count -eq 4) 'Retirada concluída com pagamento e histórico'
    $employee = Json-Request 'api/admin/employees' @{name='Funcionário teste';email='employee@http.test';password='Password-Teste-123!';role=1} $adminHeader
    $employeeAuth = Json-Request 'api/auth/login' @{email='employee@http.test';password='Password-Teste-123!'}
    $employeeHeader = @{Authorization="Bearer $($employeeAuth.accessToken)"}
    Assert-True ((Status 'api/admin/employees' $employeeHeader) -eq 403) 'Funcionário não administra a equipe'
    $null = Json-Request "api/admin/employees/$($employee.id)/active" @{isActive=$false} $adminHeader 'PUT'
    Assert-True ((Status 'api/admin/products' $employeeHeader) -eq 401) 'Desativação invalida acesso com token existente'
    Write-Host 'TESTE HTTP COMPLETO: APROVADO'
}
finally {
    foreach ($process in @($api,$web)) { if ($process -and -not $process.HasExited) { Stop-Process -Id $process.Id } }
    foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process') }
    if ($database -match '^lf_http_[0-9a-f]{16}$') {
        & $mysql '--protocol=TCP' '--host=127.0.0.1' "--port=$MySqlPort" '--user=root' '--ssl-mode=DISABLED' -e "DROP DATABASE IF EXISTS $database"
    }
}
