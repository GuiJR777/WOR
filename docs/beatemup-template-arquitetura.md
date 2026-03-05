# Documento tecnico do template Beat 'Em Up 2D

## Resumo executivo

Este projeto e um template de Beat 'Em Up 2D feito em Unity 6 (`6000.3.4f1`), com URP e Input System.

O nucleo do gameplay gira em torno de tres blocos:

- `StateMachine`: controla o estado atual de cada unidade.
- `UnitActions`: concentra movimento, combate, deteccao e funcoes utilitarias da unidade.
- `UnitSettings`: concentra quase todos os dados configuraveis da unidade no Inspector.

Em cima disso, o template adiciona:

- IA simples de inimigos via estados.
- gerenciamento de ondas via `WaveManager`.
- HUD e menus via `UIManager`.
- itens, armas, projeteis, audio e camera como sistemas de suporte.

Veredito direto: a arquitetura e boa como template jogavel e facil de editar no Unity, mas nao e uma arquitetura forte para um jogo de producao maior sem refatoracao. Ela privilegia velocidade de iteracao e uso no Inspector, nao escalabilidade.

## Onde esta a base do projeto

O codigo principal do template esta em:

- `Assets/Osarion/BeatEmUpTemplate2D/Scripts`

As cenas principais buildadas sao:

- `00_MainMenu`
- `01_LevelSelection`
- `02_TrainingHall`
- `03_Level1`
- `04_Level2`
- `05_Level3`

Os prefabs mais importantes para entender a montagem sao:

- `Prefabs/Player.prefab`
- `Prefabs/Enemy.prefab`
- `Prefabs/MainCamera.prefab`
- `Prefabs/UI.prefab`
- `Prefabs/InputManager.prefab`
- `Prefabs/AudioController.prefab`

## Arquitetura em uma frase

Cada unidade importante do jogo e um `GameObject` com componentes de dados e runtime; o fluxo e dirigido por uma maquina de estados, enquanto colisao de ataque, dano, IA e progressao de fase ficam espalhados em componentes pequenos e bastante orientados a prefab.

## Mapa dos sistemas

### 1. Nucleo de estado

Arquivos:

- `Scripts/StateMachine/State.cs`
- `Scripts/StateMachine/StateMachine.cs`

Como funciona:

- `State` e a classe base de todos os estados.
- Cada estado recebe uma referencia `unit` do tipo `UnitActions`.
- `StateMachine` guarda o estado atual, faz `Enter`, `Update`, `FixedUpdate`, `LateUpdate` e `Exit`.
- No `Start`, o player comeca em `PlayerIdle` e o inimigo em `EnemyIdle`.

Observacao importante:

- `StateMachine` herda de `UnitActions`.
- Isso significa que a propria maquina de estados tambem e o objeto de acoes da unidade.

Na pratica, isso funciona, mas mistura responsabilidade de runtime com orquestracao de estado.

### 2. Runtime de unidades

Arquivo:

- `Scripts/Units/UnitActions.cs`

Esse e o arquivo mais importante do template. Ele concentra:

- movimento (`MoveToVector`, `StopMoving`, `JumpSequence`)
- direcao (`TurnToTarget`, `TurnToDir`)
- combate (`CheckForHit`, `GetObjectsHit`)
- interacao (`GetClosestPickup`, `NearbyEnemyDown`)
- utilitarios visuais e de feedback (`ShowHitEffectAtPosition`, `ShowEffect`, `CamShake`)
- sensores (`targetInSight`, `WallDetected`)

Tambem e aqui que aparecem varios pontos estruturais do template:

- uso de `GameObject.FindGameObjectsWithTag`
- uso de `Resources.Load`
- uso de `static event` para comunicar dano
- dependencia forte de `SpriteRenderer.bounds` e da `hitBox` visual

### 3. Dados de unidade

Arquivos:

- `Scripts/Units/UnitSettings.cs`
- `Scripts/Units/AttackData.cs`

