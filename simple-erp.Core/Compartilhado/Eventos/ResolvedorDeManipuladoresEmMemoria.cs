using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace simple_erp.Core.Compartilhado.Eventos
{

    public sealed class ResolvedorDeManipuladoresEmMemoria : IResolvedorDeManipuladores
    {
        private readonly Dictionary<Type, List<object>> _manipuladores = new();

        public ResolvedorDeManipuladoresEmMemoria Registrar<TEvento>(
            IManipuladorDeEventoDeDominio<TEvento> manipulador)
            where TEvento : EventoDeDominio
        {
            if (manipulador is null)
                throw new ArgumentNullException(nameof(manipulador));

            var tipo = typeof(TEvento);

            if (!_manipuladores.TryGetValue(tipo, out var lista))
            {
                lista = new List<object>();
                _manipuladores[tipo] = lista;
            }

            lista.Add(manipulador);
            return this;
        }

        public IReadOnlyCollection<object> ResolverPara(Type tipoDoEvento)
        {
            return _manipuladores.TryGetValue(tipoDoEvento, out var lista)
                ? lista.AsReadOnly()
                : Array.Empty<object>();
        }
    }
}
