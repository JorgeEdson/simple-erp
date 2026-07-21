using Microsoft.EntityFrameworkCore;
using Npgsql;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.Entidades;
using simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor;
using simple_erp.Infraestrutura.Persistencia.Contexto;

namespace simple_erp.Infraestrutura.Repositorios.ParceirosComerciais
{  
    public abstract class ParceiroComercialRepositoryBase<TParceiro> where TParceiro : ParceiroComercial
    {
        protected readonly SimpleErpDbContext Contexto;

        protected ParceiroComercialRepositoryBase(SimpleErpDbContext contexto)
        {
            Contexto = contexto;
        }

        
        protected abstract string TabelaComSchema { get; }

        protected async Task<Resultado<bool>> AdicionarInternoAsync(TParceiro parceiro, CancellationToken cancellationToken)
        {
            try
            {
                await Contexto.Set<TParceiro>().AddAsync(parceiro, cancellationToken);
                return Resultado<bool>.Sucesso(true);
            }
            catch (Exception ex)
            {
                return Resultado<bool>.Falha(ex.Message);
            }
        }

        protected Task<Resultado<bool>> AtualizarInternoAsync(TParceiro parceiro, CancellationToken cancellationToken)
        {
            try
            {
                var entry = Contexto.Entry(parceiro);
                
                if (entry.State == EntityState.Detached)
                    Contexto.Set<TParceiro>().Update(parceiro);

                return Task.FromResult(Resultado<bool>.Sucesso(true));
            }
            catch (Exception ex)
            {
                return Task.FromResult(Resultado<bool>.Falha(ex.Message));
            }
        }

        protected async Task<Resultado<TParceiro>> ObterPorIdInternoAsync(Id id, CancellationToken cancellationToken)
        {
            try
            {
                var parceiro = await Contexto.Set<TParceiro>()
                    .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

                // Contrato: Sucesso com instância nula quando não encontrado —
                // o use case decide o erro de negócio (ex.: CLIENTE_NAO_ENCONTRADO).
                return Resultado<TParceiro>.Sucesso(parceiro!);
            }
            catch (Exception ex)
            {
                return Resultado<TParceiro>.Falha(ex.Message);
            }
        }

        protected async Task<Resultado<TParceiro?>> ObterPorDocumentoInternoAsync(Documento documento, CancellationToken cancellationToken)
        {
            try
            {
                var parceiro = await Contexto.Set<TParceiro>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Documento == documento, cancellationToken);

                return Resultado<TParceiro?>.Sucesso(parceiro);
            }
            catch (Exception ex)
            {
                return Resultado<TParceiro?>.Falha(ex.Message);
            }
        }

        protected async Task<Resultado<bool>> ExistePorIdInternoAsync(Id id, CancellationToken cancellationToken)
        {
            try
            {
                var existe = await Contexto.Set<TParceiro>()
                    .AsNoTracking()
                    .AnyAsync(p => p.Id == id, cancellationToken);

                return Resultado<bool>.Sucesso(existe);
            }
            catch (Exception ex)
            {
                return Resultado<bool>.Falha(ex.Message);
            }
        }

        protected async Task<Resultado<bool>> ExistePorDocumentoInternoAsync(Documento documento, CancellationToken cancellationToken)
        {
            try
            {
                var existe = await Contexto.Set<TParceiro>()
                    .AsNoTracking()
                    .AnyAsync(p => p.Documento == documento, cancellationToken);

                return Resultado<bool>.Sucesso(existe);
            }
            catch (Exception ex)
            {
                return Resultado<bool>.Falha(ex.Message);
            }
        }

        protected async Task<Resultado<bool>> ExisteOutroPorDocumentoInternoAsync(Id id, Documento documento, CancellationToken cancellationToken)
        {
            try
            {
                var existe = await Contexto.Set<TParceiro>()
                    .AsNoTracking()
                    .AnyAsync(p => p.Documento == documento && p.Id != id, cancellationToken);

                return Resultado<bool>.Sucesso(existe);
            }
            catch (Exception ex)
            {
                return Resultado<bool>.Falha(ex.Message);
            }
        }

        protected async Task<Resultado<ResultadoPaginado<TParceiro>>> ListarPaginadoInternoAsync(
            int numeroPagina,
            int tamanhoPagina,
            string? nome,
            string? documento,
            string? email,
            bool? ativo,
            string? cidade,
            string? estado,
            CancellationToken cancellationToken)
        {
            try
            {
                var pagina = numeroPagina < 1 ? 1 : numeroPagina;
                var tamanho = tamanhoPagina < 1 ? 10 : Math.Min(tamanhoPagina, 100);

                var consulta = CriarConsultaFiltrada(nome, documento, email, ativo, cidade, estado);

                var total = await consulta.CountAsync(cancellationToken);

                var itens = await consulta
                    .AsNoTracking()
                    .OrderBy(p => p.Nome)
                    .Skip((pagina - 1) * tamanho)
                    .Take(tamanho)
                    .ToListAsync(cancellationToken);

                return Resultado<ResultadoPaginado<TParceiro>>.Sucesso(
                    new ResultadoPaginado<TParceiro>(
                        Itens: itens,
                        NumeroPagina: pagina,
                        TamanhoPagina: tamanho,
                        TotalRegistros: total));
            }
            catch (Exception ex)
            {
                return Resultado<ResultadoPaginado<TParceiro>>.Falha(ex.Message);
            }
        }

        private IQueryable<TParceiro> CriarConsultaFiltrada(
            string? nome,
            string? documento,
            string? email,
            bool? ativo,
            string? cidade,
            string? estado)
        {   
            var sql = $"""
                SELECT * FROM {TabelaComSchema}
                WHERE (@nome::text IS NULL OR nome ILIKE @nome)
                  AND (@documento::text IS NULL OR documento LIKE @documento)
                  AND (@email::text IS NULL OR email ILIKE @email)
                  AND (@ativo::boolean IS NULL OR ativo = @ativo)
                  AND (@cidade::text IS NULL OR endereco->>'Cidade' ILIKE @cidade)
                  AND (@estado::text IS NULL OR endereco->>'Estado' ILIKE @estado)
                """;

            return Contexto.Set<TParceiro>().FromSqlRaw(
                sql,
                CriarParametro("nome", PadraoContem(nome)),
                CriarParametro("documento", PadraoContem(ApenasDigitos(documento))),
                CriarParametro("email", PadraoContem(email)),
                CriarParametro("ativo", ativo),
                CriarParametro("cidade", PadraoContem(cidade)),
                CriarParametro("estado", Normalizar(estado)));
        }

        private static NpgsqlParameter CriarParametro(string nome, object? valor) =>
            new(nome, valor ?? DBNull.Value);

        private static string? Normalizar(string? valor) =>
            string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

        private static string? PadraoContem(string? valor)
        {
            var normalizado = Normalizar(valor);
            return normalizado is null ? null : $"%{normalizado}%";
        }

        private static string? ApenasDigitos(string? valor)
        {
            var normalizado = Normalizar(valor);

            if (normalizado is null)
                return null;

            var digitos = new string(normalizado.Where(char.IsDigit).ToArray());
            return digitos.Length == 0 ? null : digitos;
        }
    }
}
