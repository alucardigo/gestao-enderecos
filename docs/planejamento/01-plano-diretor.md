# 01 — Plano Diretor de Engenharia

> **Projeto:** Gestão de Endereços — Teste Técnico C# (AeC, Time de Sistemas)
> **Ambiente de desenvolvimento:** Claude Code App
> **Documento:** plano mestre de arquitetura, design, desenvolvimento, testes e QA.
> **Fonte da verdade dos requisitos:** [`00-requisitos-originais.md`](00-requisitos-originais.md).

---

## 0. Resumo executivo (TL;DR)

Construir um **monólito ASP.NET Core MVC (.NET 8 LTS)** — um único projeto, organizado por
pastas — que entrega **login → CRUD de endereços → autopreenchimento por CEP (ViaCEP) →
exportação CSV**, com **isolamento de dados entre usuários garantido por construção**
(EF Global Query Filter) e **senhas protegidas com o `PasswordHasher` nativo do framework**.

A régua de toda decisão é uma só: **reaproveitar o que já está resolvido pelo framework e
gastar o crédito de complexidade apenas no domínio do problema** (fluxo de login, integração
ViaCEP, isolamento por usuário). Nada de Clean Architecture multi-projeto, repositórios sobre
o EF, MediatR, AutoMapper ou criptografia escrita à mão. _Senioridade aqui é saber o que **não**
construir._

O diferencial competitivo não está na arquitetura — está em **3 coisas**: (1) o **momento mágico**
do autopreenchimento por CEP polido ao milissegundo; (2) **testes cirúrgicos** que transformam
afirmações ("é seguro") em **evidência executável** (o teste de IDOR que prova que o usuário A
não vê dado do B); (3) um **histórico de commits** e um **README** que contam a história e
justificam o julgamento.

---

## 1. Filosofia de engenharia (a síntese das quatro vozes)

Este plano foi pressionado por um painel de quatro mentalidades. Cada uma deixou uma regra:

| Voz | Princípio que fica | Aplicação concreta neste projeto |
|-----|--------------------|----------------------------------|
| **Fabio Akita** | Senioridade = risco eliminado por design + provado por teste, não linhas escritas. TDD onde dá retorno. | TDD nas unidades puras (ViaCEP, CSV, hashing); teste de integração fino no fluxo de login; quality gates antes de cada commit. |
| **DHH** | *Conceptual compression*: o framework já comprimiu 90% disto — não descomprima. Monólito majestoso. | Um projeto, pastas por responsabilidade, `DbContext` usado direto. JS vanilla mínimo (estilo Hotwire). |
| **Linus Torvalds** | *Good taste*: reestruture os dados para que o caso ruim seja **impossível de escrever**. | `HasQueryFilter` por `IdUsuario` elimina o IDOR na origem. `Numero` é texto, CEP é normalizado — dado honesto. |
| **Steve Jobs** | Existe **um** produto com **um** momento mágico. Foco é ter a coragem de entregar menos, impecável. | 60% do polimento no fluxo do CEP. Cortar tudo que não foi pedido (sem busca, paginação, dark mode). |

**Regra de ouro unificada:** o avaliador não mede o quanto você escreveu; mede o seu
**julgamento sobre o que não escrever**. Robustez sem inchaço.

---

## 2. Análise de requisitos e regras de negócio

### 2.1 Escopo funcional (o que ENTRA)
1. **Autenticação** por login/senha, com sessão por cookie e redirecionamento para a lista.
2. **CRUD de endereços** por usuário: criar, listar, editar, excluir (com confirmação).
3. **Autopreenchimento por CEP** via ViaCEP, com degradação graciosa.
4. **Exportação CSV** dos endereços do usuário (UTF-8 com BOM, RFC 4180).
5. **Scripts DDL** das tabelas + **README** + **um commit por funcionalidade**.

