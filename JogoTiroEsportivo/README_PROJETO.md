# Simulador de Tiro Esportivo (IPSC) & Combate Tático - Unity C#

Projeto avançado de simulação balística realista, manuseio tático de armas de fogo, regras e pontuação oficiais de IPSC (Hit Factor, Splits, Zonas Alpha/Charlie/Delta e No-Shoot), cronômetro oficial (Shot Timer), alvos metálicos reativos e múltiplos ambientes de treinamento esportivo e combate CQB.

---

## ⚡ Como Rodar o Jogo em 1 Clique (Auto-Setup)

Você **não precisa modelar nem configurar nada manualmente**:

1. Abra o projeto no **Unity Editor** (versão recomendada: **Unity 6.3 LTS `6000.3.29f1`** ou superior com `-force-d3d11`).
2. Aperte o botão **PLAY (▶️)** no topo da Unity!
3. O **Menu Principal** abrirá automaticamente na tela, permitindo que você:
   - Escolha entre **5 Ambientes de Treinamento e Combate**.
   - Escolha sua **Arma** (Pistola 9mm, Revólver Dan Wesson .357 ou SMG Tática).
   - Clique em **"INICIAR SIMULAÇÃO"** para entrar na pista!

> **Dica para o Editor:** Você também pode carregar qualquer um dos cenários diretamente na viewport da Scene clicando no menu superior:  
> 👉 **`IPSC Simulator` > `Cenários Diretos` > [Escolha o Mapa]**

---

## 🗺️ Ambientes Disponíveis

| Ambiente | Tipo | Características Principais |
| :--- | :--- | :--- |
| **IPSC Outdoor - Pista Curta** | Esportivo | 12 disparos, shooting box com faixa zebrada, tambores azuis de 55 galões, mesa de segurança e alvos de papelão a 7m, 12m e 16m. |
| **IPSC Outdoor - Pista Média** | Esportivo | 24 disparos, labirinto com barricadas de madeira ripada (*Vision Barriers*), múltiplos ângulos de engajamento e poppers metálicos. |
| **IPSC Outdoor - Pista Longa** | Esportivo | 32 disparos, estande de 30 metros de profundidade, para-balas elevados, alvos móveis/distantes e bateria de poppers de aço. |
| **Estande Indoor de Precisão** | Clube Fechado | Piso de concreto polido escuro, paredes acústicas, 4 cabines individuais de tiro com marcadores de distância a 5m, 10m, 15m, 20m e 25m. |
| **Killhouse Tático CS-Style** | Combate CQB | Cenário fechado inspirado em mapas táticos com contêineres de carga de 6m (vermelhos e azuis), caixas militares de suprimentos, tambores de óleo, esquinas de varredura (*corner clearing*) e silhuetas de ameaça hostil mescladas com reféns civis (*No-Shoot*). |

---

## 🔫 Arsenal Disponível

1. **Pistola Tática 9mm:**
   - 17 disparos por carregador, semiautomática, excelente equilíbrio de recuo e cadência, mira com massa de fibra ótica de alta visibilidade.
2. **Revólver Dan Wesson 715 .357 Magnum:**
   - 6 disparos no tambor, modo esportivo, som encorpado de grande calibre, alto recuo vertical e elevado fator de impacto balístico.
3. **Submetralhadora Tática (SMG):**
   - 30 disparos por carregador, tiro totalmente automático contínuo a 550 RPM, recuo progressivo acumulativo para combate em curta distância.

---

## 🎮 Controles e Teclas da Pista

| Tecla / Ação | Função |
| :--- | :--- |
| **W, A, S, D** | Movimentação pelo cenário |
| **Mouse** | Olhar / Mirar a visão |
| **Botão Direito (Segurar)** | **ADS (Aim Down Sights)**: Alinha alça, massa de mira e alvo perfeitamente |
| **Shift Esquerdo (Segurar)** | **Prender a Respiração**: Amortece o balanço natural em 80% |
| **Botão Esquerdo** | **Disparar**: Semiautomático ou Automático contínuo (SMG) |
| **R** | **Recarregar**: Alimenta munição completa e fecha o ferrolho |
| **T** | **Comando do RO (Range Officer)**: Dá *"Standby"*, aguarda delay aleatório e soa o **BEEP!** |
| **Y** | **Encerrar Pista**: Para o cronômetro, soma pontos/penalidades e exibe o **Hit Factor** |
| **U** | **Obrear / Resetar**: Remove furos de bala e levanta os poppers de aço caídos |
| **ESC** | **Menu de Pausa**: Permite alternar mapa, trocar de arma ou reiniciar sem fechar o jogo |

---

## 📁 Estrutura de Código

- `Assets/Scripts/Ballistics/Bullet.cs` - Balística contínua por Raycast (anti-tunneling) e gravidade física real.
- `Assets/Scripts/Weapons/Weapon.cs` - Cano no Muzzle Point (Height-Over-Bore), munição, slide lock e cadência automática/semi.
- `Assets/Scripts/Weapons/WeaponHandling.cs` - Visada precisa (ADS), respiração harmônica e recuo elástico amortecido.
- `Assets/Scripts/Targets/IPSCTarget.cs` & `IPSCTargetZone.cs` - Zonas pontuadas oficiais (A, C, D e NS), decalques de furos de bala e obreamento.
- `Assets/Scripts/Targets/IPSCSteelPopper.cs` - Pratos metálicos reativos com física de queda (-85°), áudio metálico procedural e reset.
- `Assets/Scripts/Match/RangeOfficerTimer.cs` - Cronometragem oficial em centésimos, splits entre tiros e cálculo do Hit Factor.
- `Assets/Scripts/Match/TacticalGameManager.cs` - Singleton de gerenciamento de estados (`MainMenu`, `Playing`, `Paused`), transição de mapas e cursor.
- `Assets/Scripts/Match/GameEnums.cs` - Enumerações oficiais de mapas, armas e estados.
- `Assets/Scripts/UI/MainMenuManager.cs` - Menu Principal militar e Menu de Pausa (`ESC`).
- `Assets/Scripts/UI/IPSCUIManager.cs` - HUD do cronômetro, munição, splits e pontuação final.
- `Assets/Scripts/Player/PlayerController.cs` - Locomoção e câmera em 1ª pessoa.
- `Assets/Scripts/Utilities/ProceduralAudio.cs` - Síntese matemática de áudios (bipe IPSC, tiros 9mm, .357 Magnum, SMG e impactos metálicos).
- `Assets/Scripts/Utilities/ProceduralMaterials.cs` - Geração PBR de texturas para alvos, madeira, asfalto, tambores, caixas e aço.
- `Assets/Scripts/Setup/SceneGeneratorCore.cs` & `IPSCAutoBootstrap.cs` - Construtores de ambientes e arsenal.
- `Assets/Scripts/Editor/IPSCSceneBuilder.cs` - Atalhos do menu superior do Unity Editor.
