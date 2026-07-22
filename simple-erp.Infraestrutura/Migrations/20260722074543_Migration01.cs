using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace simple_erp.Infraestrutura.Migrations
{
    /// <inheritdoc />
    public partial class Migration01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "parceiros");

            migrationBuilder.EnsureSchema(
                name: "producao");

            migrationBuilder.EnsureSchema(
                name: "estoque");

            migrationBuilder.EnsureSchema(
                name: "eventos");

            migrationBuilder.EnsureSchema(
                name: "suprimentos");

            migrationBuilder.EnsureSchema(
                name: "vendas");

            migrationBuilder.EnsureSchema(
                name: "catalogo");

            migrationBuilder.EnsureSchema(
                name: "financeiro");

            migrationBuilder.CreateTable(
                name: "clientes",
                schema: "parceiros",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    data_criacao_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    documento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    endereco = table.Column<string>(type: "jsonb", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clientes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "composicoes_de_produto",
                schema: "producao",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    id_produto_fabricado = table.Column<long>(type: "bigint", nullable: false),
                    versao = table.Column<int>(type: "integer", nullable: false),
                    ativa = table.Column<bool>(type: "boolean", nullable: false),
                    itens = table.Column<string>(type: "jsonb", nullable: false),
                    data_criacao_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_composicoes_de_produto", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fornecedores",
                schema: "parceiros",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    data_criacao_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    documento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    endereco = table.Column<string>(type: "jsonb", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fornecedores", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "movimentacoes",
                schema: "estoque",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    id_produto = table.Column<long>(type: "bigint", nullable: false),
                    tipo = table.Column<int>(type: "integer", nullable: false),
                    sentido = table.Column<int>(type: "integer", nullable: false),
                    quantidade = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    saldo_resultante = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    origem = table.Column<string>(type: "jsonb", nullable: false),
                    data_movimentacao_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_criacao_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_movimentacoes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ordens_de_producao",
                schema: "producao",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    id_produto_fabricado = table.Column<long>(type: "bigint", nullable: false),
                    id_composicao = table.Column<long>(type: "bigint", nullable: false),
                    quantidade_a_produzir = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    necessidades = table.Column<string>(type: "jsonb", nullable: false),
                    data_criacao_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ordens_de_producao", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox",
                schema: "eventos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_evento = table.Column<long>(type: "bigint", nullable: false),
                    nome_do_evento = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tipo_do_evento = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    id_agregado_origem = table.Column<long>(type: "bigint", nullable: false),
                    conteudo = table.Column<string>(type: "jsonb", nullable: false),
                    ocorrido_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    criado_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processado_em_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tentativas = table.Column<int>(type: "integer", nullable: false),
                    ultimo_erro = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pedidos_de_compra",
                schema: "suprimentos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    id_fornecedor = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    itens = table.Column<string>(type: "jsonb", nullable: false),
                    data_criacao_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pedidos_de_compra", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pedidos_de_venda",
                schema: "vendas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    numero = table.Column<int>(type: "integer", nullable: false),
                    id_cliente = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    desconto_do_pedido = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    motivo_cancelamento = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    itens = table.Column<string>(type: "jsonb", nullable: false),
                    data_criacao_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pedidos_de_venda", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "produtos",
                schema: "catalogo",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    unidade_de_medida = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    classificacao = table.Column<int>(type: "integer", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    data_criacao_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_produtos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "saldos",
                schema: "estoque",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    id_produto = table.Column<long>(type: "bigint", nullable: false),
                    quantidade_atual = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    data_criacao_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_saldos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "titulos",
                schema: "financeiro",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    tipo = table.Column<int>(type: "integer", nullable: false),
                    id_parceiro = table.Column<long>(type: "bigint", nullable: false),
                    origem = table.Column<string>(type: "jsonb", nullable: false),
                    valor_original = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    data_vencimento_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    baixas = table.Column<string>(type: "jsonb", nullable: false),
                    data_criacao_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_atualizacao_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_titulos", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_clientes_documento",
                schema: "parceiros",
                table: "clientes",
                column: "documento",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_composicoes_produto_versao",
                schema: "producao",
                table: "composicoes_de_produto",
                columns: new[] { "id_produto_fabricado", "versao" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_fornecedores_documento",
                schema: "parceiros",
                table: "fornecedores",
                column: "documento",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_movimentacoes_produto_data",
                schema: "estoque",
                table: "movimentacoes",
                columns: new[] { "id_produto", "data_movimentacao_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_ordens_de_producao_produto_data",
                schema: "producao",
                table: "ordens_de_producao",
                columns: new[] { "id_produto_fabricado", "data_criacao_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_evento_agregado",
                schema: "eventos",
                table: "outbox",
                columns: new[] { "nome_do_evento", "id_agregado_origem" });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_pendentes",
                schema: "eventos",
                table: "outbox",
                column: "criado_em_utc",
                filter: "processado_em_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_de_compra_fornecedor_data",
                schema: "suprimentos",
                table: "pedidos_de_compra",
                columns: new[] { "id_fornecedor", "data_criacao_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_de_venda_cliente_data",
                schema: "vendas",
                table: "pedidos_de_venda",
                columns: new[] { "id_cliente", "data_criacao_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_pedidos_de_venda_numero",
                schema: "vendas",
                table: "pedidos_de_venda",
                column: "numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_produtos_codigo",
                schema: "catalogo",
                table: "produtos",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_saldos_id_produto",
                schema: "estoque",
                table: "saldos",
                column: "id_produto",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_titulos_parceiro_vencimento",
                schema: "financeiro",
                table: "titulos",
                columns: new[] { "id_parceiro", "data_vencimento_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "clientes",
                schema: "parceiros");

            migrationBuilder.DropTable(
                name: "composicoes_de_produto",
                schema: "producao");

            migrationBuilder.DropTable(
                name: "fornecedores",
                schema: "parceiros");

            migrationBuilder.DropTable(
                name: "movimentacoes",
                schema: "estoque");

            migrationBuilder.DropTable(
                name: "ordens_de_producao",
                schema: "producao");

            migrationBuilder.DropTable(
                name: "outbox",
                schema: "eventos");

            migrationBuilder.DropTable(
                name: "pedidos_de_compra",
                schema: "suprimentos");

            migrationBuilder.DropTable(
                name: "pedidos_de_venda",
                schema: "vendas");

            migrationBuilder.DropTable(
                name: "produtos",
                schema: "catalogo");

            migrationBuilder.DropTable(
                name: "saldos",
                schema: "estoque");

            migrationBuilder.DropTable(
                name: "titulos",
                schema: "financeiro");
        }
    }
}