### 2.2 Fora de escopo (o que NÃO entra — disciplina de foco)
Cadastro self-service de usuário¹, "lembrar-me" elaborado, recuperação de senha, busca,
paginação, ordenação avançada, dark mode, perfis/roles, i18n, API REST pública, Docker,
multi-idioma. _Cada feature extra é tempo roubado do polimento do que importa e superfície
nova para quebrar na frente do avaliador._

> ¹ Usuários entram por **seed** (não há cadastro self-service — não consta do enunciado).
> O seed cria **dois** usuários de demonstração (ex.: `ana` e `bruno`) com endereços distintos,
> ambos documentados no README e a credencial visível na tela de login. Dois usuários permitem
> ao avaliador **ver o isolamento de dados funcionando** em 30s, sem rodar testes (Jobs:
> "avaliador que não consegue entrar não avalia nada").

### 2.3 Regras de negócio
| RN | Regra |
|----|-------|
| RN-01 | Um endereço **pertence a exatamente um usuário** e só é visível/editável por ele. |
| RN-02 | Campos obrigatórios: cep, logradouro, bairro, cidade, uf, numero. **Complemento é opcional.** |
| RN-03 | CEP é armazenado **normalizado** (8 dígitos, sem máscara). A máscara é só apresentação. |
| RN-04 | `Numero` é **texto** (aceita "S/N", "123-A", "0"). Nunca inteiro. |
| RN-05 | `Uf` é uma das 27 unidades federativas (2 letras maiúsculas). |
| RN-06 | Login (`Usuario`) é **único** no sistema (garantido pelo banco, não só pela app). |
| RN-07 | Senha nunca trafega/armazena em texto puro — só o hash versionado. |
| RN-08 | Se o ViaCEP falhar ou não encontrar o CEP, o cadastro **manual continua possível**. |
| RN-09 | Excluir um usuário remove seus endereços em cascata (`ON DELETE CASCADE` = `DeleteBehavior.Cascade`). Não há exclusão de usuário no escopo; a regra mantém o dado íntegro. |

---

## 3. Arquitetura — o Monólito Majestoso (ADR-001)

**Decisão:** um único projeto ASP.NET Core MVC, organizado por **pastas de responsabilidade**.
Sem Clean Architecture multi-projeto, sem `IEnderecoRepository`/`IUsuarioRepository` sobre o EF.

```
src/GestaoEnderecos/
├── Program.cs                  # composição: DI, auth, pipeline (Minimal Hosting)
├── appsettings.json            # config (connection string via User-Secrets/env, nunca no repo)
├── Controllers/
│   ├── AccountController.cs     # login / logout
│   ├── EnderecosController.cs   # CRUD + exportação (thin: valida → chama service → View)
│   └── ErrorController.cs       # páginas de erro amigáveis
├── Services/
│   ├── IViaCepService.cs        # ÚNICA interface que merece existir (dependência externa, mockável)
│   ├── ViaCepService.cs         # typed HttpClient, async, timeout, degradação graciosa
│   ├── EnderecoService.cs       # regras de negócio do CRUD (usa o DbContext direto)
│   ├── AutenticacaoService.cs   # valida credenciais + PasswordHasher
│   └── CsvExporter.cs           # writer RFC 4180 + BOM UTF-8 (testado à exaustão)
├── Data/
│   ├── AppDbContext.cs          # DbSets + OnModelCreating (Fluent API) + Global Query Filter
│   ├── UsuarioContext.cs        # provê o IdUsuario logado p/ o query filter (scoped)
│   └── DbSeeder.cs              # usuário de demonstração (hash gerado no seed)
├── Models/                      # entidades de domínio (POCOs): Usuario, Endereco
├── ViewModels/                  # modelos de tela + DataAnnotations (validação de apresentação)
├── Views/                       # Razor (Account/Login, Enderecos/*, Shared/_Layout, Error)
└── wwwroot/                     # css, js vanilla (cep.js), Bootstrap 5 via libman/cdn
```

**Por que esta é a resposta sênior (e não as 4 camadas do relatório Gemini):**
- O `DbContext` do EF Core **já é** o Repository + Unit of Work. Envolvê-lo em interfaces com
  uma única implementação é reescrever o EF com menos poder e mais boilerplate.