`UnitSettings` concentra praticamente toda a configuracao da unidade:

- movimento e pulo
- combos
- ataques aereos
- ataques agarrado / no chao
- knockdown e throw
- defesa
- grab
- nome, portrait e FOV
- lista de ataques de inimigo

`AttackData` e o DTO serializavel de ataque:

- dano
- nome da animacao
- SFX
- tipo do ataque
- knockdown ou nao

`Combo` tambem e serializado dentro de `UnitSettings`, como lista de `AttackData`.

Conclusao: a autoria de conteudo acontece majoritariamente no prefab/Inspector, nao em `ScriptableObject`.

### 4. Vida, dano e morte

Arquivo:

- `Scripts/Units/HealthSystem.cs`

Esse componente:

- controla HP atual e maximo
- emite evento de mudanca de vida
- emite evento de morte de unidade
- cria barra pequena opcional
- mostra hit flash, shake e efeitos
- remove objetos comuns ao zerar vida

Para unidades:

- quando um alvo chega a 0 HP, a troca real de estado para morte geralmente acontece no fluxo de combate (`UnitActions.CheckForHit`) ou nos estados de knockdown.

### 5. Estados do player

Pasta:

- `Scripts/Units/PlayerStates`

Fluxo principal:

- `PlayerIdle`: estado base. Escuta input e decide proximo estado.
- `PlayerMove`: movimento horizontal/profundidade.
- `PlayerAttack`: resolve combo de punch/kick.
- `PlayerJump` e `PlayerJumpAttack`: pulo e ataque no ar.
- `PlayerTryGrab`: decide entre pegar item ou agarrar inimigo.
- `PlayerGrabEnemy`, `PlayerGrabAttack`, `PlayerThrowEnemy`: loop de agarrar.
- `PlayerWeaponAttack`: usa arma equipada.
- `PlayerGroundPunch` e `PlayerGroundKick`: finalizacao em inimigo derrubado.
- `PlayerLand`: aterrissagem.

Estados genericos compartilhados:

- `UnitHit`
- `UnitDefend`
- `UnitKnockDown`
- `UnitKnockDownGrounded`
- `UnitStandUp`
- `UnitDropWeapon`
- `UnitDeath`

Esse desenho e simples e facil de seguir. Para um template de combate corpo a corpo, e uma boa escolha.

### 6. IA de inimigos

Arquivos:

- `Scripts/Units/EnemyBehaviour.cs`
- `Scripts/Units/EnemyManager.cs`
- `Scripts/Units/EnemyStates/*`

Fluxo:

- `EnemyBehaviour` toma decisoes em intervalos.
- O inimigo so decide algo quando ja viu o alvo.
- A decisao so acontece se ele estiver em `EnemyIdle`.
- A IA consulta `EnemyManager.GetEnemyAttackerCount()` para nao deixar muitos inimigos atacando ao mesmo tempo.

Estados principais:

- `EnemyIdle`
- `EnemyMoveTo`
- `EnemyKeepDistance`
- `EnemyMoveToTargetAndAttack`
- `EnemyAttack`
- `EnemyGrabbed`
- `EnemyWait`

Essa IA e funcional para Beat 'Em Up classico: aproxima, cerca, espera, reveza ataque.

Ela nao e sofisticada, mas cumpre o papel do genero.

### 7. Ondas e progressao de fase

Arquivo:

- `Scripts/WaveManager/WaveManager.cs`

Como funciona:

- `WaveManager` assume que cada filho dele representa uma onda.
- No `Start`, ele coleta as ondas, desativa todas e ativa a primeira.
- Quando um inimigo morre, ele atualiza contagens.
- Quando a onda atual zera, ativa a proxima.
- Quando tudo acaba, abre menu de fim de fase.

Integracoes importantes:

- usa `HealthSystem.onUnitDeath`
- move `LevelBound` para controlar a camera
- grava progresso em `LevelProgress`
- chama `UIManager` por nome de menu

### 8. Camera e limites

