# Manual: criar uma cena 2.5D com 1 player funcional

## Objetivo

Este guia cria uma cena nova com:

- 1 player jogavel
- camera funcional em 2.5D
- input, audio e UI funcionando
- combate basico funcionando
- dash, parry e knockback ativos

## Pre-requisitos

- Projeto aberto no Unity 6.
- Pasta do template disponivel em `Assets/Osarion/BeatEmUpTemplate2D`.

## Caminho recomendado (rapido)

1. Duplique a cena [02_TrainingHall.unity](c:/Users/MSI/Desktop/RamiresTech/Games/Projetos/WOR/Assets/Osarion/BeatEmUpTemplate2D/Scenes/02_TrainingHall.unity).
2. Renomeie para algo como `06_Test2p5D.unity`.
3. Apague os objetos que nao quer usar e mantenha:
`MainCamera`, `InputManager`, `AudioController`, `UI` e `Player`.
4. Ajuste cenario e posicao do player.
5. Teste os controles e as mecanicas novas (checklist no final).

Se voce quer montar totalmente do zero, use o passo a passo abaixo.

## Montagem do zero

1. Crie e salve uma nova cena.
Sugestao de caminho: `Assets/Osarion/BeatEmUpTemplate2D/Scenes/06_Test2p5D.unity`.

2. Arraste para a cena os prefabs globais obrigatorios:
- [MainCamera.prefab](c:/Users/MSI/Desktop/RamiresTech/Games/Projetos/WOR/Assets/Osarion/BeatEmUpTemplate2D/Prefabs/MainCamera.prefab)
- [InputManager.prefab](c:/Users/MSI/Desktop/RamiresTech/Games/Projetos/WOR/Assets/Osarion/BeatEmUpTemplate2D/Prefabs/InputManager.prefab)
- [AudioController.prefab](c:/Users/MSI/Desktop/RamiresTech/Games/Projetos/WOR/Assets/Osarion/BeatEmUpTemplate2D/Prefabs/AudioController.prefab)
- [UI.prefab](c:/Users/MSI/Desktop/RamiresTech/Games/Projetos/WOR/Assets/Osarion/BeatEmUpTemplate2D/Prefabs/UI.prefab)

3. Arraste o player para a cena:
- [Player.prefab](c:/Users/MSI/Desktop/RamiresTech/Games/Projetos/WOR/Assets/Osarion/BeatEmUpTemplate2D/Prefabs/Player.prefab)

4. Configure o plano 2.5D da cena.
Use `X` para esquerda/direita, `Z` para profundidade e `Y` para altura.

5. Monte um chao visual.
Pode ser um `Plane`, `Quad`, tilemap renderizado ou sprites de cenario.

6. Adicione colisao de ambiente.
Para setup 2.5D real, prefira `BoxCollider`/`MeshCollider` 3D nos blocos do cenario e use layer `Environment`.

7. (Opcional, mas recomendado) adicione areas de superficie para footstep.
Crie GameObjects com collider trigger e componente `Surface`.
Defina `footstepSFX` no componente.

8. Ajuste a camera para 2.5D.
No objeto `MainCamera`, mantenha o `CameraFollow`.
Se necessario, ajuste posicao/rotacao para enquadrar o plano `X/Z`.
Depois ajuste limites `Left`, `Right`, `Bottom` e `Top` do `CameraFollow`.

9. Posicione o player.
Deixe o player com `Y` no nivel do chao e ajuste `X/Z` para o ponto de spawn.

10. Valide fisica do personagem.
`Player` (e `Enemy`, se usar) deve ter `Rigidbody` + `CapsuleCollider` 3D.
Nao use `Rigidbody2D`/`CapsuleCollider2D` para novas configuracoes.

11. Verifique tags/layers minimas.
Player precisa estar com tag `Player`.
Elementos de bloqueio precisam estar em layer `Environment`.

12. (Opcional) adicione um inimigo para validar combate real.
Use [Enemy.prefab](c:/Users/MSI/Desktop/RamiresTech/Games/Projetos/WOR/Assets/Osarion/BeatEmUpTemplate2D/Prefabs/Enemy.prefab).

13. (Opcional) para fluxo de fase, adicione WaveManager.
Crie `WaveManager` e monte ondas como filhos (um filho por wave).
Cada wave pode ter seu `LevelBound`.

14. Salve a cena e adicione no Build Settings.

## Controles padrao (template)

- Movimento: setas / analogico esquerdo
- Punch: `Z` / gamepad `West`
- Kick: `X` / gamepad `North`
- Defend: `C` / shoulder
- Grab: `V` / shoulder
- Jump: `Space` / gamepad `South`
- Dash: duplo toque horizontal (esquerda ou direita)
- Parry: defender no timing da janela de parry

## Checklist de validacao (obrigatorio)

1. Player anda em `X` e profundidade em `Z`.
2. Jump sobe/desce em `Y`.
3. Dash dispara no duplo toque horizontal e mostra ghost trail.
4. Ataques aplicam dano.
5. Hits normais aplicam knockback.
6. Defend bloqueia.
7. Parry funciona no timing (janela padrao via `UnitSettings.parryWindow`).
8. Camera acompanha player sem sair dos bounds.
9. Player/Enemy estao com componentes 3D (`Rigidbody`/`CapsuleCollider`), sem setup 2D antigo.

## Ajustes rapidos de gameplay no Player

Abra o `Player` e ajuste em `UnitSettings`:

- `dashSpeed`, `dashDuration`, `dashCooldown`, `dashGhostInterval`
- `parryWindow`, `parryStunDuration`
- `hitKnockbackForce`, `hitKnockbackDuration`
- `depthMoveMultiplier`

## Problemas comuns

1. Player nao se move.
Verifique se existe exatamente 1 `InputManager` ativo na cena.

2. Dash nao ativa.
Teste duplo toque horizontal mais rapido e confira `canDash` no `UnitSettings`.

3. Parry nunca entra.
Aumente temporariamente `parryWindow` para validar timing.

4. Player atravessa paredes.
Confirme layer `Environment` nos colliders de cenario.

5. Footstep sempre default.
Confirme collider trigger + componente `Surface` + `footstepSFX`.

6. Camera estranha.
Ajuste rotacao/posicao da camera e os limites `Left/Right/Bottom/Top` no `CameraFollow`.

## Observacao importante sobre cenas antigas

O runtime atual faz conversao de compatibilidade para cenas antigas do template (onde profundidade era em `Y`).
Mesmo assim, para producao, monte novos niveis ja no padrao 2.5D (`X/Z` chao, `Y` altura).
