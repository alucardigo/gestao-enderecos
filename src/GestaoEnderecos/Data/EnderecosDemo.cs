using GestaoEnderecos.Models;

namespace GestaoEnderecos.Data;

/// <summary>
/// Conjunto de endereços reais brasileiros (CEP, logradouro, bairro, cidade, UF) para popular a
/// demonstração com dados verossímeis e extensos. Distribuídos entre os usuários de demonstração.
/// </summary>
public static class EnderecosDemo
{
    public static IReadOnlyList<Endereco> Lista() =>
    [
        N("01310-100", "Avenida Paulista", "Bela Vista", "São Paulo", "SP", "1578"),
        N("01001-000", "Praça da Sé", "Sé", "São Paulo", "SP", "S/N"),
        N("01419-101", "Alameda Santos", "Cerqueira César", "São Paulo", "SP", "1000"),
        N("04094-050", "Avenida Ibirapuera", "Indianópolis", "São Paulo", "SP", "3103"),
        N("05402-000", "Rua Oscar Freire", "Jardim Paulista", "São Paulo", "SP", "725"),
        N("08090-284", "Avenida Aricanduva", "Vila Aricanduva", "São Paulo", "SP", "5555"),
        N("20040-002", "Rua da Assembleia", "Centro", "Rio de Janeiro", "RJ", "10"),
        N("22070-011", "Avenida Atlântica", "Copacabana", "Rio de Janeiro", "RJ", "1702"),
        N("22410-003", "Avenida Vieira Souto", "Ipanema", "Rio de Janeiro", "RJ", "176"),
        N("20021-130", "Avenida Rio Branco", "Centro", "Rio de Janeiro", "RJ", "156"),
        N("24230-002", "Rua Coronel Moreira César", "Icaraí", "Niterói", "RJ", "229"),
        N("30130-009", "Avenida Afonso Pena", "Centro", "Belo Horizonte", "MG", "1212"),
        N("30140-071", "Rua da Bahia", "Centro", "Belo Horizonte", "MG", "1148"),
        N("30310-000", "Avenida do Contorno", "Funcionários", "Belo Horizonte", "MG", "6594"),
        N("38400-100", "Avenida Rondon Pacheco", "Centro", "Uberlândia", "MG", "1000"),
        N("40020-000", "Avenida Sete de Setembro", "Centro", "Salvador", "BA", "123"),
        N("41750-300", "Avenida Tancredo Neves", "Caminho das Árvores", "Salvador", "BA", "1632"),
        N("40170-110", "Avenida Oceânica", "Barra", "Salvador", "BA", "2400"),
        N("80010-000", "Rua XV de Novembro", "Centro", "Curitiba", "PR", "200"),
        N("80420-090", "Avenida Sete de Setembro", "Centro", "Curitiba", "PR", "2775"),
        N("82530-200", "Avenida Anita Garibaldi", "Cabral", "Curitiba", "PR", "850"),
        N("86010-000", "Avenida Higienópolis", "Centro", "Londrina", "PR", "32"),
        N("90010-150", "Rua dos Andradas", "Centro Histórico", "Porto Alegre", "RS", "1001"),
        N("90570-020", "Avenida Carlos Gomes", "Auxiliadora", "Porto Alegre", "RS", "222"),
        N("95020-472", "Rua Sinimbu", "Centro", "Caxias do Sul", "RS", "1700"),
        N("50030-230", "Rua do Bom Jesus", "Recife Antigo", "Recife", "PE", "77"),
        N("51020-000", "Avenida Boa Viagem", "Boa Viagem", "Recife", "PE", "3722"),
        N("52050-000", "Rua da Hora", "Espinheiro", "Recife", "PE", "300"),
        N("60160-230", "Avenida Beira Mar", "Meireles", "Fortaleza", "CE", "3980"),
        N("60115-000", "Avenida Dom Luís", "Aldeota", "Fortaleza", "CE", "500"),
        N("60050-110", "Rua Senador Pompeu", "Centro", "Fortaleza", "CE", "350"),
        N("70040-010", "Esplanada dos Ministérios", "Zona Cívico-Administrativa", "Brasília", "DF", "S/N"),
        N("70297-400", "Praça dos Três Poderes", "Zona Cívico-Administrativa", "Brasília", "DF", "S/N"),
        N("71219-900", "SHIS QI 23", "Lago Sul", "Brasília", "DF", "10"),
        N("69005-070", "Avenida Eduardo Ribeiro", "Centro", "Manaus", "AM", "620"),
        N("69020-030", "Rua dos Barés", "Centro", "Manaus", "AM", "45"),
        N("88010-400", "Rua Felipe Schmidt", "Centro", "Florianópolis", "SC", "390"),
        N("88015-200", "Avenida Beira-Mar Norte", "Centro", "Florianópolis", "SC", "2800"),
        N("89010-000", "Rua XV de Novembro", "Centro", "Blumenau", "SC", "550"),
        N("74015-908", "Avenida Goiás", "Setor Central", "Goiânia", "GO", "100"),
        N("74110-010", "Avenida T-9", "Setor Bueno", "Goiânia", "GO", "1500"),
        N("29010-360", "Avenida Jerônimo Monteiro", "Centro", "Vitória", "ES", "1000"),
        N("29055-260", "Avenida Nossa Senhora da Penha", "Praia do Canto", "Vitória", "ES", "1495"),
        N("64000-280", "Avenida Frei Serafim", "Centro", "Teresina", "PI", "2280"),
        N("59020-110", "Avenida Rio Branco", "Cidade Alta", "Natal", "RN", "634"),
        N("58013-420", "Avenida Epitácio Pessoa", "Estados", "João Pessoa", "PB", "1200"),
        N("57020-600", "Avenida da Paz", "Centro", "Maceió", "AL", "1422"),
        N("49015-110", "Avenida Barão de Maruim", "Centro", "Aracaju", "SE", "200"),
        N("66053-000", "Avenida Presidente Vargas", "Campina", "Belém", "PA", "800"),
        N("78005-370", "Avenida Getúlio Vargas", "Centro Norte", "Cuiabá", "MT", "1300"),
    ];

    private static Endereco N(string cep, string logradouro, string bairro, string cidade, string uf, string numero) =>
        new()
        {
            Cep = Models.Cep.Normalizar(cep),
            Logradouro = logradouro,
            Bairro = bairro,
            Cidade = cidade,
            Uf = uf,
            Numero = numero,
        };
}