Arquivos:

- `Scripts/Camera/CameraFollow.cs`
- `Scripts/Camera/CameraShake.cs`
- `Scripts/Level/LevelBound.cs`

`CameraFollow`:

- segue todos os objetos com tag `Player`
- calcula centro entre players
- aplica damp em X e Y
- restringe movimento a uma area configuravel
- opcionalmente mantem players dentro da tela
- respeita `LevelBound` para travar avanco ate liberar onda

Ponto de projeto importante:

- a camera nao apenas observa o player; ela tambem pode corrigir diretamente a posicao do player para mante-lo dentro da view.

Isso e pragmatico, mas mistura regra de camera com regra de movimentacao.

### 9. UI e fluxo de menus

Arquivos:

- `Scripts/UI/UIManager.cs`
- `Scripts/UI/UIHUDHealthBar.cs`
- `Scripts/UI/UILevelSelection.cs`
- `Scripts/UI/UIButton.cs`
- `Scripts/UI/UIExitSign.cs`

`UIManager` e simples:

- recebe uma lista de menus por nome
- desliga todos
- ativa o menu desejado

`UIHUDHealthBar`:

- escuta evento de vida
- atualiza barra do player, inimigo ou boss

`UILevelSelection`:

- gera botoes de fase em runtime
- usa `LevelProgress.levelsCompleted` para travar/destravar

`UIButton`:

- adiciona navegacao por input
- toca SFX
- carrega cenas com delay do audio

### 10. Itens, armas e projeteis

Arquivos:

- `Scripts/Items/Item.cs`
- `Scripts/Items/WeaponPickup.cs`
- `Scripts/Units/WeaponAttachment.cs`
- `Scripts/Items/HealthPickup.cs`
- `Scripts/Items/Projectile.cs`

Fluxo:

- item e um objeto simples com feedback visual.
- arma equipada vira filha do `WeaponAttachment`.
- o sprite muda para a versao de "arma na mao".
- o estado `PlayerWeaponAttack` usa `attackData` da arma.
- projeteis sao objetos independentes que andam e verificam hit por overlap de bounds.

### 11. Audio

Arquivos:

- `Scripts/AudioController/AudioController.cs`
- `Scripts/AudioController/AudioItem.cs`

Modelo:

- `AudioController` singleton
- `AudioItem[]` configurado no Inspector
- todo o projeto dispara audio por string (`PlaySFX("NomeDoSfx")`)

Funciona bem para template e e rapido de configurar.
Para projeto grande, gera risco de erro por typo e dependencia fraca de referencia.

### 12. Ferramentas de editor

Pasta:

- `Scripts/Editor`

O template investe bastante em UX no Inspector:

- `UnitSettingsEditor` organiza a configuracao por foldouts e ajuda contextual
- `WaveManagerEditor`, `HealthSystemEditor`, `CameraFollowEditor` explicam o uso do componente

Isso e um ponto forte real da base. Ela foi pensada para ser consumida por designer ou developer dentro do editor.

## Fluxo real do gameplay

### Fluxo do player

1. A cena sobe com `InputManager`, `AudioController`, `MainCamera`, `UI` e um ou mais players.
2. O `StateMachine` do player entra em `PlayerIdle`.
3. `PlayerIdle` consulta `InputManager` e troca para `Move`, `Jump`, `Attack`, `TryGrab`, `Defend` ou `WeaponAttack`.
4. Estados de ataque usam `AttackData` e chamam `unit.CheckForHit`.
5. `CheckForHit` encontra alvos por tag, confere interseccao de bounds e aplica dano.
6. `HealthSystem` reduz HP e dispara eventos.
7. Se houve knockdown, o alvo entra em `UnitKnockDown`; senao em `UnitHit`; se morreu, entra em `UnitDeath`.

### Fluxo do inimigo

