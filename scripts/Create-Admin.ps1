$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$env:LIMPAFACIL_ADMIN_NAME = Read-Host 'Nome do primeiro administrador'
$env:LIMPAFACIL_ADMIN_EMAIL = Read-Host 'E-mail do primeiro administrador'
$secure = Read-Host 'Senha inicial (mínimo 10 caracteres)' -AsSecureString
$env:LIMPAFACIL_ADMIN_PASSWORD = [System.Net.NetworkCredential]::new('', $secure).Password
try {
    dotnet run --project (Join-Path $root 'src/LimpaFacil.Api') --launch-profile http -- --create-admin
    if ($LASTEXITCODE -ne 0) { throw 'A criação não foi concluída. Confira o banco e se já existe um administrador.' }
}
finally {
    Remove-Item Env:LIMPAFACIL_ADMIN_PASSWORD,Env:LIMPAFACIL_ADMIN_EMAIL,Env:LIMPAFACIL_ADMIN_NAME -ErrorAction SilentlyContinue
}
