/* =============================================================================
   Gestão de Endereços — Teste Dev C# (AeC, Time de Sistemas)
   Script de criação da estrutura das tabelas (DDL) — SQL Server.

   Atende ao requisito: "Será necessário enviar os scripts de criação da
   estrutura das tabelas apenas."

   Fonte da verdade do modelo: EF Core Code-First (este .sql espelha o modelo).
   Em produção, gerar/atualizar via:
     dotnet ef migrations script --idempotent -o db/scripts/01-create-tables.sql

   Decisões de modelagem (ver docs/02-decisoes-arquiteturais-adr.md):
   - Cep   : CHAR(8) somente dígitos (normalizado, sem máscara) — dado consistente.
   - Numero: NVARCHAR (texto) — aceita "S/N", "123-A", "0". Nunca inteiro.
   - SenhaHash: É a coluna "senha" da spec da AeC, evoluída para guardar APENAS o hash
                versionado do PasswordHasher (PBKDF2-HMAC-SHA256), nunca o texto puro (NFR-01).
   - UNIQUE(Usuario): login duplicado é impossível por garantia do banco. O índice é
                case-insensitive (collation padrão do SQL Server) — login não diferencia
                maiúsculas/minúsculas (intencional); o Service normaliza para minúsculas.
   - Índice em IdUsuario: toda query do app filtra por ele (evita table scan).
   - Fonte de verdade: este .sql é o ARTEFATO ENTREGUE (atende "apenas scripts de criação").
                A migração EF Code-First é detalhe interno de desenvolvimento; antes de
                entregar, confira a paridade (diff) entre este script e o gerado pelo EF.
   ============================================================================= */

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* -----------------------------------------------------------------------------
   Tabela: Usuarios
   ----------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.Usuarios', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Usuarios
    (
        Id        INT            IDENTITY(1,1) NOT NULL,
        Nome      NVARCHAR(120)  NOT NULL,
        Usuario   NVARCHAR(60)   NOT NULL,
        SenhaHash NVARCHAR(256)  NOT NULL,   -- coluna "senha" da spec, guardando o hash
        IsAdmin   BIT            NOT NULL CONSTRAINT DF_Usuarios_IsAdmin DEFAULT(0),  -- extra: papel admin

        CONSTRAINT PK_Usuarios PRIMARY KEY CLUSTERED (Id)
    );

    -- Login único: última linha de defesa contra contas duplicadas.
    CREATE UNIQUE INDEX UX_Usuarios_Usuario
        ON dbo.Usuarios (Usuario);
END;
GO

/* -----------------------------------------------------------------------------
   Tabela: Enderecos
   ----------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.Enderecos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Enderecos
    (
        Id          INT            IDENTITY(1,1) NOT NULL,
        Cep         CHAR(8)        NOT NULL,            -- somente dígitos
        Logradouro  NVARCHAR(150)  NOT NULL,
        Complemento NVARCHAR(60)   NULL,                -- opcional
        Bairro      NVARCHAR(80)   NOT NULL,
        Cidade      NVARCHAR(80)   NOT NULL,
        Uf          CHAR(2)        NOT NULL,
        Numero      NVARCHAR(15)   NOT NULL,            -- texto: "S/N", "10A"
        IdUsuario   INT            NOT NULL,
        CONSTRAINT PK_Enderecos PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Enderecos_Usuarios
            FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuarios (Id)
            ON DELETE CASCADE,   -- espelha DeleteBehavior.Cascade do EF; ver RN-09 (§2.3)
        -- Reforço barato e real no nível de dados: CEP só pode conter dígitos.
        -- O CHECK pressupõe os 8 dígitos completos (garantidos pela validação da aplicação);
        -- como Cep é CHAR(8), um valor mais curto seria preenchido com espaços e violaria o CHECK.
        -- (A validação de UF fica na aplicação — ViewModel/Service — para não duplicar
        --  uma lista de 27 valores em dois lugares de manutenção.)
        CONSTRAINT CK_Enderecos_Cep_Digitos
            CHECK (Cep NOT LIKE '%[^0-9]%')
    );

    -- Toda consulta de endereços é filtrada por usuário (multitenancy lógico).
    CREATE NONCLUSTERED INDEX IX_Enderecos_IdUsuario
        ON dbo.Enderecos (IdUsuario);
END;
GO
