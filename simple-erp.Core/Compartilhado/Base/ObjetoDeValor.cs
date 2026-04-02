using System;
using System.Collections.Generic;
using System.Text;

namespace simple_erp.Core.Compartilhado.Base
{
    public interface IConfiguracaoObjetoDeValor
    {
    }

    public abstract class ObjetoDeValor<TValor, TConfiguracao>
    where TConfiguracao : IConfiguracaoObjetoDeValor
    {
        protected ObjetoDeValor(TValor valor, TConfiguracao? configuracao = default)
        {
            Valor = valor;
            Configuracao = configuracao;
        }

        public TValor Valor { get; }
        public TConfiguracao? Configuracao { get; }

        public bool IgualA(ObjetoDeValor<TValor, TConfiguracao> objetoDeValor)
        {
            return EqualityComparer<TValor>.Default.Equals(Valor, objetoDeValor.Valor);
        }

        public bool DiferenteDe(ObjetoDeValor<TValor, TConfiguracao> objetoDeValor)
        {
            return !IgualA(objetoDeValor);
        }
    }
}
