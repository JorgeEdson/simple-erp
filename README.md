# Simple ERP — Monólito Modular com DDD, Casos de Uso e Transactional Outbox

> ⚠️ **Projeto didático.** Este repositório é uma **prova de conceito**, criada para **ensinar como estruturar um sistema de negócio com modelagem de domínio rica, contextos delimitados e comunicação entre módulos por eventos de domínio** (Result pattern, Unit of Work, Domain Events, Transactional Outbox e entrega assíncrona *at-least-once*). Ele **não** é uma referência pronta para produção — várias simplificações foram feitas de propósito para manter o foco no aprendizado (ver [Limitações](#limitações-por-ser-didático)).

Demonstra, ponta a ponta, como um ERP simples pode ser organizado em **módulos que representam bounded contexts**, mantendo um **Core isolado de frameworks**, e como um módulo reage ao que acontece em outro **sem conhecê-lo** — via eventos capturados na mesma transação do agregado e despachados depois, fora da requisição.

---

## Visão geral do fluxo

```
Cliente/Swagger → [API] → [Dispatcher] → [Use Case] → [Agregado emite evento]
                                                            │
                              (mesma transação, via interceptor do EF Core)
                                                            ▼
                                              [PostgreSQL: eventos.outbox]
                                                            │
                                   [BackgroundService do Outbox] lê o lote pendente
                                                            │
                          1 escopo + 1 transação por evento → [Dispatcher de Eventos]
                                                            │
                            ┌───────────────────────────────┼───────────────────────────┐
                            ▼                               ▼                           ▼
                       [Estoque]                      [Financeiro]                 [Produção]
                  entrada/saída de saldo          título a pagar/receber        unicidade da receita
```

1. A **API** recebe a requisição e o **Dispatcher** resolve o **caso de uso** correspondente (mediator próprio, sem MediatR).
2. O **caso de uso** carrega o agregado, aplica a regra e o agregado **emite eventos de domínio**.
3. Um **interceptor de `SaveChanges`** captura esses eventos e grava as linhas em `eventos.outbox` **na mesma transação** que persiste o agregado — atomicidade entre o dado e o evento.
4. Um **BackgroundService** lê a caixa de saída em lotes, **reidrata** cada evento e o entrega aos seus manipuladores, **um escopo e uma transação por evento**.
5. Os **handlers** de outros módulos reagem: Estoque movimenta saldo, Financeiro emite título, Produção garante a unicidade da receita ativa.

Os documentos estratégicos que embasam o desenho — **mapa de contextos** e **catálogo de eventos** — estão em [Documentação complementar](#documentação-complementar).

---

## Padrões e mecanismos aplicados

| Padrão / mecanismo | Onde | O que resolve |
|---|---|---|
| **Monólito modular por bounded context** | `simple-erp.Core/Modulos` | Cada módulo tem entidades, VOs, eventos, interfaces e casos de uso próprios; referências entre módulos só por **Id**, nunca por objeto de domínio. |
| **Core isolado de frameworks** | `simple-erp.Core.csproj` | O `.csproj` do Core **não tem nenhum `PackageReference`**: nem EF, nem ASP.NET, nem MediatR. A regra de negócio não sabe onde é persistida nem como é exposta. |
| **Domínio + Aplicação no mesmo núcleo** | `Core` (`Entidades` + `UseCases`) | Casos de uso conversam diretamente com o domínio, sem camada de tradução artificial. |
| **Result pattern** | `Compartilhado/Base/Resultado.cs` | Falha de negócio é **valor de retorno** (`Resultado<T>`), não exceção — o fluxo esperado fica explícito na assinatura. |
| **Objetos de Valor** | `*/ObjetosDeValor` | `Cpf`, `Cnpj`, `Email`, `Dinheiro`, `Quantidade`, `CodigoProduto`… validam na construção: um objeto inválido não chega a existir. |
| **Unit of Work + transação explícita** | `IUnitOfWork` / `UnitOfWork` | Uma transação por operação; os repositórios compartilham o mesmo `DbContext`. |
| **Mediator próprio (`IDispatcher`)** | `Api/Mediador/Dispatcher.cs` | O controller depende só do `IDispatcher`; o use case é resolvido pelo par entrada/saída, sem biblioteca externa. |
| **Domain Events** | `Compartilhado/Base/EventoDeDominio.cs` | O agregado registra o que aconteceu; quem reage é problema de outro módulo. |
| **Transactional Outbox** | `Interceptadores/CapturaDeEventosParaOutboxInterceptor.cs` + tabela `eventos.outbox` | Elimina a janela "gravou o pedido mas perdeu o efeito colateral": agregado e evento são salvos juntos ou nenhum dos dois. |
| **Consistência eventual entre contextos** | `ProcessadorDeEventosPendentes` | Respeita "uma transação = um agregado": o efeito em outro módulo vem depois, em transação separada. |
| **1 escopo + 1 transação por evento** | `ProcessadorDeEventosPendentes` | Falha em um evento não contamina o próximo do lote; o efeito do handler e a marcação de "processado" são confirmados juntos. |
| **Teto de tentativas (poison message)** | `MaximoDeTentativas = 5` | Um evento defeituoso para de ser retomado e deixa de travar a fila atrás de si — a linha fica pendente, com o último erro registrado, para análise. |
| **Entrega *at-least-once*** | Outbox | O evento pode ser reentregue; handlers precisam ser idempotentes. É o contrato honesto de sistemas distribuídos, não uma falha da implementação. |
| **Registro histórico dos eventos** | Tabela `eventos.outbox` | O catálogo de eventos deixa de ser só documentação: fica auditável no banco. |
| **Schema por módulo** | `Persistencia/Esquemas.cs` | `parceiros`, `catalogo`, `suprimentos`, `estoque`, `producao`, `vendas`, `financeiro`, `eventos` — a fronteira do contexto aparece também no banco. |
| **EF Core Migrations aplicadas no start** | `Configuracao/MigracaoExtensions.cs` | O mesmo caminho de evolução do schema vale para dev, container e produção (sem `EnsureCreated`). |
| **Carga inicial idempotente** | `Configuracao/Seed` | Base de demonstração só em `Development`, sem duplicar em reinícios. |

---

## Projetos / containers

| # | Projeto | Tipo | Responsabilidade |
|---|---|---|---|
| 1 | `simple-erp.Core` | Class library (.NET 10) | **Domínio + Aplicação.** Entidades, objetos de valor, eventos, handlers, interfaces de repositório e casos de uso, organizados por módulo. **Zero dependências externas.** |
| 2 | `simple-erp.Infraestrutura` | Class library (.NET 10) | EF Core 10 + Npgsql: `DbContext`, configurações, conversores de VO, repositórios, Unit of Work, interceptor e processador do Outbox, migrations. |
| 3 | `simple-erp.Api` | ASP.NET Core 10 (REST) | Controllers, Swagger, mediator, `LogService`, carga inicial e o `BackgroundService` que processa a caixa de saída. |
| 4 | `simple-erp.Testes` | xUnit (.NET 10) | ~73 arquivos de teste: domínio, casos de uso e repositórios (estes com **Postgres real** via Testcontainers). |
| — | PostgreSQL 17 | Infra (container) | Banco `simple_erp`, com um schema por módulo. |
| — | pgAdmin 4 | Infra (container) | Inspeção do banco, já com a conexão registrada. |

---

## Módulos (bounded contexts)

| Módulo | Papel no mapa | Agregado raiz | Casos de uso | Eventos |
|---|---|---|---|---|
| **ParceirosComerciais** | Upstream (identidade) | `Cliente`, `Fornecedor` | 12 | 6 |
| **CatalogoDeProdutos** | Upstream (identidade) | `Produto` | 8 | 5 |
| **Suprimentos** (Compras) | Transacional | `PedidoDeCompra` | 8 | 4 |
| **Estoque** | **Hub** de integração | `SaldoDeEstoque`, `MovimentacaoDeEstoque` | 3 | 2 |
| **Producao/Composicao** (BOM) | Engenharia / suporte | `ComposicaoDeProduto` | 5 | 3 |
| **Producao** | Transacional (núcleo) | `OrdemDeProducao` | 6 | 4 |
| **Vendas** | Transacional | `PedidoDeVenda` | 9 | 4 |
| **Financeiro** | Downstream | `Titulo` (a pagar / a receber) | 6 | 4 |

**Reações entre módulos hoje implementadas** (todas via Outbox, nenhuma com referência direta entre módulos):

| Evento publicado | Módulo que reage | Handler | Efeito |
|---|---|---|---|
| `PedidoDeCompraEfetivado` | Estoque | `EntradaPorCompraHandler` | Entrada por compra, uma movimentação por item |
| `PedidoDeCompraEfetivado` | Financeiro | `GeracaoDeTituloAPagarHandler` | Título a pagar (vencimento padrão: 30 dias) |
| `PedidoDeVendaAprovado` | Estoque | `SaidaPorVendaHandler` | Saída por venda (valida saldo e baixa) |
| `PedidoDeVendaAprovado` | Financeiro | `GeracaoDeTituloAReceberHandler` | Título a receber (vencimento padrão: 30 dias) |
| `OrdemDeProducaoConcluida` | Estoque | `MovimentacoesPorProducaoHandler` | Saída das matérias-primas **+** entrada do produto acabado |
| `ComposicaoDeProdutoAtivada` | Produção (intra-contexto) | `ManipuladorUnicidadeDeReceitaAtiva` | Desativa a versão anterior da receita |

> Repare no **fan-out**: `PedidoDeCompraEfetivado` dispara reações em **dois módulos que Suprimentos não conhece**. É a demonstração central do projeto.

---

## Requisitos funcionais (RF)

- **RF01 — Parceiros comerciais.** Cadastrar, editar, consultar (por id e paginado com filtros), inativar e reativar **clientes** e **fornecedores**, com CPF/CNPJ validado e único por tipo de parceiro.
- **RF02 — Catálogo de produtos.** Cadastrar, editar, listar, inativar/reativar produtos e **classificá-los** como *Fabricado* ou *Matéria-Prima*.
- **RF03 — Compras.** Criar pedido de compra para um fornecedor, adicionar/remover itens, aprovar, efetivar e cancelar, com cálculo de total e máquina de estados.
- **RF04 — Estoque.** Manter saldo por produto e registrar movimentações tipadas (entrada por compra, saída por venda, saída/entrada por produção, ajuste), consultar saldo e extrato paginado, **impedindo saída sem saldo**.
- **RF05 — Composição de produto (BOM).** Definir a receita de um produto fabricado com **versionamento e histórico**, ativar/inativar versões e garantir **uma única receita ativa** por produto.
- **RF06 — Produção.** Criar ordem de produção, calcular a necessidade de insumos a partir da composição ativa, confirmar, concluir e cancelar conforme o status.
- **RF07 — Vendas.** Criar pedido de venda para um cliente, adicionar/remover itens, aplicar desconto, aprovar, concluir e cancelar (com motivo).
- **RF08 — Financeiro.** Emitir títulos a pagar e a receber, registrar baixas (parciais/total), cancelar e consultar títulos.
- **RF09 — Integração por eventos.** Toda reação entre módulos acontece por **evento de domínio persistido em Outbox** e despachado fora da requisição, com retentativa e teto de tentativas.

> ℹ️ **Estado da API REST:** hoje só os endpoints de **Clientes** e **Fornecedores** estão expostos (`/api/clientes`, `/api/fornecedores`). Os demais módulos estão **completos no Core, na Infraestrutura e nos testes**, mas ainda sem controller — exercitá-los é feito pelos testes automatizados. Ver [Limitações](#limitações-por-ser-didático).

---

## Como rodar

Na raiz do repositório:

```bash
docker compose up --build
```

Isso sobe: **PostgreSQL**, a **API** (que aplica as migrations e, em `Development`, a carga inicial) e o **pgAdmin**.

Endereços padrão:

| Serviço | URL / Porta | Credenciais |
|---|---|---|
| API (Swagger) | http://localhost:8080/swagger | — |
| pgAdmin | http://localhost:5050 | `admin@simpleerp.com` / `admin` |
| PostgreSQL | `localhost:5432` | `simple_erp` / `simple_erp` (db `simple_erp`) |

Para parar:

```bash
docker compose down
```

> Os volumes `simple-erp-pgdata` e `simple-erp-pgadmin` **persistem** entre reinícios. Para começar do zero (banco vazio, migrations e seed reaplicados): `docker compose down -v`.

### Rodando localmente (sem o container da API)

```bash
docker compose up -d postgres
dotnet run --project simple-erp.Api
```

A connection string padrão do `appsettings.json` já aponta para `localhost:5432`.

### Testes

```bash
dotnet test
```

> Os testes de repositório sobem um **PostgreSQL descartável via Testcontainers** — precisam do Docker em execução.

---

## Testando com a collection do Insomnia

O arquivo está em [`collection/simple-erp-parceiros.insomnia.json`](./collection/simple-erp-parceiros.insomnia.json).

1. No Insomnia: **Import → From File** e selecione o arquivo.
2. Use o environment **Local (docker compose)** (já traz `base_url` e os ids de apoio).
3. Requests disponíveis:

   **Clientes** — cadastro (201), documento inválido (400), e-mail inválido (400), documento duplicado (400), obter por id (200/404), listar paginado e com filtros (nome, ativo, cidade), editar (200/404), inativar e reativar.

   **Fornecedores** — o ciclo equivalente, com validação de CNPJ, filtro por documento formatado e por estado.

   **Casos de borda** — cliente com o **mesmo CNPJ** de um fornecedor (permitido: a unicidade é por tipo de parceiro) e paginação (página 1 e 2 com tamanho 1, tamanho acima do limite).

### Demonstração do Outbox (roteiro)

1. Suba o ambiente e observe os logs da API: `Processamento da caixa de saída iniciado (lote de 20, intervalo de 5s).`
2. Cadastre um cliente pela collection ou pelo Swagger.
3. Consulte a tabela `eventos.outbox`: a linha do `ClienteCadastrado` aparece com `processado_em_utc` **nulo**.
4. Aguarde até 5 segundos e consulte de novo: `processado_em_utc` preenchido, e o log `Evento de domínio despachado a partir da caixa de saída.`
5. Para ver o **fan-out completo** (compra → estoque + financeiro), rode os testes do módulo de Suprimentos — o caminho REST desses módulos ainda não existe.

---

## Verificação no banco

```sql
-- Cadastros
SELECT * FROM parceiros.clientes;
SELECT * FROM parceiros.fornecedores;
SELECT * FROM catalogo.produtos;

-- Transacional
SELECT * FROM suprimentos.pedidos_de_compra;
SELECT * FROM vendas.pedidos_de_venda;
SELECT * FROM producao.ordens_de_producao;
SELECT * FROM producao.composicoes_de_produto;

-- Efeitos gerados por evento
SELECT * FROM estoque.saldos;
SELECT * FROM estoque.movimentacoes;
SELECT * FROM financeiro.titulos;

-- Caixa de saída: pendentes, processados e falhas
SELECT nome_do_evento, id_agregado_origem, criado_em_utc, processado_em_utc, tentativas, ultimo_erro
FROM eventos.outbox
ORDER BY id DESC;

-- Só o que ainda não foi despachado
SELECT * FROM eventos.outbox WHERE processado_em_utc IS NULL;

-- Poison messages: estouraram o teto de tentativas
SELECT * FROM eventos.outbox WHERE processado_em_utc IS NULL AND tentativas >= 5;
```

---

## Tecnologias

**Base:** .NET 10 · C# · ASP.NET Core (controllers) · Swashbuckle/Swagger

**Persistência:** PostgreSQL 17 · EF Core 10 · Npgsql · EFCore.NamingConventions (snake_case) · EF Migrations

**Testes:** xUnit · FluentAssertions · NSubstitute · Testcontainers.PostgreSql

**Infra local:** Docker Compose · pgAdmin 4

> Note o que **não** está aqui: nenhuma biblioteca de mediator, de mapeamento ou de validação. O mediator, o Result pattern e a validação nos objetos de valor são do próprio projeto — de propósito, para que o mecanismo fique visível em vez de escondido atrás de um pacote.

---

## Documentação complementar

| Documento | Conteúdo |
|---|---|
| `Requisitos Funcionais - Simple ERP.pdf` | Especificação de origem do domínio. |
| `context-map.md` | **Mapa de contextos**: os 8 bounded contexts, o papel de cada um (upstream / downstream / hub) e as integrações. |
| `mapa-eventos.md` | **Catálogo de eventos**: para cada evento, quem publica, quando, o payload e quem reage — mais a matriz publicador → assinante e os fluxos de Event Storming. |
| `apresentacao-ddd-result-usecases.md` | Material de apresentação sobre DDD, Result pattern e casos de uso. |

> Esses documentos são mantidos **fora do repositório de código**, junto ao material de estudo do projeto.

---

## Limitações (por ser didático)

Estas simplificações são **intencionais** para focar no aprendizado; em produção você trataria cada uma:

- **API parcial:** só Parceiros Comerciais tem controller. Compras, Estoque, Produção, Vendas e Financeiro existem por completo no Core/Infra, mas só são exercitados por testes.
- **Segredos em texto claro** no `docker-compose.yml` e no `appsettings.json` (use secrets / variáveis de ambiente seguras).
- **API sem autenticação/autorização.**
- **Outbox com polling e instância única:** o worker roda dentro do próprio processo da API, sem lock distribuído (`FOR UPDATE SKIP LOCKED`). Com mais de uma réplica, o mesmo evento pode ser processado em paralelo.
- **Sem broker de mensageria:** o despacho é in-process. O passo natural de evolução é publicar o conteúdo do Outbox em RabbitMQ/Kafka em vez de chamar os handlers diretamente.
- **Handlers não são idempotentes por construção:** a entrega é *at-least-once*, mas não há chave de idempotência no lado consumidor — uma reentrega pode duplicar um efeito.
- **Sem retry com atraso (backoff):** as 5 tentativas acontecem na cadência do polling, sem espaçamento crescente.
- **Sem expurgo/retenção da Outbox:** a tabela cresce indefinidamente; falta um job de limpeza das linhas já processadas.
- **Sem reprocessador de poison messages:** a linha que estoura as 5 tentativas fica parada no banco, sem DLQ nem ferramenta de reenvio.
- **Sem métricas e sem tracing distribuído** (o `ILogService` cobre só o logging estruturado).
- **Ids gerados pela aplicação**, com valores fixos no seed — sem estratégia de geração distribuída.
