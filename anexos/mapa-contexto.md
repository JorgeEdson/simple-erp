# Mapa de Contexto — Simple ERP

## 1. Domínio e destilação de subdomínios

Antes dos contextos, o *strategic design* pergunta **onde está a vantagem competitiva** do negócio. No Simple ERP (didático, mas modelado como um ERP de manufatura), a destilação fica assim:

| Subdomínio | Tipo | Por quê | Bounded context |
|---|---|---|---|
| **Produção** (ordens + composição) | **Core** | É o coração do ERP de manufatura: transformar insumos em produto acabado a partir de uma receita versionada. A regra mais rica do sistema vive aqui. | `Producao`, `Producao/Composicao` |
| **Vendas** | **Core** | Onde o valor é capturado; orquestra a saída de estoque e o faturamento. | `Vendas` |
| **Suprimentos** (Compras) | **Supporting** | Necessário para abastecer, mas segue processo padrão de pedido→aprovação→efetivação. | `Suprimentos` |
| **Estoque** | **Supporting** (com papel de *hub*) | Sustenta produção e vendas; concentra a integração, mas não é diferencial competitivo por si só. | `Estoque` |
| **Financeiro** | **Supporting** | Consequência contábil das operações; regras de título a pagar/receber são convencionais. | `Financeiro` |
| **Parceiros Comerciais** | **Generic** | Cadastro de clientes/fornecedores com documento validado — problema resolvido, sem diferenciação. | `ParceirosComerciais` |
| **Catálogo de Produtos** | **Generic** | Identidade e classificação de produto; também um problema conhecido. | `CatalogoDeProdutos` |

> **Leitura estratégica:** o esforço de modelagem mais rico deve se concentrar em **Produção** e **Vendas** (Core). Parceiros e Catálogo (Generic) poderiam, num sistema real, ser terceirizados ou comprados prontos — aqui são mantidos internos por serem didáticos e leves.

---

## 2. Os 8 bounded contexts

Cada contexto = um módulo em `simple-erp.Core/Modulos`, um **schema próprio no PostgreSQL** (`Persistencia/Esquemas.cs`) e uma fronteira transacional (**um agregado por transação**).

| # | Bounded Context | Papel no mapa | Agregado(s) raiz | Casos de uso | Eventos | Schema |
|---|---|---|---|---|---|---|
| 1 | **ParceirosComerciais** | Upstream — *identidade* | `Cliente`, `Fornecedor` (`ParceiroComercial`) | 12 | 6 | `parceiros` |
| 2 | **CatalogoDeProdutos** | Upstream — *identidade* | `Produto` | 8 | 5 | `catalogo` |
| 3 | **Suprimentos** (Compras) | Transacional (Supporting) | `PedidoDeCompra` | 8 | 4 | `suprimentos` |
| 4 | **Estoque** | **Hub** de integração | `SaldoDeEstoque`, `MovimentacaoDeEstoque` | 3 | 2 | `estoque` |
| 5 | **Producao/Composicao** | Engenharia / suporte | `ComposicaoDeProduto` | 5 | 3 | `producao` |
| 6 | **Producao** | Core — núcleo transacional | `OrdemDeProducao` | 6 | 4 | `producao` |
| 7 | **Vendas** | Core — transacional | `PedidoDeVenda` | 9 | 4 | `vendas` |
| 8 | **Financeiro** | Downstream — *consequência* | `Titulo` (a pagar / a receber) | 6 | 4 | `financeiro` |

Fora dos módulos de negócio há o **schema `eventos`**, que hospeda a tabela `eventos.outbox` — o mecanismo de integração, não um bounded context.

### Linguagem ubíqua por contexto (destaques)

- **ParceirosComerciais:** *Cliente*, *Fornecedor*, *Documento* (Cpf/Cnpj), *Endereço*. Unicidade de documento é **por tipo de parceiro** — o mesmo CNPJ pode ser cliente e fornecedor.
- **CatalogoDeProdutos:** *Produto*, *CódigoProduto*, *Classificação* (Fabricado / MatériaPrima), *UnidadeDeMedida*.
- **Suprimentos:** *PedidoDeCompra*, *ItemDePedidoDeCompra*, *StatusPedidoDeCompra* (máquina de estados), *Dinheiro*.
- **Estoque:** *SaldoDeEstoque*, *MovimentaçãoDeEstoque*, *TipoDeMovimentacao*, *SentidoDaMovimentacao*, *OrigemDaMovimentacao*, *Quantidade*.
- **Producao/Composicao:** *ComposiçãoDeProduto* (receita), *ItemDeComposicao*, *NecessidadeCalculada* — com **versionamento** e **uma única receita ativa** por produto.
- **Producao:** *OrdemDeProducao*, *NecessidadeDeMateriaPrima*, *StatusOrdemDeProducao*.
- **Vendas:** *PedidoDeVenda*, *ItemDePedidoDeVenda*, *MotivoCancelamento*, *StatusPedidoDeVenda*.
- **Financeiro:** *Título*, *TipoDeTitulo* (a pagar / a receber), *OrigemDoTitulo*, *BaixaDoTitulo*, *StatusTitulo*, *Dinheiro*.

