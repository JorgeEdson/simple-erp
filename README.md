# simple-erp

Uma prova de conceito desenvolvida em **.NET** com foco em **modelagem de domínio rica**, **casos de uso bem definidos**, **organização por contexto delimitado** e **isolamento do Core em relação a frameworks e tecnologias externas**.

O objetivo deste projeto não é apenas simular um ERP simples, mas principalmente servir como laboratório prático para explorar decisões arquiteturais que favoreçam **clareza de negócio**, **coesão**, **manutenibilidade** e **testabilidade**.

---

## Propósito do projeto

O `simple-erp` foi criado como um projeto de estudo e experimentação para validar, na prática, uma forma de estruturar software em que as camadas de **Domínio** e **Aplicação** são tratadas dentro de um único núcleo chamado **Core**.

A ideia central é manter esse núcleo:

- independente de frameworks
- livre de bibliotecas externas
- focado exclusivamente na regra de negócio
- organizado por módulos que representam contextos delimitados do sistema

Com isso, o projeto busca demonstrar que o mais importante não é seguir uma estrutura de pastas rigidamente, mas sim construir um modelo que torne o negócio **explícito**, **coeso** e **sustentável ao longo do tempo**.

---

## O que este projeto busca praticar

Este repositório foi pensado para exercitar conceitos importantes de arquitetura e modelagem de software, como:

- **Orientação a Objetos aplicada ao negócio**
- **Domínio rico**
- **Objetos de Valor**
- **Entidades com comportamento**
- **Casos de Uso como orquestradores de fluxo**
- **Contextos Delimitados**
- **Separação clara de responsabilidades**
- **Baixo acoplamento com infraestrutura**
- **Testes unitários focados em comportamento**
- **Evolução segura do software a partir de um núcleo isolado**

---

## Visão arquitetural

O projeto adota uma abordagem em que **Domínio** e **Aplicação** convivem no mesmo núcleo chamado **Core**.

Essa escolha foi feita para reforçar a ideia de que:

- a regra de negócio deve ser o centro da solução
- os casos de uso devem conversar diretamente com o domínio
- detalhes externos, como ORM, banco de dados, API e frameworks, devem ficar ao redor do núcleo, e não no centro dele

Na prática, isso significa que o projeto foi desenhado para que o Core possa evoluir com o mínimo possível de interferência de detalhes técnicos externos.

---

## Organização por contexto delimitado

A estrutura do projeto é organizada por **módulos**, cada um representando um contexto delimitado do sistema.

Dentro de cada módulo, a organização busca deixar explícitos os principais elementos do modelo, como por exemplo:

- **Entidades**
- **Objetos de Valor**
- **Interfaces**
- **Casos de Uso**
- **Eventos de Domínio**

Essa abordagem ajuda a manter o sistema mais compreensível, especialmente à medida que ele cresce.

---

## Modelagem do Core

O coração do projeto está no `Core`, onde ficam concentradas:

- regras de negócio
- contratos
- comportamentos das entidades
- objetos de valor
- validações
- fluxos de aplicação

Algumas características dessa modelagem:

- entidades expõem comportamento, e não apenas estado
- objetos de valor encapsulam validações e garantem consistência
- casos de uso representam fluxos funcionais do sistema
- interfaces definem contratos sem acoplamento com implementação concreta
- o retorno das operações é tratado por meio de um tipo `Resultado`, padronizando sucesso e falha sem depender de exceptions para fluxo normal de negócio

---

## Módulo atual

Atualmente, o projeto trabalha principalmente o contexto de **Parceiros Comerciais**, incluindo operações relacionadas a:

- **Clientes**
- **Fornecedores**

Entre os fluxos implementados, estão:

- cadastro
- edição
- ativação
- inativação
- obtenção por identificador
- listagem paginada

---

## Casos de uso

Os casos de uso representam a camada de aplicação dentro do Core.

Eles são responsáveis por:

- validar entradas
- orquestrar chamadas ao domínio
- consultar contratos de repositório
- consolidar respostas
- devolver saídas padronizadas

A intenção é deixar cada caso de uso pequeno, objetivo e centrado em um comportamento específico do sistema.

---

## Testes unitários

Um dos pontos mais importantes desta prova de conceito é mostrar como um Core bem isolado facilita a construção de **testes unitários simples e objetivos**.

Como o domínio não depende de banco de dados, ORM ou framework web, os testes conseguem focar no que realmente importa:

- comportamento
- regras de negócio
- fluxos dos casos de uso
- cenários de sucesso e falha

Além disso, o projeto utiliza builders de apoio nos testes para tornar a criação de massa de dados mais simples, legível e reutilizável.

---

## Tecnologias e abordagem

Este projeto foi construído com foco em práticas modernas do ecossistema .NET, mas sem acoplar o núcleo do sistema a tecnologias externas.

### Base principal
- **.NET**
- **C#**

### Testes
- **xUnit**
- **FluentAssertions**
- **NSubstitute**

---

## O que este projeto não pretende ser

Embora o nome sugira um ERP, este repositório **não tem como foco principal entregar um produto completo de mercado**.

O objetivo maior é servir como:

- ambiente de estudo
- prova de conceito arquitetural
- laboratório de modelagem
- espaço para validar padrões de organização e testes

---

## Aprendizados que o projeto reforça

Ao longo do desenvolvimento, o `simple-erp` procura reforçar algumas percepções importantes:

- orientação a objetos vai muito além de organização de pastas
- um domínio bem modelado facilita testes, manutenção e evolução
- casos de uso bem definidos deixam o sistema mais explícito
- isolamento do Core aumenta previsibilidade e segurança na evolução
- boas decisões de modelagem impactam diretamente a sustentabilidade do software

---

## Próximos passos

Entre os próximos avanços naturais do projeto, estão:

- expansão de novos módulos/contextos
- ampliação da cobertura de testes unitários
- implementação de infraestrutura concreta
- exposição dos casos de uso por uma API
- evolução gradual do modelo sem comprometer a clareza do Core

---

## Conclusão

O `simple-erp` é, acima de tudo, uma iniciativa para explorar uma ideia simples, mas poderosa:

> mais importante do que seguir uma estrutura fielmente é construir um modelo que deixe o negócio claro, coeso e sustentável.

Esse projeto existe para colocar essa ideia em prática.
