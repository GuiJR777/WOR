# Whiskers of Rage (WOR)

Projeto de jogo em Unity 6 focado em combate Beat'n Up com estrutura roguelike.

## O que e este projeto

Whiskers of Rage e um Beat'n Up Roguelike em 2.5D:

- mundo e fisica em 3D
- personagens em sprites 2D
- camera fixa com leitura clara de combate

O jogador controla **Ryu**, um gato ninja preso em um loop temporal, evoluindo build entre runs para recuperar a Armadura Sagrada.

## Resumo do jogo

- **Genero:** Beat'n Up Roguelike
- **Engine:** Unity 6
- **Plataforma alvo atual:** PC
- **Referencia de gameplay:** Absolum, Hades, Streets of Rage 4

Loop base:

1. entrar em uma run
2. lutar em encontros (combate, elite, tesouro etc.)
3. receber recompensas e ajustar build
4. avancar biomas e bosses
5. retornar ao hub ao morrer ou concluir ciclo

## Features principais (GDD)

### Combate

- combo de ataques leves/pesados
- jump, block, parry, grab, throw e dash
- knockback e knockdown com fisica 3D
- dash com ghost trail
- ataques com projeteis e armas equipaveis

### Progressao roguelike

- mapa por nodes com tipos de encontro
- recompensas por run (equipamentos, tecnicas, jutsus, economia)
- meta-progresso com recursos persistentes

### Build e RPG

- sistema de stats centralizado por unidade:
  - Constitution, Chakra, Strength, Defense, Agility, Luck
- critico, defesa e dano baseados em stats
- suporte a upgrades/downgrades por modificadores runtime
- faccoes e deteccao entre units para IA/combate

### Sistemas de jogo (em evolucao)

- tecnicas passivas por evento de combate
- sistema de jutsus elementais (base + combinados)
- condicoes elementais (status effects)
- equipamentos por slot e bonus de set

## Arquitetura tecnica

Arquitetura atual focada em **MVP modular** e separacao de responsabilidades:

- `Model`: dados e regras puras
- `View`: integracao Unity (`MonoBehaviour`)
- `Presenter`: orquestracao e fluxo de runtime

Pontos principais:

- `StateMachine` desacoplado de `UnitActions` (composicao)
- registro e targeting de units centralizados em services
- IA de inimigos no fluxo MVP (`EnemyAiModel` + presenter + view)
- calculo de dano separado em modulo de combate
- eventos/glue por barramento (`GameEventBus`) e services de dominio

## Estrutura de codigo (resumo)

- `Assets/Game/Runtime/Core`: base MVP + event bus
- `Assets/Game/Runtime/Modules`: modulos de Input, Units, AI e Combat
- `Assets/Game/Runtime/Units`: runtime de unidade, estados e configuracoes
- `Assets/Game/Editor`: ferramentas e custom inspectors

## Documentacao

- GDD: [docs/Whiskers_of_Rage_GDD_v2_1.md](docs/Whiskers_of_Rage_GDD_v2_1.md)
- Arquitetura MVP: [docs/arquitetura-wor-mvp.md](docs/arquitetura-wor-mvp.md)

