# Planilha de exemplo — Importação de endereços

O arquivo [`enderecos-exemplo-importacao.csv`](enderecos-exemplo-importacao.csv) demonstra **todos
os comportamentos** da importação: linhas válidas (com vários casos de borda) e linhas inválidas
(uma por tipo de erro). Use-o em **Endereços → Importar CSV**.

Resultado esperado: **10 importadas, 12 rejeitadas** (22 linhas de dados).

## Colunas
`CEP, Logradouro, Número, Complemento, Bairro, Cidade, UF`
(o cabeçalho aceita `Número` ou `Numero`; o mesmo formato gerado por **Exportar CSV**).

## Linhas válidas (10) — cobrem casos de borda
| Caso demonstrado |
|------------------|
| CEP com máscara (`01001-000`) e complemento vazio |
| CEP sem máscara e com complemento |
| Número não numérico (`S/N`) |
| **UF em minúsculas** (`ba`) — normalizada para `BA` |
| **Vírgula no logradouro** (entre aspas) — `"Rua XV de Novembro, 200"` |
| Número `0` |
| **Aspas dentro do complemento** — `Sala "B"` |
| CEP com pontos (`01.310-100`) — normalizado para 8 dígitos |

## Linhas inválidas (12) — rejeitadas com motivo
| Linha | Motivo |
|-------|--------|
| Rua Sem CEP | CEP vazio |
| Rua com CEP Curto (`123`) | CEP com menos de 8 dígitos |
| Rua com CEP em Letras (`abcdefgh`) | CEP não numérico |
| (logradouro vazio) | Logradouro obrigatório |
| Rua Sem Bairro | Bairro obrigatório |
| Rua Sem Cidade | Cidade obrigatória |
| Rua Sem Numero | Número obrigatório |
| UF Inexistente (`XX`) | UF fora das 27 unidades federativas |
| UF Inválida (`Brasil`) | UF inválida |
| Numero Muito Longo | Número excede 15 caracteres |
| Logradouro longo | Logradouro excede 150 caracteres |
| Complemento longo | Complemento excede 60 caracteres |

> A importação **nunca falha o arquivo inteiro** por causa de uma linha ruim: importa as válidas e
> lista as rejeitadas com o número da linha e o motivo. Os endereços importados pertencem apenas ao
> usuário autenticado (mesmo isolamento do CRUD).
