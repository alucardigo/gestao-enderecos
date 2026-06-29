# 02 — Architecture Decision Records (ADRs)

> Registro das decisões arquiteturais e suas justificativas. Cada ADR nasceu de um confronto
> entre o relatório prévio (Gemini) e um painel de revisão (Akita / DHH / Linus / Jobs +
> verificação técnica .NET). Formato: **Contexto → Decisão → Consequências → Alternativas
> rejeitadas**. Este documento é a defesa do *julgamento* por trás do código.

---

## ADR-001 — Monólito Majestoso em vez de Clean Architecture multi-projeto
**Status:** Aceito · **Decisores:** painel unânime (Akita, DHH, Linus, Jobs)

**Contexto.** O relatório Gemini propôs Clean Architecture em 4 camadas (Core/Aplicação/
Infraestrutura/Apresentação) com `IEnderecoRepository`, `IUsuarioRepository` e camada de
*use cases*. O escopo real: **2 entidades** (Usuario, Endereco) e **1 integração** (ViaCEP).

**Decisão.** Um único projeto ASP.NET Core MVC organizado por **pastas** (Controllers, Services,
Data, Models, ViewModels, Views). Controllers finos delegam a Services de negócio; os Services
usam o `DbContext` **diretamente**. Nenhum repositório sobre o EF.

**Consequências.**
- ✅ O avaliador entende a solution em 30 segundos; um júnior navega sem se perder.
- ✅ Demonstra a habilidade mais difícil de ensinar: **julgar a dose de abstração**.
- ✅ Menos código, menos indireção, menos superfície de bug.
- ⚠️ Avaliador que premia camadas por reflexo pode estranhar → **mitigado pelo README** que
  defende a escolha explicitamente.

**Alternativas rejeitadas.** Clean Architecture multi-projeto (*layer-itis* para 2 tabelas;
"insegurança disfarçada de arquitetura"). Repository genérico/`BaseRepository<T>`
(reescreve o EF com menos poder). O `DbContext` **já é** Repository + Unit of Work.

---

## ADR-002 — `PasswordHasher<TUser>` nativo em vez de PBKDF2 manual
**Status:** Aceito · **Decisores:** painel unânime · **Criticidade:** ALTA (segurança)

**Contexto.** O Gemini rejeitou o ASP.NET Core Identity (correto, seria pesado) **mas** mandou
escrever PBKDF2 à mão (`Rfc2898DeriveBytes`, salt 128–256 bits, 310k iterações, comparação em
tempo constante). É uma contradição: reinventar a primitiva criptográfica é justamente onde
*"o código que um sênior faria"* significa **não** escrever você mesmo.

**Decisão.** Usar `PasswordHasher<Usuario>` — classe do `using Microsoft.AspNetCore.Identity`,
**já incluída no shared framework** (`Microsoft.AspNetCore.App`): **não é pacote NuGet** e **não**
puxa o Identity completo / `AddIdentity` / tabelas extras. Registrar no DI; `HashPassword` na
criação/seed; `VerifyHashedPassword` no login.

**Consequências.**
- ✅ No .NET 8: **PBKDF2-HMAC-SHA256, 100.000 iterações** (default IdentityV3), salt de 128 bits
  por hash, formato versionado e comparação em tempo constante — tudo mantido pela Microsoft.
- ✅ *Rehash*: `VerifyHashedPassword` sinaliza `SuccessRehashNeeded` quando o formato envelhece;
  re-hashear é decisão **explícita** do chamador (opcional neste escopo, **não** automático).
- ✅ Ganha os pontos de "boas práticas + segurança" com código curto e auditável.
- ✅ Remove 3 lugares para errar sutilmente (serialização do salt, escolha de iterações,
  comparação não-constante).
- ⚠️ Acopla a um tipo do namespace Identity (trivial; é só uma classe, zero pacote/tabela).

