# 03 — Backlog de Execução (loop incremental TDD)

> Plano de execução em **fatias verticais**, cada uma = **um commit por funcionalidade**
> (requisito da AeC). Cada fatia tem: objetivo, critério de aceite, testes (TDD onde aplicável)
> e *Definition of Done*. Ordem pensada para sempre ter algo que **compila, passa e roda**.
>
> **Loop por fatia (princípio Akita):** definir aceite → escrever teste que falha →
> implementar o mínimo → verificar (build/format/test) → refatorar → commit atômico → repetir.

---

## DoD global (vale para toda fatia)
- [ ] `dotnet format --verify-no-changes` limpo
- [ ] `dotnet build -warnaserror` sem warnings
- [ ] `dotnet test` verde
- [ ] Sem segredo commitado (connection string via User-Secrets/env)
- [ ] Commit atômico, mensagem no imperativo, *porquê* no corpo

---

## Fatia 0 — Scaffold + banco + CI
**Commit:** `chore: scaffold do projeto MVC + scripts SQL + CI`

- Criar `src/GestaoEnderecos` (`dotnet new mvc`, net8.0, `global.json`).
- Pacotes: `Microsoft.EntityFrameworkCore.SqlServer`, `...Tools`, `...Design` e `CsvHelper`.
  ⚠️ **NÃO** adicionar pacote `Microsoft.AspNetCore.Identity`: o `PasswordHasher<T>` já vem no
  *shared framework* (`Microsoft.AspNetCore.App`) — é só um `using`, zero pacote extra.
- Entidades `Usuario`, `Endereco`; `AppDbContext` (DbSets vazios por enquanto).
- Migração inicial + `db/scripts/01-create-tables.sql` (via `ef migrations script`).
- `.github/workflows/ci.yml` (format + build + test).
- `.gitignore`, `appsettings` com placeholder, `README` esqueleto.

**Aceite:** projeto compila; `dotnet run` sobe; CI verde no push.
**Testes:** nenhum (scaffold).

---

## Fatia 1 — Autenticação
**Commit:** `feat: autenticação por cookie com PasswordHasher`

- `AutenticacaoService` (valida `Usuario` + `VerifyHashedPassword`).
- Cookie auth no `Program.cs` (`AddAuthentication/AddCookie`, `LoginPath`, ordem do pipeline).
- `AccountController` (Login GET/POST, Logout). `ClaimsPrincipal` com `NameIdentifier`.
- `DbSeeder` cria **dois** usuários de **demonstração** (`ana`, `bruno`) com endereços distintos,
  hash gerado no seed → permite ver o isolamento sem rodar testes.
- View de login (card, floating labels, credenciais demo visíveis, mensagem genérica de erro).
- **Já entregar a tela de login polida** (o polimento vive na fatia, não num commit separado).

**Aceite (R-01/R-02/R-03):** login válido redireciona para `Enderecos/Index`; inválido mostra
"Credenciais inválidas"; logout encerra a sessão.
**Testes (TDD):**
- T5 — hashing: senha certa passa, errada falha.
- T9 — integração: POST login válido → 302 para a lista (`WebApplicationFactory`).

---

## Fatia 2 — CRUD de endereços + isolamento
**Commit:** `feat: CRUD de endereços com isolamento por usuário (query filter)`

- `EnderecoService` (create/list/get/update/delete) usando `DbContext` direto.
- **Global Query Filter** por `IdUsuario` (`UsuarioContext` scoped + `IHttpContextAccessor`).
- `EnderecosController` `[Authorize]` (Index, Create, Edit, Delete) — *thin*.
- ViewModels com `DataAnnotations` (RN-02/04/05); CEP normalizado no Service (RN-03).
- Views: lista (estado **vazio** tratado), form criar/editar, **modal** de exclusão.
- Antiforgery em todos os POST.

- **Global Query Filter robusto:** `AppDbContext` lê `_currentUserId` (campo de instância) do
  `IHttpContextAccessor`; **fallback `= 0`** quando não há `HttpContext`/claim (seed/migração) →
  conjunto vazio sem `NullReferenceException`. Comentar no `OnModelCreating` por que é scoped/instância.
- Estados **vazio/erro** da lista já entregues nesta fatia (polimento embutido).

**Aceite (R-04..R-09):** usuário logado faz CRUD completo só dos próprios endereços; complemento
opcional; número aceita texto; UF validada; exclusão confirmada em modal.
**Testes (TDD/integração):**
- T6 — normalização de CEP (com/sem máscara → 8 dígitos).
- T7 — IDOR-leitura: A não vê endereço de B na lista.
- T8 — IDOR-escrita: A editar/excluir endereço de B → 404.