---

## 3. Padrões de Context Mapping (Strategic DDD)

Relações entre contextos, no vocabulário clássico de Evans/Vernon. Notação: **U** = upstream (a montante, influencia), **D** = downstream (a jusante, depende).

| Relação (U → D) | Padrão | Como se manifesta no código |
|---|---|---|
| CatalogoDeProdutos **(U)** → Estoque, Suprimentos, Producao, Vendas **(D)** | **Customer/Supplier** + **Conformist** | Os contratantes referenciam `Produto` apenas por `Id`. Não há tradução: aceitam a identidade do Catálogo como está. |
| ParceirosComerciais **(U)** → Suprimentos, Vendas **(D)** | **Customer/Supplier** + **Conformist** | Fornecedor/Cliente entram no pedido apenas por `Id`. |
| Suprimentos **(U)** → Estoque, Financeiro **(D)** | **Publisher/Subscriber** via **Domain Event** | `PedidoDeCompraEfetivado` é publicado; Estoque e Financeiro reagem sem que Suprimentos os conheça. |
| Vendas **(U)** → Estoque, Financeiro **(D)** | **Publisher/Subscriber** via **Domain Event** | `PedidoDeVendaAprovado` dispara saída de estoque e título a receber. |
| Producao **(U)** → Estoque **(D)** | **Publisher/Subscriber** via **Domain Event** | `OrdemDeProducaoConcluida` gera baixa de insumos + entrada do acabado. |
| Producao/Composicao **(U)** → Producao **(D)** | **Shared Kernel** parcial (intra-família Produção) | A ordem calcula necessidade a partir da **receita ativa**; convivem no mesmo schema `producao`. |
| Todos os publicadores → `eventos.outbox` → todos os assinantes | **Open Host Service** + **Published Language** | O **evento de domínio serializado** é a linguagem publicada; a Outbox é o serviço aberto de entrega *at-least-once*. |

### Anti-Corruption Layer (ACL) — onde está (e onde não está)

O papel de **ACL** é exercido pelos **handlers** do contexto downstream: eles recebem o evento do publicador e o **traduzem** para o caso de uso local (ex.: `PedidoDeCompraEfetivado` → `RegistrarMovimentacaoDeEstoqueEntrada`). O contexto que reage nunca importa o agregado alheio — só o **payload do evento** e o **Id**. Isso mantém cada modelo protegido de mudanças no vizinho.

---

## 4. Integração por eventos — reações implementadas

Todas as reações entre contextos passam pela **Transactional Outbox** (`eventos.outbox`). Nenhuma usa referência direta entre módulos; o handler downstream funciona como **ACL** e traduz o evento para um caso de uso local.

| Evento publicado | Contexto publicador | Contexto que reage | Handler (ACL) | Efeito |
|---|---|---|---|---|
| `PedidoDeCompraEfetivado` | Suprimentos | **Estoque** | `EntradaPorCompraHandler` | Entrada por compra — uma movimentação por item |
| `PedidoDeCompraEfetivado` | Suprimentos | **Financeiro** | `GeracaoDeTituloAPagarHandler` | Título a pagar (vencimento padrão: 30 dias) |
| `PedidoDeVendaAprovado` | Vendas | **Estoque** | `SaidaPorVendaHandler` | Saída por venda (valida saldo e baixa) |
| `PedidoDeVendaAprovado` | Vendas | **Financeiro** | `GeracaoDeTituloAReceberHandler` | Título a receber (vencimento padrão: 30 dias) |
| `OrdemDeProducaoConcluida` | Producao | **Estoque** | `MovimentacoesPorProducaoHandler` | Saída das matérias-primas **+** entrada do produto acabado |
| `ComposicaoDeProdutoAtivada` | Producao/Composicao | **Producao/Composicao** (intra-contexto) | `ManipuladorUnicidadeDeReceitaAtiva` | Desativa a versão anterior da receita ativa |