**Alternativas rejeitadas.** PBKDF2 manual (superfície de bug; "sinal de alerta de segurança,
não de competência"). Texto puro / MD5 / SHA-256 sem salt (reprovação direta). Identity completo
(overengineering para um login simples).

---

## ADR-003 — ViaCEP via proxy interno (typed HttpClient) em vez de chamada direta do JS
**Status:** Aceito · **Decisores:** painel unânime (DHH, Jobs)

**Contexto.** Autopreenchimento por CEP. Opções: JS do browser chama `viacep.com.br` direto, ou
um endpoint interno no MVC faz a chamada no servidor.

**Decisão.** Endpoint interno (`EnderecosController.BuscarCep`) usando **typed `HttpClient`** via
`IHttpClientFactory`, 100% async, `Timeout` 5s. O JS só faz `fetch` no endpoint interno.

**Consequências.**
- ✅ Normalização, validação, timeout, *logging* e degradação **num só lugar**, testável.
- ✅ A lógica de integração vive em **C#** — que é o que o teste avalia.
- ✅ `IHttpClientFactory` evita *socket exhaustion* (erro clássico do `new HttpClient()`).
- ✅ `IViaCepService` é a **única** interface justificada (dependência externa mockável).

**Nota sobre a justificativa do Gemini.** O motivo dele ("evitar CORS") está **errado** — o
ViaCEP tem CORS aberto. O motivo real é arquitetural (servidor é dono da verdade + testabilidade).

**Alternativas rejeitadas.** Chamada direta do JS (vaza lógica de erro/normalização para o
cliente; esconde a competência em C#). Polly/circuit-breaker (ver ADR-008).

---

## ADR-004 — Isolamento por usuário via EF Global Query Filter
**Status:** Aceito · **Decisores:** painel unânime (Linus lead) · **Criticidade:** ALTA (segurança)

**Contexto.** Cada usuário só pode ver/editar seus endereços (multitenancy lógico por
`IdUsuario`). O padrão ingênuo — `WHERE IdUsuario = @atual` em cada query — funciona **até** o
dia em que alguém esquece em **uma** query (tipicamente o `Find` do Delete/Edit) → **IDOR**
(deletar/editar endereço alheio passando o Id na URL).

**Decisão.** `modelBuilder.Entity<Endereco>().HasQueryFilter(e => e.IdUsuario == _currentUserId)`
no `OnModelCreating`, com o `IdUsuario` lido do cookie via um provedor *scoped*
(`IHttpContextAccessor`, **por instância** do `DbContext` — nunca singleton). Toda query
(list, `Find`, `Include`, edição, exclusão) sai filtrada automaticamente.

**Consequências.**
- ✅ O vazamento de dados deixa de ser **escrevível** — *good taste*: reestruturar para que o
  caso ruim não exista, em vez de um `if` para tratá-lo.
- ✅ Cobre o furo clássico do `Find` no Delete/Edit.
- ⚠️ O filtro atua na **leitura**; é preciso **também** validar que escrever/editar com Id alheio
  retorna 404 → **teste T8** cobre isso.
- ⚠️ Filtro só na entidade `Endereco` — **nunca** em `Usuario` (o login precisa achar o usuário
  *antes* de existir sessão). Tratar contexto sem usuário (seed/migração) explicitamente.

**Alternativas rejeitadas.** Filtragem manual por query (depende de memória humana; falha
silenciosa). Confiar em `IdUsuario` vindo do form/route (é o próprio vetor de IDOR).

---

## ADR-005 — EF Core Code-First + Fluent API; DDL gerado e versionado
**Status:** Aceito · **Decisores:** verificação técnica + Linus

**Contexto.** A AeC pede **"apenas os scripts de criação das tabelas"**, mas o desenvolvimento
usa EF Core. Como atender o requisito literal sem divergir do modelo?

**Decisão.** EF Core *Code-First* como fonte da verdade; configuração via **Fluent API** em
`OnModelCreating` (entidades limpas; `DataAnnotations` só nas ViewModels). Gerar o DDL com
`dotnet ef migrations script --idempotent -o db/scripts/01-create-tables.sql` e **versioná-lo**.
Um `.sql` de referência escrito à mão (espelhando o modelo) acompanha o repo.

**Consequências.**
- ✅ Atende o requisito ao pé da letra **e** demonstra domínio de migrações.
- ✅ `DataAnnotations` de validação não vazam para o schema do banco.
- ⚠️ Disciplina: regenerar o script quando o modelo mudar.

**Alternativas rejeitadas.** Entregar migrações do EF no lugar do DDL (ignora o enunciado).
DDL 100% à mão como fonte da verdade (diverge do modelo Code-First).

---

## ADR-006 — Exportação CSV com CsvHelper (consistência de julgamento)
**Status:** Aceito (revisto após revisão adversarial; antes favorecia writer próprio) · **Reversível**

**Contexto.** O painel dividiu 2–2: **Akita + DHH** → `CsvHelper` (não reinventar o resolvido);
**Linus + Jobs** → writer próprio (~40 linhas) RFC 4180 + BOM (demonstra domínio; o avaliador abre
no Excel). A revisão adversarial desempatou apontando que o writer próprio era a **única** decisão
do plano que *adicionava* risco e **contradizia a régua interna** (ADR-002 rejeita reescrever
criptografia; *escaping* de CSV é igualmente um problema resolvido).

**Decisão.** Usar **`CsvHelper`** (config mínima), gerando o arquivo em **UTF-8 com BOM**
(`new UTF8Encoding(true)`) para o Excel pt-BR. O domínio é demonstrado **escolhendo a ferramenta
certa + um teste (T4) que prova o resultado**, não reimplementando a biblioteca. **CSV-injection
prefixing foi removido** (corromperia o dado real; vetor inexistente em endereços de Excel local).

**Consequências.**
- ✅ Plano internamente consistente: a mesma régua que rejeita cripto à mão rejeita CSV à mão.
- ✅ Zero risco de *escaping* (vírgula/aspas/quebra de linha) — é o que o `CsvHelper` resolve.
- ✅ T4 prova o resultado (cabeçalho, *escaping*, complemento vazio = campo vazio, BOM).
- ⚠️ Uma dependência madura a mais (custo trivial, amplamente aceito).

**Alternativa documentada (reversível).** Writer próprio (~40 linhas) RFC 4180 + BOM é aceitável
**se** o candidato quiser exibir domínio do formato — porém **só** com a suíte T4 completa e código
fortemente comentado. Decisão registrada no README.

---

## ADR-007 — Tratamento global de erros enxuto (IExceptionHandler)
**Status:** Aceito · **Decisores:** verificação técnica, com ressalva do Akita

**Contexto.** Evitar a "tela amarela da morte" e vazamento de *stack trace*, sem poluir
controllers com `try/catch` espalhado nem construir uma catedral de mapeamento.

**Decisão.** **Um** `GlobalExceptionHandler` (`IExceptionHandler`, .NET 8) +
`app.UseExceptionHandler()`. Ações AJAX/JSON (busca de CEP) → **JSON simples**
(`{ "erro": "mensagem" }`) — mais legível por júnior do que `ProblemDetails`/RFC, que não foi
pedido. Navegação → `UseStatusCodePagesWithReExecute("/Error/{0}")` + view amigável. *Log* via
`ILogger`. `DeveloperExceptionPage` só em Development.

**Consequências.**
- ✅ Controllers livres de burocracia de captura de erro.
- ✅ Sigilo operacional (sem *stack trace* ao cliente em produção).
- ⚠️ **Ressalva (Akita):** manter **simples** — sem Strategy/Dictionary elaborado. O handler é
  rede de segurança; o `ViaCepService` já trata as próprias falhas localmente.

**Alternativas rejeitadas.** `try/catch` por ação (viola DRY, polui). Middleware customizado
artesanal (o `IExceptionHandler` nativo já resolve). Mapeamento Strategy elaborado (overkill).

---

## ADR-008 — .NET 8 LTS como alvo; resiliência mínima (sem Polly)
**Status:** Aceito · **Decisores:** verificação técnica

**Contexto.** Escolher *runtime* e nível de resiliência da chamada externa.

**Decisão.** **.NET 8 LTS** (suporte até nov/2026) — a aposta mais **portável** para um avaliador
que precisa clonar e rodar (`dotnet run`). Fixar em `global.json` + `<TargetFramework>net8.0`.
Resiliência da chamada ViaCEP: **timeout (5s) + try/catch**, sem Polly.

**Consequências.**
- ✅ Máxima portabilidade de execução na máquina do avaliador.
- ✅ Código idêntico em .NET 10 (`IExceptionHandler` existe desde .NET 8). Trocar é trivial.
- ✅ Resiliência dimensionada ao escopo; sem dependência supérflua.
- ⚠️ Documentar no README que `Microsoft.Extensions.Http.Resilience`
  (`AddStandardResilienceHandler`) seria o caminho com SLA/volume — deixado de fora **de propósito**.

**Alternativas rejeitadas.** .NET 9 (STS, fora de suporte). Polly/circuit-breaker (overengineering
para um CRUD de teste). .NET 10 é aceitável como "demonstra atualização", mas perde portabilidade.
