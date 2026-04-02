using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace simple_erp.Core.Compartilhado.Base
{
    public class Resultado
    {
        public bool EhSucesso => Erros == null || !Erros.Any();
        public bool EhFalha => !EhSucesso;
        public IEnumerable<string>? Erros { get; }
        protected Resultado(IEnumerable<string>? erros = null)
        {
            Erros = erros?.ToList();
        }
        public static Resultado Sucesso() => new Resultado();
        public static Resultado Falha(IEnumerable<string> erros) => new Resultado(erros);
        public static Resultado Falha(string erro) => new Resultado(new List<string> { erro });        
        public static Resultado Combinar(params Resultado[] resultados)
        {
            var erros = new List<string>();

            foreach (var resultado in resultados)
            {
                if (resultado.EhFalha)
                {
                    
                    erros.AddRange((IEnumerable<string>)resultado.Erros);
                }
            }

            return erros.Any() ? Falha(erros) : Sucesso();
        }
    }

    public class Resultado<T> : Resultado
    {
        private readonly T? _instancia;        

        private Resultado(T? instancia = default, List<string>? erros = null) : base(erros)
        {
            _instancia = instancia;            
        }

        public static Resultado<T> Sucesso(T? instancia = default)
        {
            return new Resultado<T>(instancia);
        }

        public static Resultado<T> Falha(string erro)
        {
            return new Resultado<T>(default, new List<string> { erro });
        }

        public static Resultado<T> Falha(IEnumerable<string> erros)
        {
            return new Resultado<T>(default, erros.ToList());
        }

        public static async Task<Resultado<T>> TentarAsync(Func<Task<Resultado<T>>> funcao)
        {
            try
            {
                return await funcao();
            }
            catch (Exception ex)
            {
                return Falha(ex.Message);
            }
        }

        public static Resultado<T> Tentar(Func<T> funcao)
        {
            try
            {
                return Sucesso(funcao());
            }
            catch (Exception ex)
            {
                return Falha(ex.Message);
            }
        }        

        public T Instancia
        {
            get
            {
                if (EhFalha)
                    throw new InvalidOperationException("Não é possível acessar a instância de um resultado com falha.");

                if (_instancia is null)
                    throw new InvalidOperationException("A instância está nula.");

                return _instancia;
            }
        }                

        public Resultado<object> ComFalha()
        {
            return Resultado<object>.Falha(Erros!);
        }

        public static Resultado<T[]> Combinar(params Resultado<T>[] resultados)
        {
            var falhas = resultados.Where(r => r.EhFalha).ToList();

            if (falhas.Any())
            {
                var erros = falhas
                    .SelectMany(r => r.Erros!)
                    .ToList();

                return Resultado<T[]>.Falha(erros);
            }

            var instancias = resultados
                .Select(r => r.Instancia)
                .ToArray();

            return Resultado<T[]>.Sucesso(instancias);
        }

        public static async Task<Resultado<T[]>> CombinarAsync(params Task<Resultado<T>>[] resultados)
        {
            var resultadosResolvidos = await Task.WhenAll(resultados);
            return Combinar(resultadosResolvidos);
        }
       
    }

    public sealed record ResultadoPaginado<T>(
        IReadOnlyCollection<T> Itens,
        int NumeroPagina,
        int TamanhoPagina,
        int TotalRegistros)
    {
        public int TotalPaginas =>
            TotalRegistros <= 0
                ? 0
                : (int)Math.Ceiling(TotalRegistros / (double)TamanhoPagina);
    }
}