- Há **duas** entidades e **uma** integração externa. Quatro `.csproj` para isso lê-se como
  *"copiou um boilerplate"* ou *insegurança disfarçada de arquitetura*.
- **A única costura que merece uma interface** é `IViaCepService` — porque é uma dependência
  externa, instável, que precisamos **mockar nos testes** e poder trocar. Todo o resto é YAGNI.
- _"Monólito majestoso" não é "monólito desleixado"_: os **Services de negócio existem** (controllers
  não carregam regra). O que não existe é a fantasia das camadas.

> **Dose de Services (trade-off consciente).** `ViaCepService` e `CsvExporter` são inegociáveis
> (lógica pura e testável). `AutenticacaoService` e `EnderecoService` são **finos** e carregam
> regra real (verificação de hash; normalização de CEP + dono na escrita). Se um deles encolher a
> ponto de ser só *passthrough* para o `DbContext`, **dissolva-o no controller** — extrair Service
> sem regra é cerimônia. A régua: Service existe para abrigar regra, não para preencher uma camada.

> Detalhamento e trade-offs completos em [`02-decisoes-arquiteturais-adr.md`](02-decisoes-arquiteturais-adr.md).

---

## 4. Modelagem de dados e persistência (ADR-005)

**ORM:** EF Core 8 *Code-First*; configuração via **Fluent API** em `OnModelCreating`
(mantém as entidades limpas — `DataAnnotations` ficam só nas ViewModels). **Banco:** SQL Server.

### 4.1 Esquema (dado honesto — princípio Linus)
**Tabela `Usuarios`**

| Coluna     | Tipo            | Restrições |
|------------|-----------------|------------|
| `Id`       | `INT IDENTITY`  | PK |
| `Nome`     | `NVARCHAR(120)` | NOT NULL |
| `Usuario`  | `NVARCHAR(60)`  | NOT NULL, **UNIQUE** |
| `SenhaHash`| `NVARCHAR(256)` | NOT NULL (armazena o hash versionado do `PasswordHasher`) |

**Tabela `Enderecos`**

| Coluna        | Tipo            | Restrições |
|---------------|-----------------|------------|
| `Id`          | `INT IDENTITY`  | PK |
| `Cep`         | `CHAR(8)`       | NOT NULL (somente dígitos, normalizado) |
| `Logradouro`  | `NVARCHAR(150)` | NOT NULL |
| `Complemento` | `NVARCHAR(60)`  | **NULL** (opcional) |
| `Bairro`      | `NVARCHAR(80)`  | NOT NULL |
| `Cidade`      | `NVARCHAR(80)`  | NOT NULL |
| `Uf`          | `CHAR(2)`       | NOT NULL (CHECK opcional p/ 27 UFs) |
| `Numero`      | `NVARCHAR(15)`  | NOT NULL (**texto**: "S/N", "10A") |
| `IdUsuario`   | `INT`           | NOT NULL, **FK → Usuarios(Id)**, **ÍNDICE não-clusterizado** |

**Decisões de "good taste":**
- `Numero` é texto, não inteiro — modela o **domínio real**, não o nome do campo.
- `Cep` normalizado para 8 dígitos em **um único ponto** (no Service, antes de persistir) —
  nunca confiar na origem (ViaCEP devolve com hífen; usuário digita com/sem). Dado consistente.
- `UNIQUE(Usuario)`: login duplicado é **logicamente impossível** → o banco garante.
- Índice em `IdUsuario`: **toda** query do app filtra por ele → sem o índice, *table scan*
  linear; com ele, busca logarítmica (B-Tree).
- `NVARCHAR` (Unicode) para nomes/logradouros com acento.