1. O `StateMachine` do inimigo entra em `EnemyIdle`.
2. `EnemyBehaviour` roda em paralelo e espera o target ser visto.
3. Em janelas de decisao, ele escolhe:
- atacar
- se reposicionar perto
- se afastar um pouco
4. Ao atacar, entra em `EnemyMoveToTargetAndAttack`.
5. Quando entra em range, troca para `EnemyAttack`.

### Fluxo de fase

1. `WaveManager` ativa so a primeira onda.
2. Inimigos mortos atualizam o contador global.
3. Quando a onda atual zera, a proxima e ativada.
4. O `LevelBound` da camera muda junto com a onda.
5. Quando nao ha mais ondas ou inimigos, `UIManager` abre menu de fim de fase.

## O que a arquitetura faz bem

### Boa legibilidade para quem abre o projeto pela primeira vez

O nome das pastas e dos scripts e claro. Quase tudo esta em lugares previsiveis:

- estados em `PlayerStates` e `EnemyStates`
- UI em `UI`
- itens em `Items`
- camera em `Camera`
- fase em `WaveManager` e `Level`

### Boa modelagem para um template de combate

A escolha de maquina de estados com classes pequenas funciona bem para um Beat 'Em Up:

- idle
- move
- attack
- jump
- hit
- knockdown
- defend
- death

Isso deixa o fluxo de gameplay facil de rastrear.

### Forte foco em configuracao por Inspector

`UnitSettings` e os custom editors tornam a iteracao rapida.

Voce consegue:

- duplicar `Enemy.prefab`
- ajustar ataques
- trocar stats
- mudar nome / portrait
- testar rapido

Sem quase tocar em codigo.

### Boas abstraoes para um template pequeno

Exemplos:

- `AttackData` para padronizar ataques
- `Combo` para sequencia de golpes
- `EnemyManager` para coordenacao simples do crowd control
- `WaveManager` para progressao de nivel

## Onde a arquitetura comeca a ficar fraca

### 1. Acoplamento alto ao Unity runtime

O codigo depende muito de:

- `GameObject.FindGameObjectsWithTag`
- `FindObjectsOfType`
- `Resources.Load`
- `Camera.main`
- singletons estaticos
- eventos estaticos

Isso e aceitavel em template, mas ruim para:

- performance com muitos objetos
- testes automatizados
- previsibilidade
- reaproveitamento em sistemas maiores

### 2. `StateMachine` herdando de `UnitActions`

Isso simplifica acesso aos metodos, mas mistura papeis:

- estado/orquestracao
- runtime de combate/movimento

Em um projeto maior, eu separaria em:

- `UnitController` ou `UnitRuntime`
- `StateMachine`
- `CombatController`

### 3. Dados demais dentro de `MonoBehaviour`

Quase toda a autoria de design esta em `UnitSettings` e nos prefabs.

Isso torna o template pratico, mas dificulta:

- reuso sistematico de dados
- variacoes de inimigo por data asset
- balanceamento em lote
- versionamento limpo

Para crescer, seria melhor migrar parte disso para `ScriptableObject`:

- definicoes de ataque
- definicoes de arma
- archetypes de inimigo
- configuracoes de combo

### 4. Uso pesado de strings como contrato

Exemplos:

- nome de state de animacao
- nome de menu
- nome de SFX
- nome de recurso em `Resources`

Isso deixa o sistema flexivel, mas fraco em seguranca de refatoracao.

Se voce renomear algo, o compilador nao ajuda.

### 5. Sistema de combate orientado a bounds visuais

Os hits usam:

- `settings.hitBox.bounds`
- `SpriteRenderer.bounds`

Isso e rapido para montar, mas nao e a abordagem mais robusta.

Limitacoes:

- depende muito da arte estar correta
- dificulta hurtboxes mais precisas
- dificulta ataques com varios frames / varias hitboxes
- deixa o combate menos data-driven

### 6. Escalabilidade baixa para multiplayer real

O projeto tem `playerId`, mas o `InputManager` atual usa uma unica instancia de `PlayerControls` e ignora de fato o `playerId` recebido.

