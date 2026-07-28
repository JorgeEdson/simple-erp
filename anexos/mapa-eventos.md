# Mapa de Eventos — Simple ERP

## Convenções

**Dois tipos de evento (a distinção central de DDD):**

- **Domain Event (intra-contexto)** — produtor e consumidor vivem no **mesmo** bounded context. Pode ser tratado de forma síncrona, na mesma transação.
- **Integration Event (entre contextos)** — o consumidor está em **outro** contexto. Deve ser **eventualmente consistente** (transação separada), respeitando a regra "uma transação = um agregado".

**Grau de obrigatoriedade do consumidor:**

- ◆ **Requerido** — exigido explicitamente pelos requisitos funcionais.
- ○ **Recomendado** — regra de negócio coerente/simétrica, não citada explicitamente nos requisitos.
- · **Potencial** — evolução futura razoável (auditoria, BI, notificação, CRM).

Um traço `—` indica que não há consumidor previsto para aquele lado.

---

## Catálogo de eventos

### Contexto: Parceiros Comerciais

| Evento | Emitido quando | Consumidores — mesmo contexto | Consumidores — outro contexto |
|---|---|---|---|
| `ClienteCadastrado` | Um cliente é cadastrado | — | · CRM / mensagem de boas-vindas |
| `ClienteInativado` | Um cliente é inativado | — | · Vendas (impedir novos pedidos); · Financeiro (revisar títulos a receber em aberto) |
| `ClienteReativado` | Um cliente é reativado | — | · Vendas (liberar novos pedidos) |
| `FornecedorCadastrado` | Um fornecedor é cadastrado | — | · CRM de fornecedores |
| `FornecedorInativado` | Um fornecedor é inativado | — | · Suprimentos (impedir novos pedidos de compra) |
| `FornecedorReativado` | Um fornecedor é reativado | — | · Suprimentos (liberar novos pedidos) |

> Payloads: eventos de cadastro carregam `Id`, `Documento`, `Nome`; os demais carregam apenas o `Id` do parceiro. Contexto **upstream** de identidade — publica muito, consome pouco.

### Contexto: Catálogo de Produtos

| Evento | Emitido quando | Mesmo contexto | Outro contexto |
|---|---|---|---|
| `ProdutoCadastrado` | Um produto é cadastrado | — | · Estoque (criar saldo zero proativamente — hoje o saldo nasce lazy na 1ª movimentação) |
| `ProdutoClassificadoComoFabricado` | Produto marcado como Fabricado | — | · Produção/Composição (habilitar definição de receita) |
| `ProdutoClassificadoComoMateriaPrima` | Produto marcado como Matéria-Prima | — | · Composição (habilitar uso como insumo) |
| `ProdutoInativado` | Um produto é inativado | — | · Vendas/Suprimentos (impedir novos itens); · Composição (sinalizar receitas que usam o insumo) |
| `ProdutoReativado` | Um produto é reativado | — | · (liberação simétrica) |

> Payload de `ProdutoCadastrado`: `IdProduto`, `Codigo`, `Descricao`. Demais: `IdProduto`. Também é um contexto upstream de identidade.

### Contexto: Suprimentos (Compras)

| Evento | Emitido quando | Mesmo contexto | Outro contexto |
|---|---|---|---|
| `PedidoDeCompraCriado` | Pedido de compra é criado (edição) | · Auditoria | — |
| `PedidoDeCompraAprovado` | Pedido é aprovado | — | · (rota alternativa de título a pagar — **descartada** a favor da efetivação) |
| `PedidoDeCompraEfetivado` | Entrada é efetivada (concluída) | — | ◆ Estoque dá **entrada por compra** (`EntradaPorCompraHandler`); ◆ Financeiro emite **título a pagar** (`GeracaoDeTituloAPagarHandler`) — regra adotada |
| `PedidoDeCompraCancelado` | Pedido é cancelado | — | · Financeiro (cancelar título já gerado, se houver) |

> Payloads: `PedidoDeCompraAprovado` → `IdPedido`, `IdFornecedor`, `ValorTotal`. `PedidoDeCompraEfetivado` → o anterior **+** `Itens[]` (idProduto, quantidade, custoUnitário) — é o payload que o Estoque precisa para dar entrada item a item.
>
> **Decisão de negócio (seção 2 dos requisitos):** "gerar obrigações a pagar a partir da entrada **e/ou** pedido confirmado (conforme regra adotada)". Ou seja, o Financeiro assina **`PedidoDeCompraAprovado`** OU **`PedidoDeCompraEfetivado`**, não os dois. Recomendação: gerar na **efetivação** (a obrigação se firma quando a entrada é concluída).