> **Fan-out — a demonstração central do projeto:** `PedidoDeCompraEfetivado` dispara reação em **dois contextos que Suprimentos não conhece** (Estoque e Financeiro). O publicador não muda quando um novo assinante entra — é o desacoplamento estrutural do padrão Publisher/Subscriber sobre Published Language.

### Por que a fronteira aparece também no banco

O `Estoque` é o único contexto que reage a **três** publicadores distintos (Compras, Vendas, Produção) — por isso é o **hub**. Ainda assim, cada reação chega como um evento independente e é confirmada em **sua própria transação** (1 escopo + 1 transação por evento), preservando "uma transação = um agregado".

---

## 5. Fluxo de entrega (consistência eventual)

```
[Contexto A] Use Case → Agregado emite Evento de Domínio
        │  (mesma transação, via interceptor de SaveChanges do EF Core)
        ▼
[PostgreSQL] agregado A + linha em eventos.outbox  ← atômico
        │
[BackgroundService] lê o lote pendente (polling, lote de 20, 5s)
        │  1 escopo + 1 transação por evento
        ▼
[Dispatcher de Eventos] reidrata o evento → resolve handlers
        │
        ├──▶ [Contexto B] Handler (ACL) → Use Case local → agregado B + marca "processado"  ← atômico
        └──▶ [Contexto C] Handler (ACL) → Use Case local → agregado C + marca "processado"  ← atômico
```

Garantias e trade-offs desenhados:

- **Atomicidade dado+evento:** o agregado e a linha da Outbox são gravados na **mesma transação**. Nunca "gravou o pedido mas perdeu o efeito".
- **Isolamento entre eventos:** falha em um evento não contamina o próximo do lote.
- **Poison message:** teto de **5 tentativas**; a linha para de ser retomada e fica com o último erro registrado.
- **Entrega *at-least-once*:** o mesmo evento pode ser reentregue — por contrato, handlers **deveriam** ser idempotentes (hoje, por ser didático, não são por construção).

---

## 6. Regras de convivência entre contextos (governança do mapa)

Estas são as invariantes de fronteira que mantêm o mapa saudável e devem guiar qualquer evolução:

1. **Referência entre contextos só por Id.** Nenhum módulo importa entidade/VO de agregado de outro. Produto e Parceiro cruzam a fronteira apenas como identificador.
2. **Core sem framework.** `simple-erp.Core.csproj` não tem `PackageReference`. O modelo estratégico não sabe onde é persistido nem como é exposto.
3. **Uma transação = um agregado.** Efeito colateral em outro contexto vem **depois**, em transação separada, via Outbox.
4. **O evento é a Published Language.** Mudança incompatível no payload de um evento é *breaking change* de contrato — versionar, não quebrar.
5. **Quem reage é problema do assinante.** O publicador registra o que aconteceu; adicionar/remover um assinante não altera o publicador.
6. **Objetos de valor não são compartilhados por conveniência.** Cada contexto define os seus (`Quantidade`, `Dinheiro`) — evita acoplamento acidental via Shared Kernel.

---

## Apêndice A — Catálogo de eventos

O catálogo completo — os **32 eventos de domínio** dos 8 contextos, com **quando cada um é publicado, o payload que carrega e quem reage** — é mantido em documento próprio: **[`mapa-eventos.md`](mapa-eventos.md)** (Event Catalog).

A separação é proposital: este mapa é **estratégico** (contextos e como se relacionam); o `mapa-eventos.md` é **tático/operacional** (a ficha de cada evento). Evita duplicação e fonte dupla de verdade.

Para a visão de integração — que fica aqui — basta lembrar: dos 32 eventos, apenas **4 disparam reação entre contextos hoje** (seção 5 e Apêndice B). Os demais são registrados na Outbox e ficam **auditáveis no banco**, prontos para ganharem assinantes sem alterar o publicador.

## Apêndice B — Matriz publicador → assinante

| ↓ publica / reage → | Estoque | Financeiro | Producao/Composicao |
|---|:---:|:---:|:---:|
| **Suprimentos** (`PedidoDeCompraEfetivado`) | ✅ entrada por compra | ✅ título a pagar | — |
| **Vendas** (`PedidoDeVendaAprovado`) | ✅ saída por venda | ✅ título a receber | — |
| **Producao** (`OrdemDeProducaoConcluida`) | ✅ baixa insumos + entrada acabado | — | — |
| **Producao/Composicao** (`ComposicaoDeProdutoAtivada`) | — | — | ✅ unicidade da receita ativa |