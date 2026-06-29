# Ambiente produtivo (Oracle Cloud)

A demonstração ao vivo roda em **duas instâncias Oracle Cloud na mesma VCN/sub-rede privada
(`10.0.0.0/24`)**, separando aplicação e banco — como num ambiente real.

```
Internet ──:8080──> [ App  ARM ]  ──:1433 (rede privada)──>  [ SQL Server  x86 ]
                     129.151.35.75                              10.0.0.108 (só interno)
                     ASP.NET Core (systemd)                     Azure SQL Edge (Docker)
```

## Por que Azure SQL Edge (e não SQL Server "cheio")
As instâncias x86 *Always Free* têm **~1 GB de RAM**, e o SQL Server 2022 **exige 2 GB** (o instalador
recusa abaixo disso). O **Azure SQL Edge** é o **mesmo motor do SQL Server** (mesmo protocolo TDS,
mesmo T-SQL, mesmo provider `Microsoft.EntityFrameworkCore.SqlServer`), porém roda em ~1 GB — então a
aplicação usa, online, um **banco SQL Server de verdade**. Para um SQL Server completo, basta uma
instância x86 com ≥ 2 GB (ou o `docker-compose` deste repo, que sobe o SQL Server 2022).

## Segurança de rede
A porta **1433 é exposta apenas para a sub-rede `10.0.0.0/24`** (regra da *security list*), **nunca
para a internet**. O app alcança o banco pelo IP privado `10.0.0.108`. Só a porta `8080` do app é
pública.

## Banco (instância x86)
```bash
docker run -d --name sqledge --restart unless-stopped \
  -e "ACCEPT_EULA=1" -e "MSSQL_SA_PASSWORD=<senha-forte>" \
  -p 1433:1433 mcr.microsoft.com/azure-sql-edge:latest
```

## Aplicação (instância ARM) — serviço systemd
Publicação self-contained `linux-arm64`, rodando como serviço. Variáveis relevantes:
```ini
Environment=ASPNETCORE_URLS=http://0.0.0.0:8080
Environment=Database__Provider=SqlServer
Environment="ConnectionStrings__Default=Server=10.0.0.108,1433;Database=GestaoEnderecos;User Id=sa;Password=<senha-forte>;TrustServerCertificate=True"
```
No primeiro boot, o `EnsureCreated` cria o banco `GestaoEnderecos`, o schema e popula os dados de
demonstração (2 usuários + 50 endereços reais).

## Trocar de banco sem recompilar
Basta a configuração `Database:Provider` (`SqlServer` | `Sqlite`) e a connection string — útil para o
avaliador rodar localmente com `docker-compose` (SQL Server 2022) ou com SQLite, sem tocar no código.
