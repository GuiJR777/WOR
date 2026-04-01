e

**WHISKERS OF RAGE**

Game Design Document

Versão: 2.1 (Roguelike Beat’n Up)  
Autor: Guilherme Jesuino Ramires — RamiresTech Games  
Data: 05 Mar 2026

# **Índice**

[Índice](#heading=)

[1\. Visão Geral](#heading=)

[2\. Pilares de Design](#heading=)

[3\. Estrutura de Run e Progressão](#heading=)

[3.1 Loop de Gameplay](#heading=)

[3.2 O que persiste entre Runs](#heading=)

[4\. Sistema de Combate (Beat’n Up)](#heading=)

[4.1 Ações Base](#heading=)

[4.2 Regras de Combos (proposta)](#heading=)

[5\. Técnicas (Passivas por Evento)](#heading=)

[5.1 Eventos que podem ativar técnicas](#heading=)

[5.2 Tabela de Técnicas](#5.2-tabela-de-técnicas)

[5.2.1 Técnicas — Arte Ninja do Shinobi](#5.2.1-técnicas-—-arte-ninja-do-shinobi)

[5.2.2 Técnicas — Arte Ninja das Sombras](#5.2.2-técnicas-—-arte-ninja-das-sombras)

[5.2.3 Técnicas — Arte Ninja do Veneno](#5.2.3-técnicas-—-arte-ninja-do-veneno)

[5.2.4 Técnicas — Arte Ninja da Água](#5.2.4-técnicas-—-arte-ninja-da-água)

[5.2.5 Técnicas — Arte Ninja do Fogo](#5.2.5-técnicas-—-arte-ninja-do-fogo)

[5.2.6 Técnicas — Arte Ninja do Vento](#5.2.6-técnicas-—-arte-ninja-do-vento)

[5.2.7 Técnicas — Arte Ninja da Terra](#5.2.7-técnicas-—-arte-ninja-da-terra)

[5.2.8 Técnicas — Arte Ninja do Ilusionista](#5.2.8-técnicas-—-arte-ninja-do-ilusionista)

[5.2.9 Técnicas — Arte Ninja do Trovão](#5.2.9-técnicas-—-arte-ninja-do-trovão)

[6\. Jutsus (Elementais)](#heading=)

[6.1 Slots de Jutsu](#heading=)

[6.2 Tabelas de Jutsus e combinações](#heading=)

[6.2.1 Sistema de Jutsus — Whiskers of Rage](#6.2.1-sistema-de-jutsus-—-whiskers-of-rage)

[6.2.2 Arte Ninja do Shinobi](#6.2.2-arte-ninja-do-shinobi)

[6.2.3 Arte Ninja das Sombras](#6.2.3-arte-ninja-das-sombras)

[6.2.4 Arte Ninja do Veneno](#6.2.4-arte-ninja-do-veneno)

[6.2.5 Arte Ninja da Água](#6.2.5-arte-ninja-da-água)

[6.2.6 Arte Ninja do Fogo](#6.2.6-arte-ninja-do-fogo)

[6.2.7 Arte Ninja do Vento](#6.2.7-arte-ninja-do-vento)

[6.2.8 Arte Ninja da Terra](#6.2.8-arte-ninja-da-terra)

[6.2.9 Arte Ninja do Ilusionista](#6.2.9-arte-ninja-do-ilusionista)

[6.2.10 Arte Ninja do Trovão](#6.2.10-arte-ninja-do-trovão)

[6.3 Cores de cada Arte Ninja (UI)](#6.3-cores-de-cada-arte-ninja-\(ui\))

[6.4 Ícones sugeridos](#6.4-ícones-sugeridos)

[6.5 Raridade de Jutsus](#6.5-raridade-de-jutsus)

[6.6 Sistema de Upgrade de Jutsu](#6.6-sistema-de-upgrade-de-jutsu)

[7\. Condições Elementais (Status Effects)](#heading=)

[8\. Equipamentos (Slots e Bônus)](#heading=)

[8.1 Slots](#heading=)

[8.2 Sets de Equipamentos](#heading=)

[8.3 Tabela de Equipamentos](#8.3-tabela-de-equipamentos)

[8.4 Bônus de Sets](#8.4-bônus-de-sets)

[Observação importante de lore](#observação-importante-de-lore)

[9\. Arremessáveis e Ferramentas Ninja](#heading=)

[9.1 Regras](#9.1-regras)

[9.2 Tabela de Arremessáveis](#9.2-tabela-de-arremessáveis)

[9.3 Distribuição de raridade](#9.3-distribuição-de-raridade)

[9.4 Preço sugerido no Mercado](#9.4-preço-sugerido-no-mercado)

[10\. Mapa de Nodes e Tipos de Encontro](#10.-mapa-de-nodes-e-tipos-de-encontro)

[11\. Economia e Meta-progresso](#heading=)

[12\. Stats do Personagem](#heading=)

[13\. Artes Ninja (9 Escolas)](#heading=)

[14\. Lore Completa](#heading=)

[15\. Biomas e Bosses](#heading=)

[16\. Hub (Templo)](#heading=)

[17\. Direção de Arte e UX/HUD](#heading=)

[17.1 Direção de Arte](#heading=)

[17.2 HUD (proposta)](#heading=)

[18\. Requisitos Técnicos e Nomenclaturas](#heading=)

[19\. Status do Projeto](#heading=)

# **1\. Visão Geral**

Whiskers of Rage é um Beat’n Up Roguelike em 2.5D inspirado em Absolum e Hades. O jogador controla Ryu, um gato ninja prodígio, que vive preso em um loop temporal e precisa recuperar a Armadura Sagrada roubada por clãs rivais — dominando novas Artes Ninja, acumulando poder e derrotando os mestres de cada bioma.

| Título | Whiskers of Rage |
| :---- | :---- |
| Gênero | Beat’n Up Roguelike |
| Referências | Absolum, Hades, Streets of Rage 4 |
| Plataforma | PC (futuro: consoles) |
| Engine | Unity 6 |
| Estilo | 2.5D: gameplay 3D com visual de sprites |
| Jogabilidade | Runs com mapa de nodes; meta-progressão persistente |

# **2\. Pilares de Design**

| Pilar | O que significa na prática |
| :---- | :---- |
| Combate Beat’n Up como núcleo | Leitura clara, hit-confirm, combos, crowd-control e posicionamento. |
| Roguelike com escolhas relevantes | Recompensas aleatórias, caminhos no mapa, risco x recompensa (Elite/Tesouro). |
| Construção de build Ninja | Técnicas por evento \+ Jutsus elementais \+ Equipamentos de stats. |
| Meta-progresso satisfatório | Cristais e Pergaminhos persistentes para evolução contínua e desbloqueios. |
| Fantasia Ninja consistente | Ferramentas, status elementais e Artes Ninja com identidade forte. |

# **3\. Estrutura de Run e Progressão**

Uma Run é uma tentativa completa de atravessar biomas e recuperar partes da Armadura Sagrada. Ao morrer ou ao fim do dia, o loop reinicia e o jogador retorna ao Hub (Templo).

## **3.1 Loop de Gameplay**

* Escolher Jutsu inicial (Slot 1\) e iniciar Run  
* Selecionar caminho no mapa de nodes do bioma  
* Completar encontro (combate, elite, tesouro etc.)  
* Receber recompensa (equipamento, técnica, jutsu, ouro, recursos)  
* Visitar mercado/meditação quando disponível  
* Derrotar boss do bioma e avançar  
* Se morrer: voltar ao templo mantendo recursos persistentes

## **3.2 O que persiste entre Runs**

| Recurso | Persiste? |
| :---- | :---- |
| Gold | NÃO (somente na run) |
| Secret Scrolls (Pergaminhos Secretos) | SIM (compras, reroll, upgrade de técnicas) |
| Battle Experience | SIM (gera cristais) |
| Experience Crystals | SIM (up de stats) |
| Artes Ninja desbloqueadas | SIM (novas escolas disponíveis para futuras runs) |

# **4\. Sistema de Combate (Beat’n Up)**

Combate rápido e técnico com foco em:  
\- Controle de grupo (crowd control)  
\- Cancelamentos (dash/jump) e posicionamento  
\- Recompensa por precisão (parry) e agressividade (combos)  
\- Construção de build via técnicas \+ jutsus \+ equipamentos

## **4.1 Ações Base**

| Ação | Descrição |
| :---- | :---- |
| Light Attack | Ataque rápido para iniciar combo e ativar técnicas on-hit. |
| Heavy Attack | Ataque mais lento, maior stagger/knockback. |
| Finisher | Golpe final do combo (pode disparar técnicas específicas). |
| Dash | Reposicionamento e i-frames curtos (dependendo do balance). |
| Jump | Mobilidade vertical; ataques aéreos; evita certas ondas/armadilhas. |
| Block | Defesa; reduz dano; habilita técnicas on-block. |
| Parry | Janela curta; anula dano e abre punish; habilita técnicas on-parry. |
| Grab | Agarra inimigo vulnerável; permite arremessar. |
| Throw | Arremesso do alvo agarrado; controle de espaço e dano. |

## **4.2 Regras de Combos (proposta)**

Combos avançam apenas se o golpe acertar (hit-confirm). Finalizadores mudam conforme direção, estado (chão/ar) e build.

# **5\. Técnicas (Passivas por Evento)**

Técnicas são habilidades especiais que NÃO consomem chakra. Elas ativam automaticamente quando um evento acontece.

## **5.1 Eventos que podem ativar técnicas**

| Trigger | Evento |
| ----- | ----- |
| Quick Attack Hit | Acertou ataque rápido |
| Heavy Attack Hit | Acertou ataque forte |
| Finisher Hit | Acertou finalizador |
| Dash Start | Iniciou dash |
| Jump Start | Iniciou pulo |
| Landing | Aterrissou |
| Block | Bloqueou ataque |
| Parry | Parry perfeito |
| Throw | Agarrou inimigo |
| Throw Release | Arremessou inimigo |
| Throwable Use | Usou arremessável |

* 

## **5.2 Tabela de Técnicas** {#5.2-tabela-de-técnicas}

###  **5.2.1 Técnicas — Arte Ninja do Shinobi** {#5.2.1-técnicas-—-arte-ninja-do-shinobi}

| Técnica | Trigger | Efeito |
| ----- | ----- | ----- |
| Instinto do Assassino | Finisher Hit | finalizador aplica **Bleeding** |
| Passo Fantasma | Dash Start | dash ganha invulnerabilidade curta |
| Precisão Mortal | Quick Attack Hit | pequena chance de crítico |
| Arremesso Ninja | Throwable Use | kunais causam bleed |
| Execução Silenciosa | Heavy Attack Hit | \+30% dano em inimigos com status |

### **5.2.2 Técnicas — Arte Ninja das Sombras** {#5.2.2-técnicas-—-arte-ninja-das-sombras}

| Técnica | Trigger | Efeito |
| ----- | ----- | ----- |
| Passo Sombrio | Dash Start | deixa **clone temporário** |
| Manto da Escuridão | Block | cria pequena área de sombra |
| Dança das Sombras | Quick Attack Hit | chance de aplicar **Blind** |
| Clone Protetor | Landing | clone absorve próximo golpe |
| Abraço do Abismo | Heavy Attack Hit | dano aumentado em inimigos cegos |

### **5.2.3 Técnicas — Arte Ninja do Veneno** {#5.2.3-técnicas-—-arte-ninja-do-veneno}

| Técnica | Trigger | Efeito |
| ----- | ----- | ----- |
| Lâmina Tóxica | Quick Attack Hit | aplica **Poisoned** |
| Nuvem Corrosiva | Finisher Hit | pequena explosão venenosa |
| Sangue Corrompido | Heavy Attack Hit | bleed vira poison |
| Veneno Persistente | Throw | agarrões aplicam poison |
| Vapores Mortais | Landing | pequena nuvem venenosa surge |

### **5.2.4 Técnicas — Arte Ninja da Água** {#5.2.4-técnicas-—-arte-ninja-da-água}

| Técnica | Trigger | Efeito |
| ----- | ----- | ----- |
| Passo Fluido | Dash Start | dash deixa rastro de água |
| Maré Curativa | Finisher Hit | cura pequena quantidade |
| Correnteza | Heavy Attack Hit | empurra inimigos |
| Reflexo da Água | Parry | contra ataque automático |
| Névoa Calmante | Landing | cria névoa que reduz dano recebido |

### **5.2.5 Técnicas — Arte Ninja do Fogo** {#5.2.5-técnicas-—-arte-ninja-do-fogo}

| Técnica | Trigger | Efeito |
| ----- | ----- | ----- |
| Punho Ardente | Quick Attack Hit | aplica **Burning** |
| Explosão Carmesim | Finisher Hit | explosão flamejante |
| Marca da Chama | Heavy Attack Hit | inimigos recebem \+20% dano |
| Passo Incendiário | Dash Start | deixa rastro de fogo |
| Fúria Flamejante | Throw Release | inimigo arremessado explode |

### **5.2.6 Técnicas — Arte Ninja do Vento** {#5.2.6-técnicas-—-arte-ninja-do-vento}

| Técnica | Trigger | Efeito |
| ----- | ----- | ----- |
| Salto do Furacão | Jump Start | cria mini tornado |
| Passo Cortante | Dash Start | empurra inimigos |
| Tempestade Ascendente | Landing | vento arremessa inimigos |
| Golpe da Rajada | Heavy Attack Hit | knockback grande |
| Dança do Vendaval | Finisher Hit | lança inimigos no ar |

### 

### **5.2.7 Técnicas — Arte Ninja da Terra** {#5.2.7-técnicas-—-arte-ninja-da-terra}

| Técnica | Trigger | Efeito |
| ----- | ----- | ----- |
| Pele de Pedra | Block | reduz dano recebido |
| Contra Rocha | Parry | cria espinhos no chão |
| Muralha Ninja | Landing | pequeno escudo temporário |
| Punho da Montanha | Heavy Attack Hit | aplica **Stun** |
| Terra Instável | Throw Release | inimigo cria onda de choque |

### **5.2.8 Técnicas — Arte Ninja do Ilusionista** {#5.2.8-técnicas-—-arte-ninja-do-ilusionista}

| Técnica | Trigger | Efeito |
| ----- | ----- | ----- |
| Substituição | Dash Start | deixa clone ilusório |
| Reflexo Fantasma | Parry | cria clone defensivo |
| Teatro do Caos | Finisher Hit | aplica **Confused** |
| Máscara da Mentira | Block | inimigos perdem alvo |
| Rastro Fantasma | Landing | cria clone ilusório temporário |

### **5.2.9 Técnicas — Arte Ninja do Trovão** {#5.2.9-técnicas-—-arte-ninja-do-trovão}

| Técnica | Trigger | Efeito |
| ----- | ----- | ----- |
| Lâmina Elétrica | Quick Attack Hit | aplica **Electrocuted** |
| Passo Relâmpago | Dash Start | dash causa dano |
| Impacto do Trovão | Landing | choque elétrico em área |
| Parry Relâmpago | Parry | raio atinge inimigo |
| Tempestade Interior | Finisher Hit | chain lightning curto |

# **6\. Jutsus (Elementais)**

Jutsus são habilidades elementais poderosas. O jogador equipa 1 antes da run e pode ganhar outro durante a run. Um terceiro jutsu é gerado automaticamente combinando os elementos dos dois primeiros.

## **6.1 Slots de Jutsu**

| Slot | Regra |
| :---- | :---- |
| Jutsu Slot 1 (Equipável) | Escolhido antes da run; pode ser trocado por recompensas. |
| Jutsu Slot 2 (Equipável) | Obtido durante a run; pode ser substituído por recompensas. |
| Jutsu Slot 3 (Automático) | Combinação dos elementos do Slot 1 \+ Slot 2\. |

## **6.2 Tabelas de Jutsus e combinações**

### **6.2.1 Sistema de Jutsus — Whiskers of Rage** {#6.2.1-sistema-de-jutsus-—-whiskers-of-rage}

| Tipo | Quantidade |
| ----- | ----- |
| Artes Ninja | 9 |
| Jutsus Base | 9 |
| Jutsus Combinados | 36 |
| Total | **45 Jutsus** |

---

### **6.2.2 Arte Ninja do Shinobi** {#6.2.2-arte-ninja-do-shinobi}

| Combinação | Nome PT | Nome JP sugerido | Descrição | Gameplay | VFX |
| ----- | ----- | ----- | ----- | ----- | ----- |
| Shinobi | Tempestade de Lâminas | **Senbon Arashi** | Ryu salta e lança várias kunais e shurikens. | Área grande com múltiplos hits. | projéteis girando \+ trilhas metálicas |
| Shinobi \+ Sombras | Breu do Assassino | **Kage Ankoku** | Escuridão cobre o cenário. | Ataques viram **críticos** enquanto inimigos estão cegos. | tela escurece com aura roxa |
| Shinobi \+ Veneno | Chuva de Agulhas Tóxicas | **Dokubari Ame** | Disparo massivo de agulhas venenosas. | aplica **Poisoned** em área. | agulhas verdes |
| Shinobi \+ Água | Assassino das Profundezas | **Suiton Koroshi** | Ryu mergulha na água e surge atrás dos inimigos. | críticos em cadeia. | splash \+ teleport aquático |
| Shinobi \+ Fogo | Meteoro Carmesim | **Katon Kunai Rain** | Kunais sobem e caem explodindo. | explosões aleatórias. | chuva de projéteis flamejantes |
| Shinobi \+ Vento | Tornado Cortante | **Fūton Shuriken Cyclone** | Tornado com shurikens gigantes. | dano contínuo. | tornado metálico |
| Shinobi \+ Terra | Enterro Ninja | **Doton Burial Grip** | Ryu emerge da terra e enterra inimigo. | **Hit Kill (não boss)** | terra abrindo |
| Shinobi \+ Ilusionista | Substituição Fantasma | **Mugen Kawarimi** | Evita dano e teleporta. | aplica **Confused** no atacante. | clone desaparecendo |
| Shinobi \+ Trovão | Dança das Lâminas Elétricas | **Raijin Blade Dance** | cortes ultra rápidos pelo mapa. | multi-hit global. | cortes com raio |

---

### **6.2.3 Arte Ninja das Sombras** {#6.2.3-arte-ninja-das-sombras}

| Combinação | Nome PT | Nome JP | Descrição | Gameplay | VFX |
| ----- | ----- | ----- | ----- | ----- | ----- |
| Sombras | Clonagem Sombria | **Kage Bunshin** | Cria 3 clones reais. | companions de combate. | clones negros |
| Sombras \+ Veneno | Clones Venenosos | **Dokukage Bunshin** | clones aplicam veneno. | morrem liberando gás. | fumaça verde |
| Sombras \+ Água | Vórtice Vampírico | **Suiton Blood Drain** | drena vida dos inimigos. | cura Ryu. | espiral aquática |
| Sombras \+ Fogo | Clones Explosivos | **Katon Shadow Bomb** | clones explodem ao morrer. | dano em área. | explosão flamejante |
| Sombras \+ Vento | Tornado do Abismo | **Kage Storm** | tornado negro que cega inimigos. | blind \+ knockback. | vento escuro |
| Sombras \+ Terra | Espinhos da Sombra | **Doton Shadow Spike** | espinhos surgem sob inimigos. | dano \+ stun. | pedras emergindo |
| Sombras \+ Ilusão | Teatro Sombrio | **Phantom Shadow Army** | clones criam clones ilusórios. | caos total. | múltiplos clones |
| Sombras \+ Trovão | Execução do Abismo | **Raijin Kage Prison** | prende alvo e invoca raio. | hit kill não boss. | sombra \+ relâmpago |

---

### **6.2.4 Arte Ninja do Veneno** {#6.2.4-arte-ninja-do-veneno}

| Combinação | Nome PT | Nome JP | Descrição | Gameplay | VFX |
| ----- | ----- | ----- | ----- | ----- | ----- |
| Veneno | Aura Tóxica | **Dokugiri Aura** | aura venenosa em volta de Ryu. | aplica Poisoned. | fumaça verde |
| Veneno \+ Água | Vórtice Venenoso | **Suiton Doku Spiral** | vortex que envenena e cura Ryu. | poison \+ heal. | água verde |
| Veneno \+ Fogo | Inferno Tóxico | **Katon Toxic Trail** | bola de fogo venenosa. | burn \+ poison. | fogo verde |
| Veneno \+ Vento | Explosão de Gás | **Doku Cyclone** | nuvem tóxica gigante. | área total. | nuvem venenosa |
| Veneno \+ Terra | Solo Corrompido | **Doton Poison Field** | chão venenoso. | quem pisa sofre poison. | terreno verde |
| Veneno \+ Ilusão | Ilusão Tóxica | **Mugen Poison Trap** | áreas falsas e reais. | jogador precisa identificar. | zonas verdes |
| Veneno \+ Trovão | Tempestade Tóxica | **Raijin Toxic Shock** | onda elétrica venenosa. | poison \+ electrocuted. | raio verde |

---

### **6.2.5 Arte Ninja da Água** {#6.2.5-arte-ninja-da-água}

| Combinação | Nome PT | Nome JP | Descrição | Gameplay | VFX |
| ----- | ----- | ----- | ----- | ----- | ----- |
| Água | Onda do Leviatã | **Suiton Leviathan Wave** | onda gigante empurra inimigos. | aplica Soaked. | tsunami |
| Água \+ Fogo | Pilar de Vapor | **Steam Burst** | explosão de vapor. | burn \+ soaked. | vapor |
| Água \+ Terra | Pântano Profundo | **Mud Prison** | inimigos presos na lama. | move speed 0\. | lama |
| Água \+ Vento | Tempestade Glacial | **Blizzard Strike** | gelo congela inimigos. | hp=1 freeze. | gelo |
| Água \+ Ilusão | Reflexos Assassinos | **Mirror Phantom** | reflexos viram clones. | clones inimigos. | reflexos |
| Água \+ Trovão | Tempestade Divina | **Raijin Storm** | chuva e raios constantes. | dano contínuo. | chuva \+ raios |

---

### **6.2.6 Arte Ninja do Fogo** {#6.2.6-arte-ninja-do-fogo}

| Combinação | Nome | JP | Descrição | Gameplay | VFX |
| ----- | ----- | ----- | ----- | ----- | ----- |
| Fogo | Esfera Infernal | **Katon Hellfire Orb** | bola de fogo explosiva. | burn \+ área. | explosão |
| Fogo \+ Vento | Inferno Giratório | **Infernal Tornado** | tornado de fogo. | burn contínuo. | tornado flamejante |
| Fogo \+ Terra | Erupção Vulcânica | **Volcanic Burst** | lava emerge do chão. | burn \+ stun. | lava |
| Fogo \+ Ilusão | Demônio Carmesim | **Infernal Oni** | invoca demônio de fogo. | summon. | oni flamejante |
| Fogo \+ Trovão | Tempestade da Destruição | **Thunderfire Barrage** | cortes elétricos flamejantes. | multi hit. | fogo \+ raio |

---

### **6.2.7 Arte Ninja do Vento** {#6.2.7-arte-ninja-do-vento}

| Combinação | Nome | JP | Descrição | Gameplay | VFX |
| ----- | ----- | ----- | ----- | ----- | ----- |
| Vento | Ciclone Cortante | **Blade Cyclone** | tornado arremessa inimigos. | knockback. | tornado |
| Vento \+ Terra | Tempestade de Areia | **Sandstorm** | ciclone de areia. | stun \+ knockback. | areia |
| Vento \+ Ilusão | Miragem Caótica | **Phantom Mirage** | clones ilusórios empurram inimigos. | confused. | miragens |
| Vento \+ Trovão | Furacão Elétrico | **Thunder Cyclone** | tornado elétrico. | electrocuted. | tornado elétrico |

---

### **6.2.8 Arte Ninja da Terra** {#6.2.8-arte-ninja-da-terra}

| Combinação | Nome | JP | Descrição | Gameplay | VFX |
| ----- | ----- | ----- | ----- | ----- | ----- |
| Terra | Espinhos da Montanha | **Stone Spike** | espinhos emergem do chão. | stun. | pedra |
| Terra \+ Ilusão | Gato da Fortuna | **Lucky Cat Prison** | gato gigante aprisiona inimigos. | condição aleatória. | lucky cat |
| Terra \+ Trovão | Tempestade de Vidro | **Glass Storm** | espinhos de vidro elétricos. | bleed \+ electrocuted. | vidro |

---

### **6.2.9 Arte Ninja do Ilusionista** {#6.2.9-arte-ninja-do-ilusionista}

| Combinação | Nome | JP | Descrição | Gameplay | VFX |
| ----- | ----- | ----- | ----- | ----- | ----- |
| Ilusionista | Teatro das Ilusões | **Mugen Bunshin** | clones ilusórios defensivos. | troca de posição. | clones espectrais |
| Ilusão \+ Trovão | Flash Fantasma | **Phantom Flash** | clones explodem em luz. | blind \+ electrocuted. | explosão de luz |

---

### **6.2.10 Arte Ninja do Trovão** {#6.2.10-arte-ninja-do-trovão}

| Combinação | Nome | JP | Descrição | Gameplay | VFX |
| ----- | ----- | ----- | ----- | ----- | ----- |
| Trovão | Dança do Relâmpago | **Lightning Dance** | dash entre inimigos cortando. | multi-target. | rastro elétrico |

---

## **6.3 Cores de cada Arte Ninja (UI)** {#6.3-cores-de-cada-arte-ninja-(ui)}

| Arte | Cor |
| ----- | ----- |
| Shinobi | branco |
| Sombras | roxo |
| Veneno | verde |
| Água | azul |
| Fogo | vermelho |
| Vento | ciano |
| Terra | marrom |
| Ilusionista | magenta |
| Trovão | amarelo |

---

## **6.4 Ícones sugeridos** {#6.4-ícones-sugeridos}

| Arte | Ícone |
| ----- | ----- |
| Shinobi | shuriken |
| Sombras | lua |
| Veneno | frasco |
| Água | onda |
| Fogo | chama |
| Vento | tornado |
| Terra | montanha |
| Ilusão | máscara |
| Trovão | raio |

---

## **6.5 Raridade de Jutsus** {#6.5-raridade-de-jutsus}

| Tier | Chance |
| ----- | ----- |
| Comum | 50% |
| Raro | 30% |
| Épico | 15% |
| Lendário | 5% |

---

## **6.6 Sistema de Upgrade de Jutsu** {#6.6-sistema-de-upgrade-de-jutsu}

Jutsus podem subir até **nível 5**.

| Nível | Efeito |
| ----- | ----- |
| 1 | jutsu base |
| 2 | \+20% dano |
| 3 | nova propriedade |
| 4 | \+30% duração |
| 5 | efeito especial |

## 

## **7\. Condições Elementais (Status Effects)**

Condições podem ser aplicadas por player e inimigos. Na implementação, os nomes devem ser em inglês.

| Condition (EN) | Efeito |
| :---- | :---- |
| Burning | Aplica dano por segundo baseado na resistência da vítima. |
| Poisoned | Aplica 1 de dano toda vez que se mexe ou ataca. |
| Soaked | Diminui drasticamente a velocidade de movimento. |
| Bleeding | Reduz ataque e defesa enquanto durar. |
| Blind | Incapaz de localizar adversário (player: shader escurece a tela). |
| Stunned | Adversário inerte pelo tempo que durar. |
| Confused | Troca facção para CONFUSED: ataca qualquer um próximo. |
| Electrocuted | Anda lento, dano por segundo, transmite a aliados próximos. |

# **8\. Equipamentos (Slots e Bônus)**

Equipamentos são como em Absolum: adicionam apenas status. Não é permitido equipar dois itens do mesmo slot, mas é permitido substituir.

## **8.1 Slots**

| Slot | Temática |
| :---- | :---- |
| Head | Máscaras ninja, kabuto leve, bandanas sagradas |
| Chest | Peitoral lamelar, do-maru, jaqueta shinobi |
| Shoulders | Ombreiras leves, placas samurai |
| Arms | Luvas, tekko, braceletes |
| Waist | Obi/cinto, pouch de ferramentas |
| Legs | Hakama reforçado, grevas leves |
| Feet | Waraji, tabi, sandálias shinobi |

## **8.2 Sets de Equipamentos**

| Set | Identidade |
| ----- | ----- |
| Shinobi | dano físico / críticos |
| Sombras | evasão / cegueira |
| Tempestade | velocidade / eletrocuted |
| Inferno | burn |
| Guardião da Terra | defesa / stun |

Cada set terá **7 peças** (uma por slot).

Total: **35 equipamentos**

---

## **8.3 Tabela de Equipamentos** {#8.3-tabela-de-equipamentos}

| Nome | Tipo | Efeito | Raridade | Set |
| ----- | ----- | ----- | ----- | ----- |
| Bandana Shinobi | Cabeça | \+10 Agility | Comum | Shinobi |
| Máscara do Assassino | Cabeça | \+5% Critical Chance | Raro | Shinobi |
| Capuz das Sombras | Cabeça | \+10% Evasion | Raro | Sombras |
| Elmo do Guardião | Cabeça | \+15 Defense | Comum | Guardião da Terra |
| Máscara da Tempestade | Cabeça | \+5% Attack Speed | Épico | Tempestade |
| Máscara do Oni Flamejante | Cabeça | \+10% Burn Damage | Épico | Inferno |

| Nome | Tipo | Efeito | Raridade | Set |
| ----- | ----- | ----- | ----- | ----- |
| Armadura Shinobi | Peitoral | \+25 HP | Comum | Shinobi |
| Armadura das Sombras | Peitoral | \+10% Evasion | Raro | Sombras |
| Peitoral do Guardião | Peitoral | \+20 Defense | Raro | Guardião da Terra |
| Casaco da Tempestade | Peitoral | \+10% Lightning Damage | Épico | Tempestade |
| Armadura do Inferno | Peitoral | ataques aplicam Burn | Épico | Inferno |

| Nome | Tipo | Efeito | Raridade | Set |
| ----- | ----- | ----- | ----- | ----- |
| Ombreiras Shinobi | Ombros | \+10 Strength | Comum | Shinobi |
| Ombreiras da Sombra | Ombros | \+5% Dodge Chance | Raro | Sombras |
| Ombreiras da Terra | Ombros | \+15 Defense | Raro | Guardião da Terra |
| Ombreiras do Trovão | Ombros | \+5% Attack Speed | Épico | Tempestade |
| Ombreiras do Inferno | Ombros | \+10% Burn Damage | Épico | Inferno |

| Nome | Tipo | Efeito | Raridade | Set |
| ----- | ----- | ----- | ----- | ----- |
| Braçadeiras Shinobi | Braços | \+5% Quick Attack Damage | Raro | Shinobi |
| Luvas das Sombras | Braços | ataques têm chance de aplicar Blind | Épico | Sombras |
| Manoplas da Terra | Braços | ataques pesados têm chance de Stun | Raro | Guardião da Terra |
| Luvas da Tempestade | Braços | ataques aplicam Electrocuted | Épico | Tempestade |
| Luvas Flamejantes | Braços | ataques aplicam Burning | Épico | Inferno |

| Nome | Tipo | Efeito | Raridade | Set |
| ----- | ----- | ----- | ----- | ----- |
| Faixa Shinobi | Cintura | \+10 Strength | Comum | Shinobi |
| Faixa Sombria | Cintura | \+5% Critical Chance | Raro | Sombras |
| Cinto da Terra | Cintura | \+15 Defense | Raro | Guardião da Terra |
| Cinto do Trovão | Cintura | \+10% Dash Speed | Épico | Tempestade |
| Cinto do Inferno | Cintura | \+10% Burn Duration | Épico | Inferno |

| Nome | Tipo | Efeito | Raridade | Set |
| ----- | ----- | ----- | ----- | ----- |
| Calças Shinobi | Pernas | \+10 Agility | Comum | Shinobi |
| Calças das Sombras | Pernas | \+10% Evasion | Raro | Sombras |
| Grevas da Terra | Pernas | \+15 Defense | Raro | Guardião da Terra |
| Grevas Relâmpago | Pernas | Dash Distance \+20% | Épico | Tempestade |
| Calças do Inferno | Pernas | \+10% Fire Damage | Épico | Inferno |

| Nome | Tipo | Efeito | Raridade | Set |
| ----- | ----- | ----- | ----- | ----- |
| Sandálias Shinobi | Pés | \+10 Agility | Comum | Shinobi |
| Sandálias Fantasma | Pés | Dash Cooldown \-20% | Raro | Sombras |
| Botas da Montanha | Pés | \+15 Defense | Comum | Guardião da Terra |
| Botas do Trovão | Pés | \+10% Movement Speed | Épico | Tempestade |
| Sandálias de Brasas | Pés | deixa rastro de fogo ao dash | Épico | Inferno |

---

## **8.4 Bônus de Sets** {#8.4-bônus-de-sets}

| Set | 3 Peças | 5 Peças |
| ----- | ----- | ----- |
| Shinobi | \+10% Attack Damage | \+15% Critical Chance |
| Sombras | \+15% Evasion | ataques contra inimigos cegos causam \+30% dano |
| Tempestade | \+15% Movement Speed | ataques geram chain lightning |
| Inferno | \+20% Burn Damage | inimigos queimando explodem ao morrer |
| Guardião da Terra | \+20% Defense | bloqueios criam ondas de choque |

---

### **Observação importante de lore** {#observação-importante-de-lore}

A **Armadura Sagrada do Raijin** funciona diferente:

* recuperada ao longo da história

* buffs permanentes

* **não ocupa slots de equipamento**

* buffs sempre ativos

Exemplo de buffs narrativos possíveis:

| Parte recuperada | Buff |
| ----- | ----- |
| Elmo | \+10% Jutsu Damage |
| Peitoral | \+20 HP |
| Braços | \+10% Attack Speed |
| Pernas | \+15 Agility |
| Núcleo | desbloqueia Arte Ninja do Trovão |

## 

## **9\. Arremessáveis e Ferramentas Ninja**

Arremessáveis são consumíveis de combate que criam janelas de controle, utilidade e aplicação de condições. Eles podem disparar técnicas com evento Use Throwable.

## **9.1 Regras** {#9.1-regras}

| Regra | Descrição |
| ----- | ----- |
| Uso | Consumível |
| Stack | pode carregar múltiplos |
| Uso | cada item pode ser usado apenas uma vez |
| Drop | recompensas, mercado ou baús |
| Trigger | ativa instantaneamente |

---

## **9.2 Tabela de Arremessáveis** {#9.2-tabela-de-arremessáveis}

| Nome | Tipo | Efeito | Uso Tático | VFX |
| ----- | ----- | ----- | ----- | ----- |
| Kunai | Projétil | lança uma kunai no inimigo mais próximo | dano rápido e preciso | trilha metálica |
| Shuriken | Projétil | atinge inimigo distante em arco | bom contra inimigos à distância | giro metálico |
| Bomba de Fumaça | Utilitário | cria nuvem onde Ryu fica indetectável | reposicionamento | fumaça cinza |
| Selo Explosivo | Armadilha | coloca explosivo no chão | controle de área | explosão |
| Makibishi | Armadilha | espalha espinhos no chão atrás de Ryu | controle de perseguição | espinhos metálicos |
| Bomba de Veneno | Área | cria nuvem tóxica | controle de multidão | gás verde |
| Lanterna Cegante | Controle | explode em luz cegando inimigos | crowd control | flash branco |
| Bomba Elétrica | Área | choque elétrico em área | aplica Electrocuted | arco elétrico |

---

## **9.3 Distribuição de raridade** {#9.3-distribuição-de-raridade}

| Item | Raridade |
| ----- | ----- |
| Kunai | Comum |
| Shuriken | Comum |
| Makibishi | Comum |
| Selo Explosivo | Raro |
| Bomba de Fumaça | Raro |
| Bomba de Veneno | Raro |
| Lanterna Cegante | Épico |
| Bomba Elétrica | Épico |

---

## **9.4 Preço sugerido no Mercado** {#9.4-preço-sugerido-no-mercado}

| Item | Preço |
| ----- | ----- |
| Kunai | 10 gold |
| Shuriken | 12 gold |
| Makibishi | 15 gold |
| Selo Explosivo | 25 gold |
| Bomba de Fumaça | 30 gold |
| Bomba de Veneno | 30 gold |
| Lanterna Cegante | 40 gold |
| Bomba Elétrica | 45 gold |

## **10\. Mapa de Nodes e Tipos de Encontro** {#10.-mapa-de-nodes-e-tipos-de-encontro}

Cada bioma apresenta um mapa ramificado estilo Slay The Spire.

| Node | Descrição |
| :---- | :---- |
| Combat | Cenário principal com travessia, inimigos e armadilhas. |
| Elite | Cenário curto com inimigo mais forte; recompensa melhor. |
| Meditation | Recupera vida OU aprende nova técnica. |
| Treasure | Waves de inimigos; ao final ganha um tesouro. |
| Market | Comerciante: compra de equipamentos (Gold). |
| Boss | Encontro final do bioma. |

# **11\. Economia e Meta-progresso**

O jogo possui economia de run (Gold) e economia persistente (Scrolls/Crystals).

| Recurso | Uso |
| :---- | :---- |
| Gold | Gasto no Market. Não cumulativo. |
| Secret Scrolls | Compra de técnicas, reroll, upgrade. Cumulativo. |
| Battle Experience | Progresso de combate que gera cristais. Cumulativo. |
| Experience Crystals | Up de stats do personagem. Cumulativo. |

# **12\. Stats do Personagem**

| Stat | Efeito |
| :---- | :---- |
| Constitution | HP máximo e resistência geral. |
| Strength | Dano físico base. |
| Chakra | Poder/escala de jutsus e resistências elementais (a definir). |
| Defense | Redução de dano e poise. |
| Agility | Velocidade, mobilidade e janelas de invulnerabilidade. |
| Luck | Chance de crit/recompensas/qualidade de loot (balance). |

# **13\. Artes Ninja (9 Escolas)**

Artes Ninja definem o foco de build, pool de jutsus e conjunto de técnicas disponíveis.

| Arte | Foco | Condição-chave | Identidade |
| :---- | :---- | :---- | :---- |
| Arte Ninja do Shinobi | Começa com ela. Foco em ataques físicos e Bleeding. | Bleeding | Dano consistente e pressão |
| Arte Ninja das Sombras | Evasão, clonagem e Blind. | Blind | Stealth, mobilidade |
| Arte Ninja do Veneno | Aplicação de Poisoned e controle gradual. | Poisoned | DPS por ação, desgaste |
| Arte Ninja da Água | Cura e Soaked. | Soaked | Sustentação e controle de velocidade |
| Arte Ninja do Fogo | Aumento de dano e Burning. | Burning | Burst e pressão |
| Arte Ninja do Vento | Controle de área e ataques a distância. | — | Zoning e crowd control |
| Arte Ninja da Terra | Defesa e Stunned. | Stunned | Tank e controle |
| Arte Ninja do Ilusionista | Confusão e Confused. | Confused | Caos tático |
| Arte Ninja do Trovão | Velocidade e Electrocuted. Desbloqueia ao completar armadura. | Electrocuted | Alta mobilidade e propagação |

# **14\. Lore Completa**

Um filhote de gato preto é deixado às portas de um templo em uma vila de gatos ninjas. O mestre do templo encontra o pequenino, o acolhe e o treina. Assim surge o talentosíssimo RYU.

Anos depois, o jovem Ryu precisa enfrentar sua provação final para ser aceito oficialmente no Clã dos Gatos Ninjas das Sombras. Esta provação acontece na primeira fase/bioma do jogo, fora do loop roguelike, funcionando como tutorial. Ao concluir o desafio, Ryu enfrenta seu mestre e aprende a Arte Ninja das Sombras.

Após vencer, Ryu sobe a montanha para gravar seu nome em uma pedra no topo. Porém, à noite, ao retornar, encontra seu vilarejo em chamas e vários companheiros mortos. Ao chegar ao templo, encontra seu mestre mortalmente ferido.

Antes de morrer, o mestre revela que outros clãs de animais se uniram para atacar a vila e roubar a Armadura Sagrada. Ele pede a Ryu que recupere a armadura. Em seu último ato, o mestre toca a testa de Ryu e executa um jutsu misterioso, sacrificando sua própria força vital.

Algumas semanas depois, Ryu decide partir em busca de vingança. No momento em que seu coração se prepara para a jornada, o jutsu do mestre se ativa: o templo torna-se o hub e a linha do tempo daquele dia é selada em um loop. O loop reinicia sempre que Ryu morre ou o dia termina — mas apenas Ryu mantém consciência e memórias.

Ryu parte para sua primeira parada: o clã dos répteis. Lá, é morto em combate por ainda não possuir força suficiente. Para sua surpresa, ele acorda no templo novamente, na manhã daquele mesmo dia, carregando as memórias e experiências.

Após morrer mais vezes, uma versão fantasmagórica de seu mestre surge e revela a verdade: o jutsu foi criado para permitir que Ryu acumulasse poder e experiência para enfrentar os clãs rivais e recuperar a Armadura Sagrada. Porém, isso também significa que ele precisa realizar tudo em apenas um dia, repetido infinitas vezes.

Depois de muitas mortes e treinos, Ryu derrota a mestra Camaleoa do clã dos lagartos e aprende a Arte Ninja do Veneno. A cada novo mestre derrotado, Ryu aprende uma nova Arte Ninja.

Ordem resumida de conquistas: começa como Shinobi; derrota seu mestre e recebe Sombras; derrota a Camaleoa e ganha Veneno; derrota o Sapo e ganha Água; derrota o Orangotango e ganha Fogo; derrota a Garça e ganha Vento; derrota o Javali e ganha Terra.

Ao chegar à Torre Alpha, Ryu enfrenta o guarda-costas do vilão final, o cão Alpha. O guarda-costas luta com duas katanas e usa as Artes Ninja do Ilusionista e do Shinobi. Ao ser derrotado, seu disfarce se desfaz: ele não é um cachorro, mas sim outro gato do Clã das Sombras — o discípulo anterior do mestre.

Ryu o enfrenta novamente. Agora, além do estilo Ilusionista, ele usa também o estilo das Sombras. Ao vencê-lo, Ryu recebe a Arte Ninja do Ilusionista e revela que Alpha foi responsável pela morte de seu mestre. O gato admite ter sido enganado.

Alpha, que assistia à luta, percebe que ambos podem se unir contra ele e foge. Ryu, agora em posse da Armadura completa, parte para a última fase: a Fábrica de Armas. Lá, enfrenta capangas armados com armas de fogo.

No final, Ryu enfrenta o próprio Alpha e depois Alpha em uma Armadura Meca. Ao derrotá-lo, o dia é libertado do loop, e Ryu retorna à vila dos gatos, encerrando a história.

# **15\. Biomas e Bosses**

Biomas são territórios de clãs. Cada bioma tem mapa de nodes e culmina em um boss (mestre).

| Bioma | Tema | Boss |
| :---- | :---- | :---- |
| Bioma 1 (Tutorial) | Vila/Templo — Provação final e aprendizado das Sombras | Mestre do Templo (Gato) |
| Bioma 2 | Clã dos Répteis | Mestra Camaleoa (Veneno) |
| Bioma 3 | Clã dos Anfíbios | Boss Sapo (Água) |
| Bioma 4 | Clã dos Primatas | Orangotango (Fogo) |
| Bioma 5 | Clã das Aves | Garça (Vento) |
| Bioma 6 | Clã dos Canídeos/Javali | Javali (Terra) |
| Bioma 7 | Torre Alpha | Guarda-costas (Ilusionista/Shinobi) |
| Bioma 8 | Fábrica de Armas | Alpha \+ Alpha Meca |

# **16\. Hub (Templo)**

O templo funciona como hub entre runs.

* Selecionar Jutsu inicial e iniciar run  
* Up de stats com Experience Crystals  
* Comprar/upgrade de técnicas com Secret Scrolls (quando desbloqueado)  
* Treino / tutorial / sala de testes (opcional)

# **17\. Direção de Arte e UX/HUD**

## **17.1 Direção de Arte**

Estilo anime/cel-shaded. Personagens em sprites com proporções marcantes (Ryu com mãos/antebraços grandes) e cenários 3D estilizados. VFX elementais claros para leitura de status.

## **17.2 HUD (proposta)**

* Barra de HP  
* Indicador de Chakra (ou energia de jutsu, se aplicável)  
* Slots de Jutsu (1 e 2\) \+ indicador do Jutsu combinado (3)  
* Ícones de condições elementais ativas  
* Gold (run) \+ Secret Scrolls (meta)  
* Mini-map / progresso do node atual (opcional)

# **18\. Requisitos Técnicos e Nomenclaturas**

Condições e nomes internos devem ser em inglês para consistência no código.

Facção especial: CONFUSED (entidade confusa ataca qualquer alvo próximo).

O jogo deve evitar strings hardcoded para facilitar localização (usar IDs/ScriptableObjects/loc tables).

# **19\. Status do Projeto**

GDD reestruturado para o novo core (Beat’n Up Roguelike). Próximos itens sugeridos: lista completa de Técnicas e Jutsus por Arte Ninja, raridades, tabelas de loot e balance inicial.