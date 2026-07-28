### 1.1 Clientes

- Cadastrar cliente (PF/PJ) com dados básicos (documento, nome/razão social, contatos, endereço).
    
- Editar cliente.
    
- Inativar/reativar cliente.
    
- Consultar/listar clientes com filtros (nome, documento, status).
    
- Validar unicidade de documento (CPF/CNPJ).

### 1.2 Fornecedores

- Cadastrar fornecedor (PF/PJ) com dados básicos.
    
- Editar fornecedor.
    
- Inativar/reativar fornecedor.
    
- Consultar/listar fornecedores com filtros.
    
- Validar unicidade de documento (CPF/CNPJ).

### 1.3 Produtos

- Cadastrar produto com código, descrição, unidade de medida e status.
    
- Editar produto.
    
- Inativar/reativar produto.
    
- Consultar/listar produtos com filtros.
    
- Classificar produto como  **Fabricado** (para habilitar produção/composição).
	
- Classificar produto como  **Materia Prima** (para compor um produto composto).
### 2) Entrada de Produtos

- Criar pedido de compra para um fornecedor.
    
- Adicionar/remover itens (produtos, quantidade, custo).
    
- Calcular total do pedido.
    
- Status da Entrada: edição, aprovada, cancelada, concluida.
    
- Consultar/listar Entradas de compra com filtros (fornecedor, status, período).
    
- Efetivar entrada gerando movimentações de estoque.
    
- Gerar obrigações financeiras a pagar (títulos) a partir da entrada e/ou pedido confirmado (conforme regra adotada).
### 3) Estoque e Movimentações

- Manter saldo de estoque por produto controlado.
    
- Registrar movimentações:
    
    - Entrada por compra
        
    - Saída por venda
        
    - Saída por produção (consumo de insumos)
        
    - Entrada por produção (produto acabado)
        
    - Ajuste manual (inventário/correção)
        
- Consultar saldo atual por item.
    
- Consultar extrato de movimentações por item/período/origem.
    
- Regra operacional: impedir saída se não houver saldo (ou permitir negativo via configuração do sistema).
### 4.1 Definir Composição do Produto

- Criar composição vinculada a um produto classificado como **Fabricado**.
    
- Informar lista de produtos **matéria prima** e quantidades necessárias para produzir **1 unidade** do produto.
    
- Impedir repetição do mesmo insumo na receita.
    
- Validar quantidades > 0.
### 4.2 Gestão de Versões e Ativação de uma Composição de produto

- Ativar/inativar composição.
    
- Alterar composição criando nova versão (mantendo histórico).
    
- Garantir que exista **uma receita ativa** para permitir produção.
### 5.1 Ordem de Produção

- Criar ordem de produção para um produto fabricado, informando quantidade a produzir.
    
- Calcular automaticamente a necessidade total de matéria prima (com base na composição ativa).
    

### 5.2 Validação de Matéria Prima

- Validar disponibilidade de estoque de todos os produtos antes de confirmar/iniciar produção.
    
- Informar claramente quais produtos estão insuficientes quando houver falta.

### 5.3 Execução e Finalização

- Confirmar/iniciar ordem de produção (mudança de status).
    
- Concluir ordem de produção:
    
    - dar baixa das matérias primas consumidas (saída de estoque)
        
    - dar entrada do produto acabado (entrada de estoque)
        
    - registrar movimentações com referência à ordem de produção
        
- Cancelar ordem de produção, respeitando regras do status (ex.: não cancelar se já concluída; se confirmada pode exigir estorno de reservas, se existir).
### 6.1 Emissão de Pedido de Venda

- Criar pedido de venda para um cliente.
    
- Adicionar/remover itens (produto, quantidade, preço unitário).
    
- Calcular total do pedido.
    
- Aplicar descontos (por item e/ou por pedido, conforme regra adotada).
    
- Gerar número do pedido (sequencial).

### 6.2 Ciclo de Vida do Pedido

- Status do pedido: edição, aprovado, cancelado, concluído.
    
- Confirmar pedido congelando valores e condições.
    
- Cancelar pedido com motivo.
    
- Consultar/listar pedidos com filtros (cliente, status, período).
    

### 6.3 Integração com Estoque

- Ao aprovar uma venda:
    
    - validar disponibilidade de estoque do produto
        
    - dar baixa no estoque (saída)