### 4.2 Entrega dos scripts (requisito literal da AeC)
A AeC pede **"apenas os scripts de criação da estrutura das tabelas"**. Para não criar dupla fonte
de verdade, **um único artefato é entregue**: o DDL escrito à mão em
[`../db/scripts/01-create-tables.sql`](../db/scripts/01-create-tables.sql) — exatamente o que o
enunciado pede. A migração EF Code-First permanece como **detalhe interno de desenvolvimento**, não
como artefato entregue. **Antes da entrega**, conferir a paridade rodando
`dotnet ef migrations script --idempotent` e fazendo o *diff* contra o `.sql` à mão, para que não
exista divergência silenciosa (constraints, nomes de índice, `DeleteBehavior`).

---

## 5. Segurança, autenticação e criptografia (ADR-002)

| Vetor | Mitigação |
|-------|-----------|
| **Senha** | `PasswordHasher<Usuario>` nativo — **classe do shared framework** (`using Microsoft.AspNetCore.Identity`; **não é pacote NuGet**, não puxa o Identity completo). No .NET 8 é **PBKDF2-HMAC-SHA256, 100.000 iterações** (default IdentityV3), salt de 128 bits por hash, formato versionado e comparação em tempo constante. *Rehash* **não** é automático: `VerifyHashedPassword` pode retornar `SuccessRehashNeeded`, mas re-hashear é responsabilidade do chamador (opcional aqui). **Nunca** reinventar cripto. |
| **Sessão** | Cookie Authentication **sem** ASP.NET Core Identity. Cookie `HttpOnly` + `Secure` + **`SameSite=Lax`** (default; equilíbrio padrão contra CSRF e não quebra navegação top-level; o antiforgery token cobre o resto). `ExpireTimeSpan` 8h, `SlidingExpiration`. |
| **Autorização** | `[Authorize]` no `EnderecosController`; `[AllowAnonymous]` no login. Pipeline na ordem correta: `UseRouting → UseAuthentication → UseAuthorization`. |
| **IDOR / vazamento entre usuários** | **EF Global Query Filter** (`HasQueryFilter`) na entidade `Endereco`, ancorado no `IdUsuario` do cookie (ADR-004). O `AppDbContext` recebe `IHttpContextAccessor` no construtor e expõe um **campo de instância** `_currentUserId`; o filtro lê esse campo (`HasQueryFilter(e => e.IdUsuario == _currentUserId)`) — nunca uma variável capturada (o model do EF é cacheado uma vez). Fora de requisição HTTP (seed/migração) `_currentUserId = 0` → filtro retorna vazio com segurança, sem `NullReferenceException`. Torna o vazamento **impossível por construção** — inclusive em `Find`/edição/exclusão. |
| **CSRF** | `[ValidateAntiForgeryToken]` (ou `[AutoValidateAntiforgeryToken]` global) + `asp-antiforgery` nos forms POST. |
| **XSS** | Razor faz *encoding* por padrão; nunca usar `Html.Raw` com input do usuário. |
| **SQL Injection** | EF Core parametriza tudo; zero SQL concatenado. |
| **Segredos** | Connection string via **User-Secrets/variável de ambiente**, nunca no `appsettings` de um repo público. |

**Fluxo de login (resumo):** valida `Usuario` no banco → `VerifyHashedPassword` →
monta `ClaimsPrincipal` (claim `NameIdentifier` = Id, `Name` = usuário) →
`HttpContext.SignInAsync` → redireciona para `Enderecos/Index`. Falha → mensagem genérica
*"Credenciais inválidas"* (sem revelar se foi o usuário ou a senha).

---

## 6. Integração ViaCEP — proxy interno resiliente (ADR-003)

**Decisão:** endpoint interno no MVC (`EnderecosController.BuscarCep`) que chama o ViaCEP no
**servidor** via **typed `HttpClient`** (`IHttpClientFactory`). O JS do cliente nunca fala com
`viacep.com.br` diretamente.

**Por quê (o motivo certo, não "evitar CORS"):** o ViaCEP responde com CORS aberto. A razão
real é **arquitetural**: o servidor é quem **persiste e valida**; normalização do CEP, timeout,
*logging* e degradação ficam num só lugar, testável, e **mostra C#** — que é o que está sendo
avaliado.

