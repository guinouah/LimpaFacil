# LimpaFácil

Projeto acadêmico para uma loja de produtos de limpeza: aplicativo .NET MAUI para clientes, API ASP.NET Core com MySQL e painel administrativo Blazor. Os pedidos são retirados na loja e pagos presencialmente.

## Aplicações

| Projeto | Responsabilidade |
|---|---|
| `LimpaFacil.Mobile` | Interface MAUI para Android e Windows |
| `LimpaFacil.Mobile.Core` | ViewModels MVVM, comandos e persistência SQLite |
| `LimpaFacil.Api` | Autenticação, autorização, catálogo, estoque e pedidos |
| `LimpaFacil.Web` | Painel administrativo Blazor |
| `LimpaFacil.Domain` | Entidades, perfis e regras de negócio |
| `LimpaFacil.Contracts` | Contratos compartilhados de requisição e resposta |
| `tests` | Testes unitários, SQLite, MVVM e integração MySQL |

## Funcionalidades

- Cadastro PF/PJ e login com renovação de sessão.
- Catálogo com categorias, busca, preços em reais e consulta offline com data do cache.
- Carrinho SQLite separado por usuário, quantidades e observação opcional.
- Checkout online, reserva de estoque, idempotência e proteção contra compras concorrentes.
- Acompanhamento, histórico, cancelamento e repetição de pedidos.
- Administração de categorias, produtos, preços, descrições e orientações de uso.
- Ajustes de estoque e gestão de funcionários exclusivos do administrador.
- Retirada com confirmação explícita do pagamento, histórico e controle de concorrência.

O aplicativo utiliza MVVM: as Views contêm layout e bindings; os ViewModels expõem propriedades observáveis e `ICommand`. HTTP, navegação, conectividade e sessão são injetados por interfaces. Consulte [a arquitetura MVVM](docs/ARQUITETURA_MVVM.md).

## Pré-requisitos

- Windows com Visual Studio 2026 e cargas de trabalho ASP.NET/.NET MAUI, ou SDK .NET 10 compatível com `global.json`.
- Android SDK 36 e JDK 21 para o app Android (instalados pela carga de trabalho MAUI).
- MySQL 8.4 ou Docker Desktop.
- PowerShell 7 para os scripts auxiliares.

## 1. Preparar o banco

Com Docker, copie `.env.example` para `.env`, substitua as senhas e execute:

```powershell
Copy-Item .env.example .env
docker compose up -d mysql
```

Alternativamente, crie no MySQL local o banco `limpafacil` e um usuário com permissões sobre esse banco. O Workbench é uma interface de administração; é necessário que o servidor MySQL esteja em execução.

Os diretórios `tools/mysql-server` e `tools/mysql-data`, quando presentes na máquina de desenvolvimento, são locais e não fazem parte do repositório. A API pode iniciar esse servidor portátil automaticamente na porta 3307 se sua conexão estiver configurada para ela.

## 2. Configurar a API

Na raiz do projeto:

```powershell
./scripts/Configure-Development.ps1
```

O script solicita a senha MySQL e grava a conexão e uma chave JWT aleatória no **User Secrets**, fora do repositório. Para outra porta, use `-Port 3307`. Ele também habilita migrations e dados demonstrativos. A senha deve coincidir com a configuração do banco ou do Docker.

Para configuração manual, há um modelo em `src/LimpaFacil.Api/appsettings.Development.example.json`. Copie para `appsettings.Development.json` e substitua os valores; esse arquivo local é ignorado pelo Git. Não use as chaves de exemplo como credenciais reais.

## 3. Criar o primeiro administrador

```powershell
./scripts/Create-Admin.ps1
```

O script solicita nome, e-mail e senha. Só funciona enquanto não existir administrador. Não há senha administrativa padrão. As contas criadas publicamente pelo aplicativo são sempre clientes.

## 4. Abrir e executar

Abra **`LimpaFacil.sln`** no Visual Studio e selecione **API + Windows Machine**. Pressione **F5** para iniciar a API, o painel e o app Windows. A janela do app tem proporção de celular e roda sem empacotamento MSIX.

| Aplicação | Endereço de desenvolvimento |
|---|---|
| Painel administrativo | `http://localhost:5200` |
| API | `http://localhost:5100` |
| Swagger | `http://localhost:5100/swagger` |
| Saúde da API | `http://localhost:5100/health` |

Para iniciar separadamente:

```powershell
dotnet run --project src/LimpaFacil.Api --launch-profile http
dotnet run --project src/LimpaFacil.Web --launch-profile http
```

Execute esses comandos em terminais separados. No painel, entre com o administrador criado e acesse Catálogo, Pedidos ou Funcionários. O endereço `/api/admin/products` é uma rota protegida de dados; não é a tela do painel.

### Android

Mantenha a API em execução. No Visual Studio, selecione `LimpaFacil.Mobile`, escolha o emulador Android e execute. O app usa `http://10.0.2.2:5100` no emulador. Em aparelho físico, configure o IP local da API em `MauiProgram.cs`.

```powershell
dotnet build src/LimpaFacil.Mobile -f net10.0-android -m:1
```

Se o Java falhar ao mover o arquivo temporário durante a assinatura em um ambiente restrito, use `./scripts/Build-Android.ps1`, que limita a pasta temporária da compilação a `outputs/java-temp`.

HTTP é usado no desenvolvimento local; ambientes publicados devem usar HTTPS com certificado válido.

## Testes

```powershell
dotnet test tests/LimpaFacil.UnitTests -m:1
```

Para integração, use um servidor MySQL de testes com permissão para criar bancos isolados:

```powershell
$env:LIMPAFACIL_TEST_MYSQL = 'Server=127.0.0.1;Port=3306;User=USUARIO_TESTE;Password=SENHA_TESTE'
dotnet test tests/LimpaFacil.IntegrationTests -m:1
Remove-Item Env:LIMPAFACIL_TEST_MYSQL
```

Os testes criam e removem exclusivamente bancos `lf_test_<identificador>`. Sem a variável, são ignorados. Não aponte para produção.

Na máquina que possui o MySQL portátil em `tools/mysql-server`, há uma verificação adicional reproduzível:

```powershell
./scripts/Test-LocalMySql.ps1 -IncludeHttp
```

Esse comando inicia um servidor de testes em `127.0.0.1:33308`, executa a integração e valida cadastro/login/perfis/checkout/cancelamento/retirada por HTTP com banco separado. Os dados da loja não são utilizados.

## Resultado verificado

A validação de 26/10/2026 aprovou **18 testes unitários/SQLite/MVVM**, **7 testes de integração com MySQL real** e **11 verificações do fluxo HTTP**. API, painel e app MAUI Windows/Android compilaram com 0 erros. Comandos, ambiente e limites estão no [relatório de testes](docs/RELATORIO_TESTES.md).

## Documentação e GitHub

- [Especificação, requisitos e regras](docs/ESPECIFICACAO.md)
- [Tecnologias e ferramentas](docs/TECNOLOGIAS.md)
- [Arquitetura MVVM](docs/ARQUITETURA_MVVM.md)
- [Relatório dos testes executados](docs/RELATORIO_TESTES.md)
- [Como publicar no GitHub](docs/GITHUB.md)

O `.gitignore` exclui credenciais locais, bancos SQLite/MySQL, certificados, instaladores, logs, caches, `bin` e `obj`. A automação em `.github/workflows/ci.yml` prepara builds da API, painel, Windows/Android e testes com MySQL 8.4; sua execução no GitHub acontece após a publicação do repositório.
