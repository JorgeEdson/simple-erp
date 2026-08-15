using simple_erp.Core.Modulos.CatalogoDeProdutos.ObjetosDeValor;

namespace simple_erp.Api.Configuracao.Seed
{
    public sealed record ParceiroDeSeed(
        Guid Id,
        string Nome,
        string Documento,
        string Email,
        string Rua,
        string Numero,
        string Complemento,
        string Bairro,
        string Cidade,
        string Estado,
        string Cep);

    public sealed record ProdutoDeSeed(
        Guid Id,
        string Codigo,
        string Descricao,
        string UnidadeDeMedida,
        ClassificacaoProduto Classificacao);


    public static class CatalogoDeSeed
    {
        public const string Pais = "Brasil";

        // CPFs e CNPJs com dígitos verificadores válidos — o VO Documento os valida, e
        // um número inventado faria a carga inteira falhar.
        public static readonly ParceiroDeSeed[] Clientes =
        {
            new(new Guid("00000000-0000-0000-0000-000000001001"), "Maria Alves Pereira",            "68807608480",    "maria.pereira@email.com",     "Rua das Acácias",        "245",  "Apto 52",    "Vila Mariana",   "São Paulo",      "SP", "04101-000"),
            new(new Guid("00000000-0000-0000-0000-000000001002"), "João Batista Nogueira",          "93584652391",    "joao.nogueira@email.com",     "Avenida Sete de Setembro","1180", "",           "Centro",         "Curitiba",       "PR", "80060-070"),
            new(new Guid("00000000-0000-0000-0000-000000001003"), "Ana Carolina Ribeiro",           "53039700197",    "ana.ribeiro@email.com",       "Rua Padre Chagas",       "78",   "Sala 3",     "Moinhos de Vento","Porto Alegre",  "RS", "90570-080"),
            new(new Guid("00000000-0000-0000-0000-000000001004"), "Carlos Eduardo Menezes",         "17257296529",    "carlos.menezes@email.com",    "Rua Antônio de Albuquerque","512","Apto 1201",  "Savassi",        "Belo Horizonte", "MG", "30112-010"),
            new(new Guid("00000000-0000-0000-0000-000000001005"), "Fernanda Lima Souza",            "52973101484",    "fernanda.souza@email.com",    "Avenida Boa Viagem",     "3344", "Apto 802",   "Boa Viagem",     "Recife",         "PE", "51020-000"),
            new(new Guid("00000000-0000-0000-0000-000000001006"), "Rafael Andrade Costa",           "71033162442",    "rafael.costa@email.com",      "Rua Barão de Itapetininga","255", "",           "República",      "São Paulo",      "SP", "01042-001"),
            new(new Guid("00000000-0000-0000-0000-000000001007"), "Juliana Moraes Pinto",           "15674361290",    "juliana.pinto@email.com",     "Rua Visconde de Pirajá",  "414",  "Cobertura",  "Ipanema",        "Rio de Janeiro", "RJ", "22410-002"),
            new(new Guid("00000000-0000-0000-0000-000000001008"), "Bruno Tavares Machado",          "36235318200",    "bruno.machado@email.com",     "Avenida Afonso Pena",    "1901", "Apto 405",   "Funcionários",   "Belo Horizonte", "MG", "30130-005"),

            new(new Guid("00000000-0000-0000-0000-000000001009"), "Contabilidade Horizonte Ltda",   "25461752000180", "contato@horizontecont.com.br","Rua XV de Novembro",     "620",  "Conjunto 91","Centro",         "Curitiba",       "PR", "80020-310"),
            new(new Guid("00000000-0000-0000-0000-000000001010"), "Clínica Vida Plena Ltda",        "20680577000107", "atendimento@vidaplena.com.br","Avenida Paulista",       "2064", "Torre B",    "Bela Vista",     "São Paulo",      "SP", "01310-200"),
            new(new Guid("00000000-0000-0000-0000-000000001011"), "Escritório Marques Advogados",   "98582941000185", "contato@marquesadv.com.br",   "Rua da Assembleia",      "10",   "Sala 2210",  "Centro",         "Rio de Janeiro", "RJ", "20011-901"),
            new(new Guid("00000000-0000-0000-0000-000000001012"), "Colégio Novo Saber",             "66154471000162", "secretaria@novosaber.com.br", "Rua Dom José Barea",     "180",  "",           "Jardim América", "Goiânia",        "GO", "74265-090"),
            new(new Guid("00000000-0000-0000-0000-000000001013"), "Construtora Pedra Alta",         "87637634000138", "obras@pedraalta.com.br",      "Avenida das Nações",     "1450", "Galpão 4",   "Distrito Industrial","Manaus",     "AM", "69075-000"),
            new(new Guid("00000000-0000-0000-0000-000000001014"), "Agência Ponto Digital",          "42843293000119", "comercial@pontodigital.com.br","Rua Fernandes Tourinho","350",  "Sala 704",   "Lourdes",        "Belo Horizonte", "MG", "30112-000"),
            new(new Guid("00000000-0000-0000-0000-000000001015"), "Rede Farmácia Bem Estar",        "65121431000151", "compras@fbemestar.com.br",    "Avenida Ipiranga",       "2200", "Loja 12",    "Praia de Belas", "Porto Alegre",   "RS", "90160-093")
        };