**Contrato ViaCEP (verificado):**
- `GET https://viacep.com.br/ws/{cep}/json/` — `{cep}` com 8 dígitos.
- Sucesso (HTTP 200): `cep, logradouro, complemento, bairro, localidade, uf, ...`
  → mapear **`localidade` → `Cidade`** e `uf → Uf`. `numero` **não** vem da API.
- **CEP inexistente:** HTTP **200** com corpo `{"erro": "true"}` — ⚠️ na versão atual `true`
  vem como **STRING**. Desserializar **tolerante**: tipar `Erro` como `string?` e comparar
  `string.Equals(erro, "true", OrdinalIgnoreCase)`, **ou** registrar um `JsonConverter<bool>`
  que aceite `JsonTokenType.String` e `True/False`. Checar **antes** de mapear. O DTO modela só
  os campos usados; campos extras da API (`ddd`, `ibge`, `gia`, `siafi`, `unidade`, `regiao`) são
  ignorados (comportamento padrão do `System.Text.Json` p/ campos não mapeados).
- **CEP mal-formado:** HTTP 400 → `GetFromJsonAsync` lança `HttpRequestException`. Mitigado pela
  normalização prévia (8 dígitos); tratado defensivamente. **Fluxo principal de "não achou" é o
  200 + `erro`**, não o 400.

**Resiliência (dimensionada ao escopo):** `Timeout` de 5s no typed client + `try/catch`
(`HttpRequestException`, `TaskCanceledException`, `JsonException`). **Polly é exagero aqui** —
documentar no README que `Microsoft.Extensions.Http.Resilience` (`AddStandardResilienceHandler`)
seria o caminho com SLA/volume, deixado de fora de propósito. `CancellationToken` propagado do
controller até o `GetFromJsonAsync`.

**Comportamento em falha:** o serviço **nunca estoura 500**; devolve um resultado consistente
("CEP não encontrado / indisponível") e o front libera os campos para digitação manual.

---

## 7. Exportação CSV — CsvHelper, por consistência de julgamento (ADR-006)

**Decisão (revista após revisão adversarial):** usar **`CsvHelper`** (uma linha de configuração).
Razão: é a **única** decisão que reduz risco em vez de adicioná-lo e mantém o plano **internamente
consistente** — a mesma régua que rejeita criptografia escrita à mão (ADR-002) rejeita reescrever
*escaping* de CSV, que é um problema igualmente resolvido. O domínio demonstra-se **escolhendo a
ferramenta certa + um teste que prova o resultado**, não reimplementando a biblioteca.

> 🔁 **Alternativa documentada (reversível):** um writer próprio (~40 linhas) RFC 4180 + BOM é
> aceitável **se** o candidato quiser exibir domínio do formato — porém **só** com a suíte T4
> completa e código fortemente comentado. Trade-off registrado no README.

**O que importa de verdade no CSV (cobrir e/ou configurar):**
1. *Escaping* RFC 4180: vírgula, aspas internas (`"`→`""`), quebra de linha — **CsvHelper já faz**.
2. **Complemento vazio** → campo vazio, **nunca** `"null"`.
3. **BOM UTF-8** no início → Excel pt-BR abre "São João"/"Belém" com acento correto
   (`new UTF8Encoding(true)` no `StreamWriter`).
4. Separador: vírgula (RFC 4180); documentar se optar por `;` (Excel pt-BR).

> ❌ **Removido:** prefixar células iniciadas com `= + - @` (defesa de "CSV injection"). Isso
> **corromperia o dado real** (`=Praça` viraria `'=Praça` visível no Excel) para mitigar um vetor
> inexistente em endereços, cujo consumidor é um Excel local de confiança. Documentar como omissão
> consciente.

**Entrega:** `EnderecosController.Exportar` lê com **`AsNoTracking()`** (read-only → menos
memória/tempo), gera os bytes e retorna
`File(bytes, "text/csv", $"enderecos_{data:yyyy-MM-dd}.csv")` — nome de arquivo decente é parte
do produto.

