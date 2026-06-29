# Gestão de Endereços

Aplicação web em **C# / ASP.NET Core MVC** para o teste técnico de desenvolvedor.
Permite **login**, **gerenciar um CRUD de endereços** (cadastro manual ou **autopreenchimento por
CEP via API do [ViaCEP](https://viacep.com.br/)**) e **exportar os endereços para CSV**.

> Implementa exatamente o escopo proposto pela avaliação — robusto, sem *overengineering*.
> O objetivo é um código que um sênior reconheça pela qualidade e um júnior leia sem esforço.

## Stack
- **.NET 8 LTS** · ASP.NET Core **MVC** · Razor Views
- **Entity Framework Core 8** (Code-First) · **SQL Server**
- **Bootstrap 5** · JavaScript *vanilla* (sem framework de front)
- Testes: **xUnit** · **Moq** · **SQLite in-memory** · `WebApplicationFactory`

> *Nota:* a sugestão "ASP.NET MVC" do enunciado foi interpretada como **ASP.NET Core MVC (.NET 8 LTS)**,
> o padrão atual e suportado.

## Como rodar
Pré-requisitos: **.NET 8 SDK** e uma instância de **SQL Server** (LocalDB, Express ou container).

```bash
# 1. Configurar a connection string (NUNCA commitar segredos — use User-Secrets)
cd src/GestaoEnderecos
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Default" "Server=(localdb)\\MSSQLLocalDB;Database=GestaoEnderecos;Trusted_Connection=True;TrustServerCertificate=True"

# 2. Criar o banco — opção A: aplicar o script DDL
#    Execute db/scripts/01-create-tables.sql no seu SQL Server.
#    (opção B, em desenvolvimento: dotnet ef database update)

# 3. Rodar
dotnet run
```
A aplicação sobe, popula usuários de demonstração (*seed*) e abre na tela de login.

## Testes
```bash
dotnet test
```

## Banco de dados
O script de criação das tabelas (entregável do teste) está em
[`db/scripts/01-create-tables.sql`](db/scripts/01-create-tables.sql).

## Decisões de arquitetura (resumo)
- **Monólito bem organizado** (um projeto MVC, pastas por responsabilidade) em vez de Clean
  Architecture multi-projeto: o EF Core já é o repositório; abstrair por cima seria cerimônia.
- **`PasswordHasher` nativo** (PBKDF2, shared framework) — segurança sem reinventar criptografia.
- **Isolamento por usuário** via *EF Global Query Filter*: um usuário nunca enxerga endereço de
  outro, por construção (não por disciplina).
- **ViaCEP por endpoint interno** (typed `HttpClient`, async, *timeout*) com degradação graciosa.
- **CSV com CsvHelper** (UTF-8 com BOM) — mesma régua de "não reinventar o resolvido".

## O que ficou de fora (de propósito)
Cadastro self-service de usuário (usuários nascem por *seed*; o enunciado pede apenas login),
recuperação de senha, busca/paginação, Polly/circuit-breaker, Docker. Foco no escopo pedido.

> Isolamento de dados e proteção de rota são decisões de engenharia derivadas dos critérios de
> **segurança** da avaliação, não exigências textuais do enunciado.