        public static readonly ParceiroDeSeed[] Fornecedores =
        {
            new(new Guid("00000000-0000-0000-0000-000000002001"), "Madeireira São Bento Ltda",          "47164538000103", "vendas@madeireirasaobento.com.br","Rodovia Anhanguera Km 32","0",  "Galpão A", "Distrito Industrial","Cajamar",   "SP", "07750-000"),
            new(new Guid("00000000-0000-0000-0000-000000002002"), "Ferragens Ipiranga Distribuidora",   "28119962000183", "pedidos@ferragensipiranga.com.br","Rua Silva Bueno",       "1890","",          "Ipiranga",       "São Paulo",   "SP", "04208-002"),
            new(new Guid("00000000-0000-0000-0000-000000002003"), "Química Nova Tintas e Vernizes",     "48044975000148", "comercial@quimicanova.com.br",    "Avenida Industrial",     "775", "Bloco 2",   "Assunção",       "São Bernardo do Campo","SP","09861-000"),
            new(new Guid("00000000-0000-0000-0000-000000002004"), "Metalúrgica Vale Aço",               "77972923000130", "vendas@metalvaleaco.com.br",      "Rua dos Metalúrgicos",   "410", "",          "Cidade Industrial","Contagem",  "MG", "32210-160"),
            new(new Guid("00000000-0000-0000-0000-000000002005"), "Componentes Prisma Comércio",        "92959110000101", "atendimento@prismacomp.com.br",   "Rua João Pessoa",        "233", "Sala 5",    "Centro",         "Joinville",   "SC", "89201-500"),
            new(new Guid("00000000-0000-0000-0000-000000002006"), "Distribuidora Papel & Cia",          "60009083000101", "sac@papelecia.com.br",            "Avenida Presidente Vargas","1900","Loja 3",  "Centro",         "Rio de Janeiro","RJ","20210-030")
        };

        
        public static readonly ProdutoDeSeed[] Produtos =
        {
            // ---------- Revenda (3001-3030): o que a padaria compra pronto e revende ----------
            new(new Guid("00000000-0000-0000-0000-000000003001"), "REV-001", "Leite Integral UHT 1L",                 "UN",  ClassificacaoProduto.Revenda),
            new(new Guid("00000000-0000-0000-0000-000000003002"), "REV-002", "Refrigerante Cola 2L",                  "UN",  ClassificacaoProduto.Revenda),
            new(new Guid("00000000-0000-0000-0000-000000003003"), "REV-003", "Água Mineral sem Gás 500ml",            "UN",  ClassificacaoProduto.Revenda),
            new(new Guid("00000000-0000-0000-0000-000000003004"), "REV-004", "Suco de Laranja Integral 1L",           "UN",  ClassificacaoProduto.Revenda),
            new(new Guid("00000000-0000-0000-0000-000000003005"), "REV-005", "Café Torrado e Moído 500g",             "PCT", ClassificacaoProduto.Revenda),
            new(new Guid("00000000-0000-0000-0000-000000003006"), "REV-006", "Achocolatado em Pó 400g",               "PCT", ClassificacaoProduto.Revenda),
            new(new Guid("00000000-0000-0000-0000-000000003007"), "REV-007", "Iogurte Natural 170g",                  "UN",  ClassificacaoProduto.Revenda),
            new(new Guid("00000000-0000-0000-0000-000000003008"), "REV-008", "Requeijão Cremoso 200g",                "UN",  ClassificacaoProduto.Revenda),
            new(new Guid("00000000-0000-0000-0000-000000003009"), "REV-009", "Manteiga com Sal 200g",                 "UN",  ClassificacaoProduto.Revenda),
            new(new Guid("00000000-0000-0000-0000-000000003010"), "REV-010", "Queijo Mussarela Fatiado",              "KG",  ClassificacaoProduto.Revenda),
            new(new Guid("00000000-0000-0000-0000-000000003011"), "REV-011", "Presunto Cozido Fatiado",               "KG",  ClassificacaoProduto.Revenda),
            new(new Guid("00000000-0000-0000-0000-000000003012"), "REV-012", "Mortadela Defumada Fatiada",            "KG",  ClassificacaoProduto.Revenda),
            new(new Guid("00000000-0000-0000-0000-000000003013"), "REV-013", "Ovos Brancos Tipo Grande",              "DZ",  ClassificacaoProduto.Revenda),
            new(new Guid("00000000-0000-0000-0000-000000003014"), "REV-014", "Chocolate ao Leite 90g",                "UN",  ClassificacaoProduto.Revenda),
            new(new Guid("00000000-0000-0000-0000-000000003015"), "REV-015", "Bala de Goma Sortida 500g",             "PCT", ClassificacaoProduto.Revenda),
            new(new Guid("00000000-0000-0000-0000-000000003016"), "REV-016", "Chiclete de Menta",                     "UN",  ClassificacaoProduto.Revenda),
            new(new Guid("00000000-0000-0000-0000-000000003017"), "REV-017", "Biscoito Recheado de Chocolate",        "PCT", ClassificacaoProduto.Revenda),
            new(new Guid("00000000-0000-0000-0000-000000003018"), "REV-018", "Salgadinho de Milho 100g",              "PCT", ClassificacaoProduto.Revenda),
            new(new Guid("00000000-0000-0000-0000-000000003019"), "REV-019", "Cerveja Pilsen Lata 350ml",             "UN",  ClassificacaoProduto.Revenda),
            new(new Guid("00000000-0000-0000-0000-000000003020"), "REV-020", "Energético Lata 250ml",                 "UN",  ClassificacaoProduto.Revenda),
            new(new Guid("00000000-0000-0000-0000-000000003021"), "REV-021", "Chá Gelado de Limão 450ml",             "UN",  ClassificacaoProduto.Revenda),
            new(new Guid("00000000-0000-0000-0000-000000003022"), "REV-022", "Pão de Forma Integral Industrializado", "UN",  ClassificacaoProduto.Revenda),
            new(new Guid("00000000-0000-0000-0000-000000003023"), "REV-023", "Geleia de Morango 230g",                "UN",  ClassificacaoProduto.Revenda),
            new(new Guid("00000000-0000-0000-0000-000000003024"), "REV-024", "Mel Silvestre 300g",                    "UN",  ClassificacaoProduto.Revenda),
            new(new Guid("00000000-0000-0000-0000-000000003025"), "REV-025", "Azeite Extra Virgem 500ml",             "UN",  ClassificacaoProduto.Revenda),
            new(new Guid("00000000-0000-0000-0000-000000003026"), "REV-026", "Guardanapo de Papel 50 folhas",         "PCT", ClassificacaoProduto.Revenda),
            new(new Guid("00000000-0000-0000-0000-000000003027"), "REV-027", "Copo Descartável 200ml",                "PCT", ClassificacaoProduto.Revenda),
            new(new Guid("00000000-0000-0000-0000-000000003028"), "REV-028", "Sacola Plástica Reforçada",             "PCT", ClassificacaoProduto.Revenda),
            new(new Guid("00000000-0000-0000-0000-000000003029"), "REV-029", "Vela de Aniversário Numérica",          "UN",  ClassificacaoProduto.Revenda),
            new(new Guid("00000000-0000-0000-0000-000000003030"), "REV-030", "Cartão de Aniversário",                 "UN",  ClassificacaoProduto.Revenda),

            // ---------- Matéria-prima (3031-3044): insumos de panificação e confeitaria ----------
            new(new Guid("00000000-0000-0000-0000-000000003031"), "MP-001", "Farinha de Trigo Tipo 1",                "KG",  ClassificacaoProduto.MateriaPrima),
            new(new Guid("00000000-0000-0000-0000-000000003032"), "MP-002", "Farinha de Trigo Integral",              "KG",  ClassificacaoProduto.MateriaPrima),
            new(new Guid("00000000-0000-0000-0000-000000003033"), "MP-003", "Polvilho Azedo",                         "KG",  ClassificacaoProduto.MateriaPrima),
            new(new Guid("00000000-0000-0000-0000-000000003034"), "MP-004", "Fermento Biológico Seco",                "KG",  ClassificacaoProduto.MateriaPrima),
            new(new Guid("00000000-0000-0000-0000-000000003035"), "MP-005", "Açúcar Refinado",                        "KG",  ClassificacaoProduto.MateriaPrima),
            new(new Guid("00000000-0000-0000-0000-000000003036"), "MP-006", "Açúcar de Confeiteiro",                  "KG",  ClassificacaoProduto.MateriaPrima),
            new(new Guid("00000000-0000-0000-0000-000000003037"), "MP-007", "Sal Refinado",                           "KG",  ClassificacaoProduto.MateriaPrima),
            new(new Guid("00000000-0000-0000-0000-000000003038"), "MP-008", "Manteiga sem Sal",                       "KG",  ClassificacaoProduto.MateriaPrima),
            new(new Guid("00000000-0000-0000-0000-000000003039"), "MP-009", "Ovo Pasteurizado",                       "L",   ClassificacaoProduto.MateriaPrima),
            new(new Guid("00000000-0000-0000-0000-000000003040"), "MP-010", "Leite Integral a Granel",                "L",   ClassificacaoProduto.MateriaPrima),
            new(new Guid("00000000-0000-0000-0000-000000003041"), "MP-011", "Creme de Leite Fresco",                  "L",   ClassificacaoProduto.MateriaPrima),
            new(new Guid("00000000-0000-0000-0000-000000003042"), "MP-012", "Chocolate em Pó 50% Cacau",              "KG",  ClassificacaoProduto.MateriaPrima),
            new(new Guid("00000000-0000-0000-0000-000000003043"), "MP-013", "Essência de Baunilha",                   "ML",  ClassificacaoProduto.MateriaPrima),
            new(new Guid("00000000-0000-0000-0000-000000003044"), "MP-014", "Doce de Leite para Recheio",             "KG",  ClassificacaoProduto.MateriaPrima),

            // ---------- Fabricados (3045-3050): o que sai do forno ----------
            new(new Guid("00000000-0000-0000-0000-000000003045"), "FAB-001", "Pão Francês",                           "KG",  ClassificacaoProduto.Fabricado),
            new(new Guid("00000000-0000-0000-0000-000000003046"), "FAB-002", "Pão de Queijo",                         "KG",  ClassificacaoProduto.Fabricado),
            new(new Guid("00000000-0000-0000-0000-000000003047"), "FAB-003", "Bolo de Chocolate 1kg",                 "UN",  ClassificacaoProduto.Fabricado),
            new(new Guid("00000000-0000-0000-0000-000000003048"), "FAB-004", "Sonho de Creme",                        "UN",  ClassificacaoProduto.Fabricado),
            new(new Guid("00000000-0000-0000-0000-000000003049"), "FAB-005", "Croissant Folhado",                     "UN",  ClassificacaoProduto.Fabricado),
            new(new Guid("00000000-0000-0000-0000-000000003050"), "FAB-006", "Torta Holandesa Fatia",                 "UN",  ClassificacaoProduto.Fabricado)
        };
    }
}
