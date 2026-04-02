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

## Tecnologias

Este projeto foi construído com foco em práticas modernas do ecossistema .NET, mas sem acoplar o núcleo do sistema a tecnologias externas.

### Base principal
- **.NET**
- **C#**

### Testes
- **xUnit**
- **FluentAssertions**
- **NSubstitute**
