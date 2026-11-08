# Tecnologias e ferramentas

- .NET 10 LTS e C#; SDK local `10.0.401`.
- ASP.NET Core Web API, JWT Bearer, `PasswordHasher<T>` e Swagger/OpenAPI.
- Entity Framework Core 9.0.0 + Pomelo.EntityFrameworkCore.MySql 9.0.0 + MySQL 8.4.
- Blazor Web App com renderização interativa no servidor.
- .NET MAUI 10.0.20 para Android/Windows, MVVM com `INotifyPropertyChanged`/`ICommand`, `sqlite-net-pcl` 1.11.285 e `SecureStorage`.
- xUnit 2.6.2 para testes; testes MySQL criam banco com nome aleatório e o descartam.
- Visual Studio Community 2026 18.10 ou CLI `dotnet`.

O projeto mantém o conjunto EF Core/Pomelo 9 sobre .NET 10. As migrations e os testes com MySQL real verificam o comportamento desse conjunto. Os ViewModels ficam no Core, sem dependência de MAUI, permitindo testes sem emulador.
