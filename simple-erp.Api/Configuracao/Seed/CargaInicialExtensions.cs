using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.CatalogoDeProdutos.Entidades;
using simple_erp.Core.Modulos.CatalogoDeProdutos.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.Entidades;
using simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor;

namespace simple_erp.Api.Configuracao.Seed
{   
    public static class CargaInicialExtensions
    {
        public static async Task AplicarCargaInicialAsync(this WebApplication app)
        {
            await using var escopo = app.Services.CreateAsyncScope();

            var unitOfWork = escopo.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var logger = escopo.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("CargaInicial");

            if (await CargaJaAplicadaAsync(unitOfWork, logger))
                return;

            var erros = new List<string>();

            var clientes = Construir(CatalogoDeSeed.Clientes, CriarCliente, erros);
            var fornecedores = Construir(CatalogoDeSeed.Fornecedores, CriarFornecedor, erros);
            var produtos = Construir(CatalogoDeSeed.Produtos, CriarProduto, erros);

            // Falha aqui é erro de dado no catálogo, não do usuário. Abortamos antes de
            // gravar qualquer coisa para não deixar uma base pela metade.
            if (erros.Count > 0)
            {
                logger.LogError(
                    "Carga inicial abortada: {Quantidade} item(ns) do catálogo não passaram nas regras do domínio. {Erros}",
                    erros.Count,
                    string.Join(" | ", erros.Take(10)));

                return;
            }

            foreach (var cliente in clientes)
                await unitOfWork.ClientesRepository.AdicionarAsync(cliente);

            foreach (var fornecedor in fornecedores)
                await unitOfWork.FornecedoresRepository.AdicionarAsync(fornecedor);

            foreach (var produto in produtos)
                await unitOfWork.ProdutosRepository.AdicionarAsync(produto);

            var persistencia = await unitOfWork.SaveChangesAsync();

            if (persistencia.EhFalha)
            {
                logger.LogError(
                    "Falha ao gravar a carga inicial: {Erros}",
                    string.Join(" | ", persistencia.Erros!));

                return;
            }

            logger.LogInformation(
                "Carga inicial aplicada: {Clientes} clientes, {Fornecedores} fornecedores " +
                "e {Produtos} produtos.",
                clientes.Count, fornecedores.Count, produtos.Count);
        }
        
        private static async Task<bool> CargaJaAplicadaAsync(IUnitOfWork unitOfWork, ILogger logger)
        {
            var marcador = Id.TentarCriar(CatalogoDeSeed.Clientes[0].Id).Instancia;

            var existe = await unitOfWork.ClientesRepository.ExistePorIdAsync(marcador);

            if (existe.EhFalha)
            {
                logger.LogWarning(
                    "Não foi possível verificar a carga inicial: {Erros}",
                    string.Join(" | ", existe.Erros!));

                return true;
            }

            if (existe.Instancia)
            {
                logger.LogInformation("Carga inicial já aplicada; nada a fazer.");
                return true;
            }

            return false;
        }

        private static List<TAgregado> Construir<TDados, TAgregado>(
            IEnumerable<TDados> catalogo,
            Func<TDados, Resultado<TAgregado>> fabrica,
            List<string> erros)
        {
            var construidos = new List<TAgregado>();

            foreach (var dados in catalogo)
            {
                var resultado = fabrica(dados);

                if (resultado.EhFalha)
                    erros.AddRange(resultado.Erros!);
                else
                    construidos.Add(resultado.Instancia);
            }

            return construidos;
        }

        private static Resultado<Cliente> CriarCliente(ParceiroDeSeed dados)
        {
            var partes = CriarPartesDoParceiro(dados);

            if (partes.EhFalha)
                return Resultado<Cliente>.Falha(partes.Erros!);

            var (nome, documento, email, endereco) = partes.Instancia;

            return Cliente.Criar(nome, documento, email, endereco, dados.Id);
        }

        private static Resultado<Fornecedor> CriarFornecedor(ParceiroDeSeed dados)
        {
            var partes = CriarPartesDoParceiro(dados);

            if (partes.EhFalha)
                return Resultado<Fornecedor>.Falha(partes.Erros!);

            var (nome, documento, email, endereco) = partes.Instancia;

            return Fornecedor.Criar(nome, documento, email, endereco, dados.Id);
        }

        /// <summary>Cliente e Fornecedor compartilham os mesmos Value Objects.</summary>
        private static Resultado<(Nome Nome, Documento Documento, Email Email, Endereco Endereco)>
            CriarPartesDoParceiro(ParceiroDeSeed dados)
        {
            var nome = Nome.TentarCriar(dados.Nome);
            var documento = Documento.TentarCriar(dados.Documento);
            var email = Email.TentarCriar(dados.Email);

            var endereco = Endereco.TentarCriar(new PropriedadesEndereco(
                Rua: dados.Rua,
                Numero: dados.Numero,
                Complemento: dados.Complemento,
                Bairro: dados.Bairro,
                Cidade: dados.Cidade,
                Estado: dados.Estado,
                Cep: dados.Cep,
                Pais: CatalogoDeSeed.Pais));

            var combinado = Resultado.Combinar(nome, documento, email, endereco);

            if (combinado.EhFalha)
                return Resultado<(Nome, Documento, Email, Endereco)>.Falha(
                    Rotular($"parceiro {dados.Id} ({dados.Nome})", combinado.Erros!));

            return Resultado<(Nome, Documento, Email, Endereco)>.Sucesso(
                (nome.Instancia, documento.Instancia, email.Instancia, endereco.Instancia));
        }

        private static Resultado<Produto> CriarProduto(ProdutoDeSeed dados)
        {
            var codigo = CodigoProduto.TentarCriar(dados.Codigo);
            var descricao = DescricaoProduto.TentarCriar(dados.Descricao);
            var unidade = UnidadeDeMedida.TentarCriar(dados.UnidadeDeMedida);

            var combinado = Resultado.Combinar(codigo, descricao, unidade);

            if (combinado.EhFalha)
                return Resultado<Produto>.Falha(
                    Rotular($"produto {dados.Codigo}", combinado.Erros!));

            return Produto.Criar(
                codigo.Instancia,
                descricao.Instancia,
                unidade.Instancia,
                dados.Classificacao,
                dados.Id);
        }

        private static IEnumerable<string> Rotular(string rotulo, IEnumerable<string> erros) =>
            erros.Select(erro => $"{rotulo}: {erro}");
    }
}