---

## Fatia 3 — Autopreenchimento por CEP (ViaCEP)
**Commit:** `feat: autopreenchimento por CEP via ViaCEP (proxy interno)`

- `IViaCepService`/`ViaCepService` (typed `HttpClient`, timeout 5s, async, `CancellationToken`).
- DTO `ViaCepResponse` com desserialização **tolerante** do campo `erro` (string/bool);
  mapear `localidade→Cidade`.
- Endpoint interno `EnderecosController.BuscarCep` (retorna JSON limpo / "não encontrado").
- `wwwroot/js/cep.js` (vanilla): máscara, spinner no campo, fetch, preenchimento, **foco→Número**,
  mensagem inline em falha.

**Aceite (R-10/R-11):** CEP válido preenche os campos e pula o foco para Número; CEP inexistente
ou ViaCEP fora → mensagem gentil + entrada manual liberada (sem 500).
**Testes (TDD):**
- T1 — CEP válido mapeia `localidade→Cidade`.
- T2 — `{"erro":"true"}` (string) vira "não encontrado".
- T3 — timeout/500 degrada sem estourar.

---

## Fatia 4 — Exportação CSV
**Commit:** `feat: exportação CSV`

- Export via **`CsvHelper`** (config mínima) com `StreamWriter` em **UTF-8 com BOM**
  (`new UTF8Encoding(true)`) para o Excel pt-BR abrir acentos corretos.
- `EnderecosController.Exportar` com `AsNoTracking()`; `File(bytes,"text/csv","enderecos_AAAA-MM-DD.csv")`.
- Botão **Exportar CSV** (contorno + ícone) na lista.

**Aceite (R-12):** download de CSV com os endereços do usuário; abre correto no Excel pt-BR
(acentos), colunas íntegras mesmo com vírgula/aspas/quebra de linha nos campos.
**Teste:**
- T4 — um teste de exportação que prova o resultado: cabeçalho + linhas, campo com vírgula/aspas/
  quebra de linha escapado corretamente, **complemento vazio = campo vazio** (não `"null"`), BOM
  presente. _(Sem regra de CSV-injection — corromperia o dado; ver ADR-006.)_

---

## Polimento de UI/UX — transversal (NÃO é um commit separado)
> Decisão pós-revisão: **não há commit "de polimento"**. Cada fatia funcional (1–4) **já entrega
> sua parte polida** — login bonito na Fatia 1, estados vazio/erro na Fatia 2, spinner/erro do CEP
> na Fatia 3. Isto mantém "um commit por funcionalidade" honesto e evita um commit órfão.

Checklist de polimento aplicado **dentro de cada fatia**:
- Refino visual (Bootstrap 5, espaçamento, uma cor de destaque, mobile-first).
- Estados **vazio/loading/erro** consistentes; *toasts* só onde agregam.
- Microcopy em português humano; modal de exclusão citando o endereço real.
- Acessibilidade básica (labels, foco, contraste).

**Verificação (NFR-06):** fluxo fluido em mobile e desktop; nenhum estado "morto"
(verificação manual no preview + revisão visual a cada fatia).

---

## Fatia 5 — README e entrega
**Commit:** `docs: README com decisões de arquitetura e instruções`

- `README.md` conforme estrutura do §12 do Plano Diretor (descrição do teste + decisões +
  como rodar + credencial demo + screenshots + "o que ficou de fora").
- Conferência da matriz de rastreabilidade (todo R-xx ↔ commit ↔ teste).
- Repositório **público** no GitHub; link por e-mail.

**Aceite (R-13..R-16):** README completo; DDL presente; histórico com um commit por
funcionalidade; repo público acessível.

---

## Rastreabilidade requisito → fatia → teste
| Requisito | Fatia | Teste |
|-----------|-------|-------|
| R-01/02/03 (login) | 1 | T5, T9 |
| R-04 (proteção área logada) | 1–2 | T9 |
| R-05..R-09 (CRUD + validação) | 2 | T6, (T7/T8) |
| R-06 (isolamento) | 2 | T7, T8 |
| R-10/R-11 (ViaCEP) | 3 | T1, T2, T3 |
| R-12 (CSV) | 4 | T4 |
| R-13 (DDL) | 0 | revisão |
| R-14/15/16 (README/commits/repo) | 0–6 | revisão |