### Contexto: Estoque (hub)

| Evento | Emitido quando | Mesmo contexto | Outro contexto |
|---|---|---|---|
| `SaldoDeEstoqueCriado` | Primeiro saldo de um produto é criado | — | · BI |
| `SaldoDeEstoqueMovimentado` | Qualquer entrada/saída/ajuste de saldo | · **Alerta de estoque mínimo** (melhor exemplo de handler intra-contexto) | · BI / relatórios / previsão de demanda |

> Payload de `SaldoDeEstoqueMovimentado`: `IdSaldo`, `IdProduto`, `Tipo`, `Sentido`, `Quantidade`, `SaldoResultante`. O Estoque é o maior **consumidor** do sistema, mas também publica — e o alerta de estoque mínimo é o exemplo canônico de Domain Event tratado **dentro do próprio contexto**.

### Contexto: Produção (núcleo)

| Evento | Emitido quando | Mesmo contexto | Outro contexto |
|---|---|---|---|
| `OrdemDeProducaoCriada` | Ordem é criada | · Auditoria | — |
| `OrdemDeProducaoConfirmada` | Ordem é confirmada/iniciada | — | · Estoque (reservar insumos — *se o modelo passar a reservar*) |
| `OrdemDeProducaoConcluida` | Ordem é concluída | — | ◆ Estoque dá **saída por produção** (baixa das MP) **+** **entrada por produção** (produto acabado) |
| `OrdemDeProducaoCancelada` | Ordem é cancelada | — | · Estoque (estornar reservas — *se existissem*) |

> Payload de `OrdemDeProducaoConcluida`: `IdOrdem`, `IdProdutoFabricado`, `QuantidadeProduzida`, `InsumosConsumidos[]` — carrega tudo que o Estoque precisa para gerar as movimentações com referência à ordem (seção 5.3).

### Subdomínio: Composição (dentro de Produção)

| Evento | Emitido quando | Mesmo contexto (Produção) | Outro contexto |
|---|---|---|---|
| `ComposicaoDeProdutoCriada` | Nova versão de receita é definida | · Auditoria de versões | — |
| `ComposicaoDeProdutoAtivada` | Uma versão é ativada | ◆ Garantir **unicidade da receita ativa** (desativar a versão anterior) — implementado no handler `ManipuladorUnicidadeDeReceitaAtiva`; · sinalizar ordens em aberto que usam versão antiga | — |
| `ComposicaoDeProdutoInativada` | Uma versão é inativada | · Auditoria | — |

> Payload: `IdComposicao`, `IdProdutoFabricado`, `Versao`. Exemplo de eventos **intra-contexto** (produtor e consumidor no mesmo bounded context) — bom contraponto didático aos eventos de integração.

### Contexto: Vendas

| Evento | Emitido quando | Mesmo contexto | Outro contexto |
|---|---|---|---|
| `PedidoDeVendaCriado` | Pedido de venda é criado (edição) | · Auditoria | — |
| `PedidoDeVendaAprovado` | Pedido é aprovado (valores congelados) | — | ◆ Estoque dá **saída por venda** (baixa); ○ Financeiro emite **título a receber** (regra simétrica à compra) |
| `PedidoDeVendaConcluido` | Pedido é concluído | — | · Expedição / faturamento |
| `PedidoDeVendaCancelado` | Pedido é cancelado com motivo | — | · Estoque (estornar baixa via entrada compensatória, se já aprovado); · Financeiro (cancelar título a receber) |

> Payload de `PedidoDeVendaAprovado`: `IdPedido`, `IdCliente`, `ValorTotal`, `Itens[]` (idProduto, quantidade). A baixa em estoque é ◆ requerida (seção 6.3); o título a receber é ○ recomendado por simetria (os requisitos citam explicitamente só o "a pagar" da compra).

### Contexto: Financeiro

| Evento | Emitido quando | Mesmo contexto | Outro contexto |
|---|---|---|---|
| `TituloEmitido` | Um título (a pagar/receber) é emitido | · Auditoria | — |
| `TituloBaixado` | Uma baixa parcial/total é registrada | · Recalcular posição financeira do parceiro | — |
| `TituloLiquidado` | Saldo devedor é zerado | — | · Vendas/Suprimentos (marcar pedido como quitado); · atualizar crédito do parceiro |
| `TituloCancelado` | Um título é cancelado | · Auditoria | — |