---

## 8. Tratamento de erros — rede de segurança enxuta (ADR-007)

**Decisão:** **um** `GlobalExceptionHandler` (`IExceptionHandler`, .NET 8) +
`app.UseExceptionHandler()`, **sem** transformar em catedral (nada de Strategy/Dictionary
elaborado — _aviso do Akita: em MVC com Views isso vira overkill_).

- **Ações AJAX/JSON** (busca de CEP): retornam um **JSON simples** (`{ "erro": "mensagem" }`) —
  mais legível por um júnior do que `ProblemDetails`/RFC; não vendemos conformidade RFC onde
  ninguém pediu.
- **Navegação de páginas:** `UseStatusCodePagesWithReExecute("/Error/{0}")` + view amigável.
- **Sempre:** *log* estruturado via `ILogger`; **nunca** vazar *stack trace* ao usuário em
  produção. Em `Development`, `UseDeveloperExceptionPage`.

Na prática, o handler é **rede de segurança**: o `ViaCepService` já trata suas falhas localmente
(não estoura), então o handler captura o inesperado.

---

## 9. Design de interface e UX (ADR — Jobs)

**Stack visual:** Bootstrap 5 (sem jQuery para componentes; *unobtrusive validation* usa o
jQuery que já vem no template), **JS vanilla mínimo**, mobile-first. Polimento é **subtração**.

### 9.1 O momento mágico — autopreenchimento por CEP (60% do polimento aqui)
Sequência que precisa ser **perfeita** (Jobs rejeita a entrega se estiver "mais ou menos"):
1. Usuário digita o CEP (máscara `00000-000`, aceita colar com/sem traço).
2. Ao completar 8 dígitos (ou `blur`) → **spinner no próprio campo**.
3. `fetch` no endpoint interno → logradouro/bairro/cidade/uf **se preenchem sozinhos** com
   *highlight* sutil (readonly suave nos campos vindos da API).
4. **Foco pula automaticamente para `Número`** — a única coisa que o ViaCEP não sabe.
5. Erro/indisponível → mensagem inline gentil *"CEP não encontrado, preencha manualmente"* e
   libera os campos. **Nunca** um alerta de exceção.

### 9.2 Os três estados que separam CRUD de produto
- **Vazio:** lista sem endereços → *"Você ainda não cadastrou endereços"* + botão *"Adicionar
  primeiro endereço"* (não uma grid triste e vazia).
- **Loading:** spinner no CEP e no submit; nunca tela congelada.
- **Erro:** inline e gentil; *toasts* só onde agregam (não em toda ação).

### 9.3 Telas
- **Login:** card centralizado, limpo, *floating labels*, nome da app, credencial demo visível.
- **Lista:** *data grid* responsiva, espaçamento generoso; botão **Adicionar** (primário) e
  **Exportar CSV** (contorno, com ícone de download).
