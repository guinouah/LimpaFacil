# Arquitetura MVVM do aplicativo

O aplicativo segue Model–View–ViewModel com injeção de dependências. As Views são construídas em C# MAUI e usam `BindingContext`, `SetBinding` e comandos; MVVM não depende de declarar o layout em XAML.

## Organização

| Camada | Local | Responsabilidade |
|---|---|---|
| Model | `LimpaFacil.Domain`, `LimpaFacil.Contracts` | Entidades, regras e contratos da API |
| ViewModel | `LimpaFacil.Mobile.Core/ViewModels` | Estado observável, validação, busca, carrinho e pedidos |
| View | `LimpaFacil.Mobile/Pages` | Layout e bindings; o ciclo de vida dispara o comando de carga |
| Persistência | `LimpaFacil.Mobile.Core/LocalStore.cs` | Cache e carrinho SQLite separados por usuário |
| Infraestrutura | `LimpaFacil.Mobile/Services` | HTTP, SecureStorage, conectividade, diálogos e navegação MAUI |
| Composição | `LimpaFacil.Mobile/MauiProgram.cs` | Registro dos serviços, interfaces, Views e ViewModels |

`ObservableObject` implementa `INotifyPropertyChanged`. `AsyncCommand` implementa `ICommand`, permite aguardar a execução nos testes e impede cliques duplicados. `BaseViewModel` mantém estado de carregamento e transforma falhas em mensagens observáveis. Não há dependência de MAUI nos ViewModels: eles ficam no projeto Core e podem ser testados sem emulador.

## Telas e comandos

- `LoginViewModel`: valida entrada, autentica, limpa a senha e navega.
- `RegistrationViewModel`: cadastro PF/PJ e placeholders observáveis.
- `StartupViewModel`: inicialização e recuperação da sessão.
- `CategoriesViewModel` / `CatalogViewModel`: sincronização, cache, busca e filtro por categoria.
- `ProductViewModel`: quantidade, limite de estoque e adição ao carrinho.
- `CartViewModel`: persistência, total, observação, bloqueio offline e finalização idempotente.
- `MyOrdersViewModel`: atualização, confirmação de cancelamento e repetição de pedidos.

A API e o painel Blazor possuem arquiteturas próprias; o requisito MVVM se aplica ao cliente MAUI. As regras finais de preço, autorização, estoque e transição de pedido permanecem na API.
