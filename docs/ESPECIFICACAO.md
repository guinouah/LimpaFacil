# Especificação — LimpaFácil

## 1. Visão e escopo

O LimpaFácil atende uma única loja de produtos de limpeza. O cliente consulta o catálogo, reserva produtos e paga em reais no momento da retirada presencial. Pagamento on-line, entrega e nota fiscal estão fora da primeira versão.

O sistema possui três aplicações: app Android em .NET MAUI, API ASP.NET Core com MySQL e painel interno Blazor. A API é a autoridade sobre autenticação, autorização, preço, estoque e estados do pedido.

## 2. Perfis e requisitos

### Cliente

- cadastrar-se como pessoa física ou jurídica; o cadastro público sempre cria `Client`;
- após o primeiro cadastro, entrar diretamente nos pedidos enquanto a sessão local existir;
- pesquisar e filtrar o catálogo, consultar detalhes e orientações;
- manter carrinho por usuário em SQLite;
- finalizar somente on-line e acompanhar apenas seus pedidos;
- cancelar um pedido somente no estado `Received`;
- consultar o último catálogo armazenado sem internet, com data de atualização.

### Funcionário

- manter categorias e produtos por desativação, sem apagar histórico;
- consultar e filtrar pedidos;
- avançar estados e cancelar em `Received` ou `InSeparation`;
- confirmar implicitamente o pagamento presencial ao concluir a retirada;
- ajustar estoque com quantidade, responsável, motivo e saldos.

### Administrador

- possui as permissões de funcionário;
- cria administradores/funcionários e ativa ou desativa suas contas;
- não pode desativar a própria conta pela operação exposta.

## 3. Modelo de dados

| Entidade | Atributos principais | Restrições |
|---|---|---|
| User | Id, Name, Email, PasswordHash, Role, IsActive, CreatedAt | e-mail único; hash, nunca senha em texto |
| Category | Id, Name, Description, IsActive | nome único; desativação lógica |
| Product | Id, CategoryId, Sku, Name, Description, Brand, Presentation, UsageInstructions, Price, StockAvailable, ImageUrl, IsActive, Version | SKU único; preço > 0; estoque >= 0; cada apresentação é um SKU |
| Order | Id, Number, ClientId, Status, Total, datas, IdempotencyKey, Version, confirmação de pagamento | número único; `(ClientId, IdempotencyKey)` único |
| OrderItem | OrderId, ProductId, SkuSnapshot, ProductNameSnapshot, Quantity, UnitPrice, Subtotal | quantidade > 0; mantém dados históricos |
| OrderHistory | OrderId, PreviousStatus, NewStatus, AuthorId, CreatedAt, Note | append-only |
| StockMovement | ProductId, OrderId opcional, Quantity, PreviousBalance, NewBalance, Reason, AuthorId, CreatedAt | trilha de auditoria |
| RefreshSession | UserId, TokenHash, ExpiresAt, CreatedAt, RevokedAt, Device | somente hash do refresh token; rotação ao renovar |

Valores monetários usam `decimal(12,2)`. Datas são armazenadas em UTC. `Version` participa do controle otimista; operações críticas também bloqueiam linhas no MySQL.

## 4. Regras de negócio

1. A API recalcula subtotal e total usando o preço vigente.
2. Preço diferente do confirmado pelo cliente gera conflito `price_changed`; o app deve atualizar e solicitar nova confirmação.
3. Produto e categoria precisam estar ativos no checkout.
4. A criação do pedido, a validação e a reserva do estoque ocorrem na mesma transação serializável.
5. Produtos são lidos com `SELECT ... FOR UPDATE`, impedindo duas vendas da última unidade.
6. A chave de idempotência representa uma tentativa lógica. Repetir a mesma tentativa retorna o pedido existente.
7. Cancelamento só ocorre a partir de estado permitido. O bloqueio e o estado final impedem devolução dupla.
8. `Received → InSeparation → ReadyForPickup → Completed`. `Cancelled` e `Completed` são finais.
9. Funcionário pode cancelar nos dois primeiros estados; cliente somente no primeiro.
10. Ao chegar a `Completed`, a API registra pagamento confirmado, autor e horário.
11. Cliente filtra pedidos pelo próprio identificador. A API valida perfis em todos os endpoints administrativos.
12. Tokens ficam no `SecureStorage` do app; SQLite guarda catálogo público, carrinho e tentativa de checkout, sempre particionados por usuário. Logout remove os dados privados daquele usuário.

## 5. Fluxos principais

### Cadastro inicial e entrada automática

1. Na primeira abertura, o cliente informa pessoa física/jurídica, nome/razão social, CPF/CNPJ, e-mail e senha.
2. API valida, normaliza o e-mail e documento, fixa o perfil `Client` e grava hash PBKDF2 do `PasswordHasher`.
3. API emite JWT curto e refresh token aleatório; grava somente SHA-256 do refresh token.
4. Nas próximas aberturas, o app usa a sessão armazenada com segurança e abre as categorias; na renovação, a sessão anterior é revogada e outro par é emitido.
5. Se a API estiver temporariamente indisponível, a identidade local permite consultar o catálogo em cache. Escritas permanecem dependentes de conexão e autenticação no servidor.

### Compra

1. App persiste carrinho e preços confirmados.
2. Ao finalizar, calcula a impressão digital do carrinho e obtém/reutiliza a chave de idempotência.
3. API inicia transação, procura pedido anterior, bloqueia produtos, valida preço/atividade/saldo e reserva estoque.
4. API grava pedido, snapshots, histórico e movimentações; somente então confirma a transação.
5. O app limpa o carrinho após resposta de sucesso. Em falha de rede, mantém carrinho e chave para repetição segura.
6. O cliente pode copiar um pedido anterior; somente produtos ainda ativos entram na nova cópia, usando os preços atuais para revisão.

### Processamento e cancelamento

1. Painel envia novo estado e versão observada. Para concluir, exige confirmação do pagamento presencial.
2. API bloqueia pedido, confere versão/transição e registra o histórico.
3. Cancelamento bloqueia a operação, devolve cada item uma única vez e registra movimentações.

## 6. Endpoints

- `POST /api/auth/register|login|refresh|logout`
- `GET /api/catalog/categories|products`, `GET /api/catalog/products/{id}`
- `POST /api/orders`, `GET /api/orders`, `GET /api/orders/{id}`, `POST /api/orders/{id}/cancel`
- `GET|POST|PUT /api/admin/categories`
- `GET|POST|PUT /api/admin/products`, `POST /api/admin/products/{id}/stock-adjustments`
- `GET /api/admin/orders`, `PUT /api/admin/orders/{id}/status`
- `GET|POST /api/admin/employees`, `PUT /api/admin/employees/{id}/active`

OpenAPI/Swagger é publicado pela API em `/swagger`.

## 7. Requisitos não funcionais

- nenhuma credencial real versionada;
- erros de negócio claros e sem detalhes internos;
- trilha de auditoria para estados e estoque;
- transações e índices únicos como última barreira de consistência;
- dados de testes em banco isolado descartável, nunca em produção;
- interface em português e Android como alvo inicial.

## 8. Arquitetura MVVM

O aplicativo MAUI usa Views com `BindingContext` e bindings, ViewModels com propriedades observáveis e comandos `ICommand`, e serviços injetados por interfaces. A lógica de apresentação e persistência é testável no projeto `LimpaFacil.Mobile.Core`, sem depender do emulador. As Views não acessam API, SQLite ou sessão diretamente. A navegação MAUI e os diálogos ficam em serviços de infraestrutura. Consulte `ARQUITETURA_MVVM.md` para o mapa completo.