- **Form (criar/editar):** validação inline (`asp-validation-for`); o herói é o campo CEP.
- **Excluir:** **modal** de confirmação citando o endereço real (*"Excluir o endereço da Rua X,
  123?"*), nunca um *"Tem certeza?"* genérico nem exclusão direta.

**Microcopy** em português humano: "Salvar endereço", "Endereço salvo", "Excluir" (não "Deletar
registro" / "Submit").

---

## 10. Estratégia de testes e QA (ADR — Akita)

**Princípio:** testes **cirúrgicos**, não cobertura por métrica. Cada teste precisa ser capaz de
**pegar um bug real** deste escopo — senão é deletado. Sem testar getter/setter, ViewModel ou
"o EF salva" (isso é testar o framework).

### 10.1 Pirâmide enxuta (alvo: 8–12 testes de alto valor)
| # | Teste | Tipo | Por que vale |
|---|-------|------|--------------|
| T1 | `ViaCepService` — CEP válido mapeia `localidade→Cidade` | Unit (HttpMessageHandler mockado) | Lógica de integração |
| T2 | `ViaCepService` — `{"erro":"true"}` vira "não encontrado" | Unit (mock) | Caso de borda real da API |
| T3 | `ViaCepService` — timeout/500 degrada sem estourar | Unit (mock) | Resiliência |
| T4 | Exportação CSV — vírgula, aspas, quebra de linha, complemento vazio, BOM | Unit | Prova o resultado do export (§7) |
| T5 | Hashing — senha certa passa, senha errada falha | Unit | Prova uso correto do `PasswordHasher` |
| T6 | Normalização de CEP (com/sem máscara → 8 dígitos) | Unit | Dado consistente (RN-03) |
| T7 | **Isolamento (IDOR-leitura)**: A não vê endereço de B na lista | Integração (SQLite in-memory) | Prova o Query Filter |
| T8 | **Isolamento (IDOR-escrita)**: A não edita/exclui endereço de B → 404 | Integração | Cobre o furo do filtro (só-leitura) |
| T9 | Fluxo de login → redirect → lista | Integração (`WebApplicationFactory`) | Caminho crítico ponta a ponta |

> **TDD de verdade** nas unidades puras (T1–T6: escreve o teste que falha primeiro). Em
> Controllers/UI, **não** forçar TDD — escrever os testes de integração (T7–T9) depois.
> Testes de integração usam **SQLite in-memory** (`Microsoft.Data.Sqlite` + `UseSqlite`) — e
> **não** o provider EF InMemory, que não traduz SQL real nem exercita os *Global Query Filters*
> com fidelidade. SQLite in-memory é rápido, determinístico, sem container no CI, e valida a
> tradução LINQ de verdade. **Nenhum teste toca a internet real do ViaCEP.**

### 10.2 Quality gates (antes de **cada** commit)
```
dotnet format --verify-no-changes   # estilo consistente
dotnet build -warnaserror           # zero warning
dotnet test                         # verde obrigatório
```
**CI:** GitHub Actions de ~20 linhas rodando os três no push. *"Vale mais ponto que uma quinta
camada de arquitetura."*

---

## 11. Estratégia de Git e entrega (ADR — Linus/AeC)

A AeC exige **um commit por funcionalidade**. Cada **funcionalidade da spec** (autenticação, CRUD,
ViaCEP, CSV) é **um** commit — uma fatia vertical que compila, passa nos testes e **já entrega a UI
polida** daquela parte (o polimento mora dentro da fatia, não num commit "órfão" no fim). Mensagens
no **imperativo**, com o *porquê* no corpo. Sem commits "WIP" ou "adicionei camada X". Mapeamento
completo em [`03-backlog-execucao.md`](03-backlog-execucao.md).

Sequência de commits planejada (4 de funcionalidade + 3 de suporte atômicos):
1. `chore: scaffold do projeto MVC + script SQL + CI` *(suporte: base do repo)*
2. `feat: autenticação por cookie com PasswordHasher` *(funcionalidade — login, já polido)*
3. `feat: CRUD de endereços com isolamento por usuário` *(funcionalidade — já com estados vazio/erro)*
4. `feat: autopreenchimento por CEP via ViaCEP (proxy interno)` *(funcionalidade — já com spinner/erro)*
5. `feat: exportação CSV` *(funcionalidade)*
6. `docs: README com decisões de arquitetura e instruções` *(suporte: entrega)*

> **Aderência ao requisito:** há **um commit por funcionalidade do enunciado** (2–5). Os commits de
> *suporte* (scaffold, docs) são atômicos e claramente rotulados — o README explicita esse mapeamento
> para que nenhum avaliador literal leia ruído. O polimento de UI/UX foi **fundido** em cada fatia
> funcional (não há mais um commit de "polimento" solto).

**Entrega:** repositório **público** no GitHub + link por e-mail. **README** descrevendo o teste
(conforme a spec) **e** com seção "Decisões de arquitetura" que **defende ativamente** o monólito,
o `PasswordHasher` nativo, o Query Filter e a escolha de CSV — transformando possíveis objeções em
demonstração de julgamento (estrutura do README no §12).

---

## 12. README — o documento de onboarding (requisito + diferencial)

> **Fronteira do que se entrega (meta-decisão importante).** Estes documentos de planejamento
> (`docs/`, ADRs, RTM) são **bastidores** — artefatos de trabalho do time/arquiteto. O **repositório
> entregue à AeC** recebe um **README enxuto** (1 página), com a seção "Decisões de arquitetura" em
> ~5 *bullets*. Não transformamos a defesa antioverengineering em mais um monumento; o repo público
> é limpo e direto. (Os ADRs podem, no máximo, ir para uma pasta `docs/` claramente rotulada como
> aprofundamento opcional.)

Estrutura planejada do `README.md` do repositório entregue:
1. **O que é** (descrição do teste, conforme a spec da AeC).
2. **Stack** (.NET 8, ASP.NET Core MVC, EF Core, SQL Server, Bootstrap 5). Nota de uma linha:
   *"ASP.NET MVC"* da spec foi lido como **ASP.NET Core MVC (.NET 8 LTS)**, o padrão atual suportado.
3. **Como rodar** (`dotnet restore/run`, connection string via User-Secrets, seed de demo).
4. **Credenciais de demonstração** (dois usuários: `ana` / `bruno` — para o avaliador entrar e
   **ver o isolamento** em 30s).
5. **Como rodar os testes** (`dotnet test`).
6. **Scripts do banco** (link para `db/scripts` — o artefato de criação único).
7. **Decisões de arquitetura** (o coração, ~5 bullets: por que monólito, por que `PasswordHasher`
   nativo, por que CsvHelper, por que Query Filter).
8. **O que ficou de fora de propósito** (Polly, Clean Arch, cadastro self-service, features extras,
   defesa de CSV-injection) — prova de foco e julgamento.
9. **Decisões de engenharia derivadas** (uma linha): isolamento por usuário e proteção de rota são
   inferências dos *critérios de segurança* da avaliação, não exigências textuais do enunciado.
10. **Screenshots** do fluxo (login → lista → mágica do CEP → CSV).

---

## 13. Riscos e mitigações (consolidado do painel)
| Risco | Mitigação |
|-------|-----------|
| Tempo curto: superinvestir em testes e não entregar UI usável | *Time-box*: 8–12 testes; o resto é UI funcional + README. |
| IA (Claude Code App) injetar MediatR/AutoMapper/Repository "por boa prática" | Revisar **cada** dependência do `.csproj`; se não serve a ESTE escopo, sai. |
| Query Filter dá falso senso de segurança (só leitura) | T8 cobre edição/exclusão de recurso alheio → 404. |
| Query Filter com `IdUsuario` nulo (seed/migração) esconde tudo e parece bug | Tratar contexto sem usuário explicitamente; filtro só em `Endereco`, nunca em `Usuario`. |
| ViaCEP `{"erro":"true"}` como string quebra desserialização | Desserialização tolerante (string ou bool) + teste T2. |
| CSV sem BOM → acento quebrado no Excel pt-BR | BOM UTF-8 + teste T4; percepção de qualidade preservada. |
| Avaliador esquecido sem conseguir logar | Credencial demo na tela **e** no README. |
| Avaliador que premia Clean Arch por reflexo | README defende o monólito com argumento — vira demonstração de julgamento. |
| Segredo commitado em repo público | User-Secrets/env + placeholder no `appsettings`. |

---

## 14. Próximos passos
1. **Aprovação deste plano** (gate humano — princípio Akita: humano lidera).
2. Execução em **loop incremental TDD** (ver [`03-backlog-execucao.md`](03-backlog-execucao.md)),
   uma fatia vertical por vez, um commit por funcionalidade, quality gates verdes a cada passo.
3. Revisão final de arquitetura, segurança e manutenibilidade antes da entrega.