> Payloads: `TituloEmitido` → `IdTitulo`, `Tipo`, `IdParceiro`, `ValorOriginal`. `TituloBaixado` → `IdTitulo`, `ValorBaixa`, `ValorBaixadoAcumulado`, `SaldoDevedor`. Contexto **downstream** — consome muito (de Compras e Vendas), publica pouco para fora.

---

## Matriz Publicador → Assinante (apenas eventos de integração)

Esta é a visão macro do acoplamento **entre contextos** — o que o dispatcher de eventos precisa rotear.

| Publicador | Evento | Assinante (outro contexto) | Grau |
|---|---|---|---|
| Suprimentos | `PedidoDeCompraEfetivado` | **Estoque** — entrada por compra | ◆ |
| Suprimentos | `PedidoDeCompraEfetivado` | **Financeiro** — título a pagar | ◆ (implementado) |
| Suprimentos | `PedidoDeCompraCancelado` | Financeiro — cancelar título | · |
| Produção | `OrdemDeProducaoConcluida` | **Estoque** — saída de MP + entrada do acabado | ◆ |
| Vendas | `PedidoDeVendaAprovado` | **Estoque** — saída por venda | ◆ |
| Vendas | `PedidoDeVendaAprovado` | **Financeiro** — título a receber | ○ |
| Vendas | `PedidoDeVendaCancelado` | Estoque — estorno; Financeiro — cancelar título | · |
| Catálogo | `Produto*` | Estoque / Vendas / Composição | · |
| Parceiros | `*Inativado` | Vendas / Suprimentos / Financeiro | · |
| Estoque | `SaldoDeEstoqueMovimentado` | BI / previsão de demanda | · |

**Leitura-chave:** o **Estoque** é o assinante de 3 eventos ◆ requeridos (compra, produção, venda) — é o hub. O **Financeiro** assina 2 (título a pagar na compra, ◆; título a receber na venda, ○ por simetria) — ambos **implementados**. Isso confirma o desenho do mapa de contexto: Estoque e Financeiro são **downstream**; Parceiros e Catálogo são **upstream** de identidade.

---

## Fluxos de negócio (Event Storming textual)

### 1. Ciclo de Compra (a peça que demonstra o fan-out)

```
[Comando] EfetivarPedidoDeCompra
        │
        ▼
(PedidoDeCompra) ──emite──► «PedidoDeCompraEfetivado»
        │
        ├──► [Estoque]    handler: registra ENTRADA por compra (1 movimentação por item)   ◆
        └──► [Financeiro] handler: emite TÍTULO A PAGAR (valor total, vencimento)           ○
```
Um evento, **dois contextos reagindo de forma independente** — Suprimentos não conhece nenhum dos dois.

### 2. Ciclo de Venda

```
[Comando] AprovarPedidoDeVenda
        │
        ▼
(PedidoDeVenda) ──emite──► «PedidoDeVendaAprovado»
        │
        ├──► [Estoque]    handler: registra SAÍDA por venda (valida saldo + baixa)   ◆
        └──► [Financeiro] handler: emite TÍTULO A RECEBER                            ○
```

### 3. Ciclo de Produção

```
[Comando] ConcluirOrdemDeProducao
        │
        ▼
(OrdemDeProducao) ──emite──► «OrdemDeProducaoConcluida»
        │
        └──► [Estoque] handler:
                 • SAÍDA por produção para cada matéria-prima consumida
                 • ENTRADA por produção do produto acabado                          ◆
```

### 4. Ativação de Receita (intra-contexto — Domain Event puro)

```
[Comando] AtivarComposicaoDeProduto
        │
        ▼
(ComposicaoDeProduto) ──emite──► «ComposicaoDeProdutoAtivada»
        │
        └──► [Produção/Composição] handler: desativa a versão anteriormente ativa
             (garante a invariante "apenas uma receita ativa por produto")          ○
```

---

## Pontos ainda em aberto

**Ainda em aberto (evolução):**

3. **Estorno no cancelamento** — cancelar um pedido/ordem **já aprovado/concluído** (com estoque baixado e/ou título gerado) exigiria movimentos compensatórios (entrada de estorno no Estoque, cancelamento do título no Financeiro). Hoje fora do escopo; documentado como evolução.
4. **Idempotência do assinante** — a entrega da Outbox é *at-least-once*, mas os handlers ainda não têm chave de idempotência; uma reentrega pode duplicar efeito. Ponto de evolução conhecido.

---