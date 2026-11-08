# Relatório de validação — 26/10/2026

## Resultado

| Verificação executada | Resultado |
|---|---|
| Compilação API | Aprovada, 0 erros |
| Compilação painel Blazor | Aprovada, 0 erros |
| Compilação MAUI Windows | Aprovada, 0 erros |
| Compilação e assinatura APK Android | Aprovadas, 0 erros |
| Testes unitários, SQLite e MVVM | 18 aprovados, 0 falhas, 0 ignorados |
| Testes com MySQL real | 7 aprovados, 0 falhas, 0 ignorados |
| Fluxo HTTP com API/painel/banco isolado | 11 verificações aprovadas |

Os arquivos TRX da execução ficam em `outputs/tests`, fora do versionamento. Os números acima são de execuções efetivas; não representam apenas testes implementados.

## Ambiente

Windows, SDK .NET 10.0.401, MAUI 10.0.20, Android SDK 36, JDK 21.0.8, EF Core/Pomelo 9. A execução local utilizou MySQL Community 26.7.0 portátil em porta exclusiva de testes. O ambiente reproduzível do GitHub Actions está configurado para MySQL 8.4; esse workflow ainda depende de publicação e execução no GitHub.

## Casos verificados

- Totais decimais, quantidades inválidas e transições de estados.
- Persistência e isolamento do carrinho SQLite, cache e chave de idempotência.
- Login MVVM: validação, mensagens de falha, notificação de bindings, navegação e limpeza da senha.
- Comandos assíncronos bloqueando cliques duplicados.
- Cadastro PJ atualizando os campos observáveis.
- Consulta de catálogo offline e bloqueio do checkout sem conexão.
- Checkout enviando observação e limpando somente o carrinho do usuário atual.
- Cadastro/login real com hash da senha e isolamento entre clientes.
- Estoque insuficiente, mudança de preço e categorias inativas.
- Disputa concorrente pela última unidade e idempotência de compra.
- Cancelamento devolvendo estoque uma única vez.
- Preservação dos snapshots de nome e preço.
- Retirada concluída somente com confirmação do pagamento e registro de histórico.

## Verificação HTTP

`scripts/Test-LocalMySql.ps1 -IncludeHttp` criou banco `lf_http_<identificador>`, administrador e clientes descartáveis, iniciou API e painel e verificou:

1. Painel responde com a tela de login.
2. Endpoints administrativos exigem autenticação (401).
3. Cadastro público cria cliente e não concede privilégios administrativos (403).
4. Administrador cria categoria/produto e ajusta estoque.
5. Checkout calcula total e não duplica pedido ao repetir a mesma tentativa.
6. Um cliente não lê o pedido de outro (404).
7. Cliente cancela pedido no estado Recebido.
8. Cancelamento devolve o estoque reservado.
9. Funcionário/administrador conduz o pedido até a retirada, com pagamento e histórico.
10. Funcionário não administra a equipe (403).
11. Conta desativada perde o acesso mesmo com um token emitido anteriormente (401).

Os processos e o banco HTTP de teste são encerrados/removidos pelo script. Os dados reais da loja não são usados.

## Problemas encontrados e corrigidos

- Estado de login do painel recriado por componente: cliente HTTP passou a ter escopo por circuito Blazor.
- MVVM ausente: lógica retirada das Views e coberta por testes no Core.
- Transação explícita incompatível com retry automático do provedor: removido o retry implícito e implementada repetição da transação inteira em conflitos de bloqueio.
- Nome do banco de teste excedendo limite do lock de migrations: prefixo reduzido e limpeza em caso de falha.
- Disputa concorrente de estoque gerando deadlock: transação refeita com a mesma chave de idempotência.
- Assinador Android não conseguia mover o APK da pasta temporária do sandbox: validação executada com `JAVA_TOOL_OPTIONS=-Djava.io.tmpdir=<pasta local gravável>`.

## Reproduzir

```powershell
dotnet test tests/LimpaFacil.UnitTests -m:1
./scripts/Test-LocalMySql.ps1 -IncludeHttp
dotnet build src/LimpaFacil.Api -m:1
dotnet build src/LimpaFacil.Web -m:1
dotnet build src/LimpaFacil.Mobile -f net10.0-windows10.0.19041.0 -m:1
dotnet build src/LimpaFacil.Mobile -f net10.0-android -m:1
```

A validação HTTP não simula cliques nem substitui testes de usabilidade no emulador. Publicação na Play Store e hospedagem de produção não fazem parte deste escopo acadêmico.

A tentativa de abrir o app Windows pelo controle do computador foi interrompida por timeout da autorização da ferramenta. Portanto, a inspeção visual da janela e a execução interativa no emulador não foram marcadas como aprovadas neste relatório.
