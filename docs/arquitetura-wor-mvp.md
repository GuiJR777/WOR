# Arquitetura WOR (MVP Modular)

## Objetivo

Esta arquitetura organiza o jogo em modulos independentes, com separacao clara entre:

- dados (`Model`)
- apresentacao/orquestracao (`Presenter`)
- integracao com Unity (`View` em `MonoBehaviour`)

A base segue as regras de `Assets/Dev/Unity6_Development_Rules.md`: componentes menores, logica fora de `MonoBehaviour`, composicao e baixo acoplamento.

## Estrutura principal

### Runtime

- `Assets/Game/Runtime/Core`
  - `Mvp`: contratos base (`IModel`, `IView`, `IPresenter`, `PresenterBehaviour`)
  - `Messaging`: `GameEventBus` para comunicacao entre modulos

- `Assets/Game/Runtime/Modules`
  - `Input`
    - `Models/InputStateModel.cs`
  - `Units`
    - `Models/UnitStatsModel.cs`
    - `Models/HealthModel.cs`
    - `Models/UnitRegistryModel.cs`
    - `Services/UnitRegistryService.cs`
    - `Services/UnitTargetingService.cs`
  - `AI`
    - `Models/EnemyAiModel.cs`
    - `Views/IEnemyAiView.cs`
    - `Services/EnemyDecisionService.cs`
    - `Presenters/EnemyAiRuntimePresenter.cs`
  - `Combat`
    - `Services/CombatDamageCalculator.cs`

- `Assets/Game/Runtime/Units`
  - `UnitSettings` (dados configuraveis de unidade + sincronizacao com modelos)
  - `UnitActions` (view/runtime de unidade, fisica, interacao e integracao com estados)
  - `HealthSystem` (view de vida, usa `HealthModel`)
  - `EnemyBehaviour` (view MVP de IA inimiga)
  - estados de player/inimigo

- `Assets/Game/Runtime/StateMachine`
  - `State` (contrato de estado)
  - `StateMachine` (orquestrador por composicao, nao herda mais de `UnitActions`)

### Editor

- `Assets/Game/Editor`
  - custom inspectors e janelas de apoio de design

## Decisoes de refatoracao aplicadas

1. `StateMachine` foi desacoplado de `UnitActions`.
   - Antes: heranca direta (alto acoplamento).
   - Agora: composicao (`StateMachine` referencia `UnitActions`).

2. Targeting saiu de busca global direta para servico modular.
   - `UnitTargetingService` centraliza validacao de alvo e selecao de hostil mais proximo.
   - `UnitActions.findClosestPlayer()` delega ao servico.

3. Registro de units foi centralizado.
   - `UnitRegistryModel` + `UnitRegistryService` mantem units ativas para queries modulares.
   - `UnitActions` registra/desregistra no ciclo `OnEnable/OnDisable`.

4. IA inimiga foi movida para fluxo MVP.
   - `EnemyBehaviour` atua como `View`.
   - `EnemyAiModel` guarda estado de runtime da IA.
   - `EnemyAiRuntimePresenter` executa o fluxo de decisao.
   - `EnemyDecisionService` encapsula regra de decisao (atacar/reposicionar).

5. Calculo de dano foi separado de `UnitSettings`.
   - `CombatDamageCalculator` foi movido para `Modules/Combat/Services`.

## Como os modulos conversam

- `View` -> `Presenter`: ciclo por frame (`Update`) e eventos de entrada.
- `Presenter` -> `Model`: atualiza estado puro.
- `Presenter` -> `StateMachine`: troca de estados de gameplay.
- `Systems` cruzados: via `GameEventBus` (quando necessario) e services (`UnitRegistryService`).

## Regras para novas features

1. Novos dados de gameplay devem nascer em `Model` puro.
2. `MonoBehaviour` deve ficar focado em ciclo de vida, referencias Unity e rendering.
3. Regras de decisao, combate e targeting devem ficar em `Services`/`Presenters`.
4. Evitar `Find*` por frame; preferir registries e cache.
5. Evitar contrato por string quando houver alternativa tipada.
