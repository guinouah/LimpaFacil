# Publicação no GitHub

O repositório Git local está inicializado. Nenhum repositório remoto foi criado e nenhum código foi enviado para sua conta.

## GitHub Desktop

1. Abra GitHub Desktop e escolha **File → Add local repository**.
2. Selecione a pasta que contém `LimpaFacil.sln`.
3. Confira os arquivos, faça o primeiro commit e use **Publish repository**.
4. Escolha o nome e a visibilidade do repositório.

## Linha de comando

Na raiz do projeto:

```powershell
git status
git add .
git commit -m "Implementa LimpaFacil com MAUI MVVM, API e painel"
git branch -M main
git remote add origin https://github.com/SEU_USUARIO/LimpaFacil.git
git push -u origin main
```

Substitua o endereço pelo repositório vazio criado na sua conta. Os comandos de publicação não foram executados automaticamente.

## Arquivos incluídos

Código, solução do Visual Studio, migrations, testes, documentação, scripts, exemplos de configuração e automação GitHub Actions.

## Arquivos locais excluídos

`appsettings.Development.json`, `.env`, User Secrets, bancos, diretórios MySQL portáteis, certificados e keystores, instaladores, `bin`, `obj`, `.vs`, logs e resultados gerados em `outputs`.

Depois do clone, siga o README para configurar um MySQL e criar o primeiro administrador. Credenciais e dados da máquina original não são distribuídos.
