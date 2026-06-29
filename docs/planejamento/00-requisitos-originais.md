# 00 — Requisitos Originais (Fonte da Verdade)

> Transcrição fiel do edital técnico **`Teste_Dev_CSharp 1.pdf`** ("Teste Dev. C# — Time de Sistemas").
> Este documento é a **fonte da verdade**. Qualquer decisão de arquitetura, design ou escopo
> é serva destes requisitos — nunca o contrário. O relatório do Gemini
> (`Planejamento Técnico Processo Seletivo Desenvolvedor.pdf`) é **insumo**, não autoridade.

---

## Objetivo do teste

Desenvolver uma **aplicação web em C#** que permita ao usuário:

1. Realizar **login**.
2. Gerenciar um **CRUD de endereços** — inserindo o endereço manualmente **ou** informando
   um **CEP** para a aplicação buscar os dados via integração com a **API do ViaCEP**
   (`https://viacep.com.br/`).
3. **Exportar** os endereços salvos para um arquivo **CSV**.

## Requisitos do sistema

### Tela de Login
- Autenticação de usuário.
- Validação de credenciais.
- Redirecionamento para a página de endereços após login bem-sucedido.

### CRUD de Endereços
- Adicionar, visualizar, editar e excluir endereços.
- Cada endereço deve conter: **cep, logradouro, complemento (opcional), bairro, cidade, uf, numero**.
- Exportar os endereços salvos para um arquivo **CSV**.

### Banco de dados
- Tabela **Usuários**: `Id, nome, usuário, senha`.
- Tabela **Endereços**: `Id, cep, logradouro, complemento (opcional), bairro, cidade, uf, numero, id do usuário`.
- ⚠️ **Enviar APENAS os scripts de criação da estrutura das tabelas.**

### Tecnologias sugeridas
- **ASP.NET MVC** para o backend.
- **Entity Framework** para interação com banco de dados.
- **HTML, CSS e JavaScript** para o frontend.
- **SQL Server** para o banco de dados.

## Critérios de avaliação
1. **Qualidade do código** (legibilidade, estrutura e organização).
2. **Boas práticas** de programação, **segurança** e **design patterns**.
3. **Funcionalidade** do sistema conforme os requisitos.
4. **Design e usabilidade** da interface.

## Entrega
O teste deve ser entregue em duas etapas:
1. Enviar **link do repositório por e-mail**.
2. Criação de **repositório no GitHub**:
   - Repositório **público**.
   - Deve existir um **`README.md`** com a descrição do teste (informações extras são opcionais).
   - **Para cada funcionalidade do sistema deve haver um commit.**

---

## Matriz de rastreabilidade de requisitos (RTM)

> Cada requisito recebe um ID e será amarrado a um commit, a critérios de aceite e a testes.
> Nenhuma linha pode ficar órfã na entrega.

> **Nota sobre origem.** Itens marcados **"Derivado/Seg"** (R-04, R-06, NFR-02) **não** estão no
> texto literal do enunciado — são decisões de engenharia inferidas dos *critérios de avaliação de
> segurança*. São mantidos por elevarem a entrega (e cobertos por T7/T8), mas registrados como
> inferência, não exigência textual, para não confundir julgamento com requisito.

| ID    | Requisito                                                        | Origem        | Verificado por           |
|-------|-----------------------------------------------------------------|---------------|--------------------------|
| R-01  | Tela de login com autenticação                                  | Login         | Teste integração + manual |
| R-02  | Validação de credenciais (usuário/senha)                        | Login         | Teste unit + integração   |
| R-03  | Redirecionar para endereços após login                          | Login         | Teste integração          |
| R-04  | Proteger área logada (acesso negado sem sessão)                 | Derivado/Seg  | Teste integração          |
| R-05  | Criar endereço (manual)                                         | CRUD          | Teste integração          |
| R-06  | Listar/visualizar endereços **do próprio usuário**             | Derivado/Seg  | Teste unit + integração   |
| R-07  | Editar endereço                                                 | CRUD          | Teste integração          |
| R-08  | Excluir endereço (com confirmação)                             | CRUD + UX     | Teste integração          |
| R-09  | Campos obrigatórios e complemento opcional                      | CRUD          | Teste unit (validação)    |
| R-10  | Autopreenchimento via ViaCEP a partir do CEP                    | Integração    | Teste unit (mock) + manual |
| R-11  | Degradação graciosa quando ViaCEP falha/indisponível           | Integração    | Teste unit (mock)         |
| R-12  | Exportar endereços para CSV (RFC 4180)                          | Export        | Teste unit (escaping)     |
| R-13  | Scripts DDL de criação das tabelas                              | Banco         | Revisão + execução        |
| R-14  | README.md descrevendo o teste                                   | Entrega       | Revisão                   |
| R-15  | Um commit por funcionalidade                                    | Entrega       | Revisão do histórico git  |
| R-16  | Repositório público no GitHub                                   | Entrega       | Revisão                   |

### Requisitos NÃO-funcionais derivados dos critérios de avaliação
| ID     | Requisito não-funcional                                                              |
|--------|--------------------------------------------------------------------------------------|
| NFR-01 | Senhas nunca em texto puro — hashing forte (PBKDF2) com salt                          |
| NFR-02 | Isolamento de dados entre usuários (multitenancy lógico por `IdUsuario`)              |
| NFR-03 | Proteção contra XSS, CSRF e SQL Injection                                             |
| NFR-04 | Código legível, autodescritivo, navegável por um júnior                              |
| NFR-05 | Resiliência a falhas de rede na integração externa (timeout + fallback)              |
| NFR-06 | Interface responsiva (mobile-first) e acessível                                       |
| NFR-07 | Tratamento de erros centralizado, sem vazar stack trace ao cliente                   |