Ou seja:

- a API sugere suporte a varios players
- a implementacao pratica e single-player

### 7. Quase nenhuma camada de teste ou validacao automatica

Nao encontrei testes proprios em `Assets`.

Para template isso nao mata o projeto.
Para produto, aumenta risco de regressao principalmente em:

- combos
- knockdown
- waves
- level progression
- menus de game over / fim de fase

## Problemas ou arestas que apareceram na leitura

Nao sao impeditivos para uso do template, mas indicam que a base nao foi pensada com o rigor de uma arquitetura de produto grande:

- `WaveManager` declara `slowMotionInProgress`, mas a flag nao e realmente ligada/desligada.
- `BallBehaviour` incrementa o contador de bounce duas vezes dentro do loop.
- `CameraFollow` pode ajustar diretamente a posicao dos targets, o que e funcional, mas invasivo.
- boa parte da deteccao e busca de objetos acontece por scan global da cena.

## Entao: a arquitetura e boa?

Resposta curta:

- Sim, para template, estudo e prototipacao.
- Nao, como base final de um jogo comercial sem refatoracao.

Resposta mais precisa:

- Como template de marketplace/starting point: boa.
- Como arquitetura de medio/longo prazo: mediana.

Ela tem varias decisoes corretas para o objetivo dela:

- clara
- editavel
- pragmatica
- facil de extender com novos estados

Mas ela sacrifica:

- separacao de responsabilidades
- robustez de contratos
- escalabilidade
- testabilidade
- performance em cenarios maiores

## O que eu manteria

- a organizacao por dominio de pasta
- o uso de estados pequenos para gameplay
- `AttackData` e `Combo` como conceito
- `WaveManager` como orquestrador de encounter
- os custom inspectors

## O que eu refatoraria antes de transformar em jogo real

### Prioridade alta

1. Separar `StateMachine` de `UnitActions`.
2. Reduzir `Find*` globais com registries ou referencias cacheadas.
3. Tirar contratos por string de partes criticas.
4. Migrar dados centrais para `ScriptableObject`.
5. Criar testes de fluxo para combate e progressao de fase.

### Prioridade media

1. Separar hitbox/hurtbox de `SpriteRenderer.bounds`.
2. Melhorar `InputManager` para multiplayer real ou simplificar API para single-player explicito.
3. Criar uma camada de eventos menos global.
4. Revisar scripts auxiliares com bugs pequenos.

## Ordem recomendada para estudar o codigo

Se voce quiser entender o projeto sem se perder, leia nesta ordem:

1. `Scripts/StateMachine/State.cs`
2. `Scripts/StateMachine/StateMachine.cs`
3. `Scripts/Units/UnitActions.cs`
4. `Scripts/Units/UnitSettings.cs`
5. `Scripts/Units/AttackData.cs`
6. `Scripts/Units/PlayerStates/*`
7. `Scripts/Units/EnemyBehaviour.cs`
8. `Scripts/Units/EnemyStates/*`
9. `Scripts/Units/HealthSystem.cs`
10. `Scripts/WaveManager/WaveManager.cs`
11. `Scripts/UI/*`
12. `Scripts/Items/*`
13. `Scripts/Camera/*`

## Conclusao

Esse template foi construido para ser entendido e alterado rapidamente dentro do Unity, e nisso ele funciona bem.

Ele tem uma arquitetura coerente para um projeto pequeno: estados claros, prefabs reaproveitaveis, dados expostos no Inspector e um fluxo de combate relativamente facil de acompanhar.

O limite aparece quando voce pensa em produto maior. A base tem acoplamento alto, muitos contratos por string, uso pesado de buscas globais e pouca separacao entre regras, dados e infraestrutura.

Se o objetivo for aprender, prototipar ou produzir uma vertical slice, eu usaria essa base.
Se o objetivo for construir um jogo maior em cima dela, eu usaria o template como referencia funcional e refatoraria o nucleo antes de expandir demais.
