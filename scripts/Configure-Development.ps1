param([string]$Server = '127.0.0.1', [int]$Port = 3306, [string]$Database = 'limpafacil', [string]$User = 'limpafacil_app')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$password = Read-Host 'Senha do usuário MySQL' -AsSecureString
$plain = [System.Net.NetworkCredential]::new('', $password).Password
$secret = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
if ($plain.Contains(';') -or $plain.Contains('"')) { throw 'Para este assistente, use uma senha sem ponto-e-vírgula e sem aspas; alternativamente configure a connection string diretamente pelo user-secrets.' }
$configuration = @{
    'ConnectionStrings:MySql' = "Server=$Server;Port=$Port;Database=$Database;User=$User;Password=$plain"
    'Jwt:Key' = $secret
    'Database:ApplyMigrationsOnStartup' = 'true'
    'Database:SeedDemoData' = 'true'
}
# Entrada padrão evita incluir a senha na linha de comando ou em arquivos do repositório.
$configuration | ConvertTo-Json | dotnet user-secrets set --project (Join-Path $root 'src/LimpaFacil.Api')
if ($LASTEXITCODE -ne 0) { throw 'Não foi possível salvar a configuração local.' }
$plain = $null; $configuration = $null; $secret = $null
Write-Host 'Configuração salva no User Secrets. Inicie o perfil API + Windows Machine no Visual Studio.'
