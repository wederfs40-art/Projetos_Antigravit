# Histórico Completo de Desenvolvimento: Simulador de Tiro Esportivo (IPSC) & Combate Tático

> **Projeto:** Simulador de Tiro em Primeira Pessoa (FPS) Ultra Realista  
> **Motor Gráfico:** Unity 3D (C#)  
> **Foco:** Balística Realista, Tiro Esportivo (Regras Oficiais IPSC / Clube de Tiro) e Combate Tático  
> **Repositório GitHub:** `JogoTiroEsportivo`  
> **Data de Criação Inicial:** Novembro de 2026  

---

## 📌 Sumário Executivo

Este documento registra cronologicamente todas as decisões de arquitetura de software, sistemas implementados, códigos-fonte criados, ferramentas procedurais e orientações de montagem desenvolvidas para este simulador. O objetivo foi criar uma base sólida, altamente modular e pronta para produção, que replique com fidelidade física o comportamento de armas de fogo, o cálculo balístico e as regras esportivas de uma pista de IPSC.

---

## 🏗️ 1. Arquitetura dos 4 Sistemas Núcleo

O projeto foi dividido em quatro subsistemas desacoplados que se comunicam através de eventos estáticos em C# (`Action`), minimizando dependências diretas (*loose coupling*):

```text
[ WeaponHandling.cs ] (Entrada do Jogador, ADS, Balanço de Respiração e Recuo Procedural)
        │
        ▼
   [ Weapon.cs ] ──(Ao disparar)──► [ RangeOfficerTimer.cs ] (Registra Splits e Shots)
        │                                    ▲
        │ (Instancia no Muzzle Point)        │ (Notifica pontuação do alvo)
        ▼                                    │
   [ Bullet.cs ] ────(Raycast Contínuo)──► [ IPSCTargetZone.cs / IPSCTarget.cs ]
                                             (Gera Furo / Decalque no Alvo)
```

### Sistema 1: Balística Realista (`Bullet.cs` e `Weapon.cs`)
- **Problema resolvido:** Em jogos de tiro convencionais, os tiros saem do centro geométrico da câmera do jogador (como um laser invisível) e balas rápidas frequentemente sofrem de *tunneling* (atravessam alvos finos sem colidir).
- **Implementação:**
  - **Height Over Bore:** O projétil nasce no `MuzzlePoint` (ponta real do cano físico), localizado alguns centímetros abaixo da linha de visada óptica. A curta distância (ex: 2 a 5 metros), o ponto de impacto (*POI*) atinge o alvo fisicamente abaixo do ponto visado (*POA*), reproduzindo a dificuldade real de tiro esportivo.
  - **Detecção Contínua Anti-Tunneling:** O script calcula o deslocamento vetorial a cada frame e projeta um `Physics.Raycast` entre a posição anterior e a nova posição.
  - **Balística Externa:** Gravidade real aplicada progressivamente ao vetor de velocidade da bala em metros por segundo (ex: 380 m/s para munição 9x19mm Parabellum).
  - **Slide Lock:** Ao disparar os 17 tiros do carregador, o ferrolho trava aberto mecanicamente e tentativas subsequentes de disparo geram cliques secos de câmara vazia (*Dry Fire*). Pressionar a tecla `R` recarrega a arma e fecha o ferrolho.

### Sistema 2: Visada, Respiração e Recuo (`WeaponHandling.cs`)
- **ADS (Aim Down Sights):** Ao segurar o botão direito do mouse, a arma é interpolada suavemente de sua posição de quadril para a linha central dos olhos do jogador, alinhando a massa de mira frontal com a alça traseira.
- **Weapon Sway (Balanço de Respiração):** Modelado usando curvas harmônicas compostas de Lissajous (`Mathf.Sin` e `Mathf.Cos`), simulando a oscilação natural do tórax humano.
- **Prender a Respiração:** Segurar a tecla `Shift Esquerdo` contrai os músculos do atirador, reduzindo o balanço da mira em 80% (`breathHoldMultiplier = 0.2f`).
- **Recuo Procedural com Amortecimento:** O disparo aciona um impulso físico para trás (eixo Z) e uma elevação angular de cano (*muzzle climb* no eixo X com leve desvio lateral aleatório). Um algoritmo elástico (*Spring/Lerp Dampening*) recupera gradualmente a postura original da arma.

### Sistema 3: Alvos de Papelão IPSC (`IPSCTarget.cs` e `IPSCTargetZone.cs`)
- **Colisores de Zonas Oficiais:**
  - **Zona Alpha (A):** Centro do alvo (15 x 32 cm) $\rightarrow$ **+5 pontos**.
  - **Zona Charlie (C):** Zona intermediária $\rightarrow$ **+3 pontos** (Fator Minor).
  - **Zona Delta (D):** Extremidades $\rightarrow$ **+1 ponto**.
  - **No-Shoot (NS):** Alvo branco de penalidade $\rightarrow$ **-10 pontos** por disparo recebido.
- **Decalques Orientados:** Ao receber o impacto da bala, instancia um decalque de furo de bala na coordenada exata do contato, orientado pela normal da malha e anexado como filho do alvo.
- **Função de Obreamento (`ObrearAlvo`):** Destrói todos os decalques de papel e zera a lista de impactos registrados, simulando os adesivos (obreias) usados para reutilizar alvos em estandes reais.

### Sistema 4: Shot Timer e Juiz de Pista (`RangeOfficerTimer.cs`)
- **Comandos de Pista Oficiais:**
  - Pressionar **T** inicia o protocolo: *"Are you ready? ... Standby!"*.
  - O sistema aguarda um intervalo de tempo aleatório entre 1.0 e 4.0 segundos (tempo regulamentar IPSC para evitar que o atirador antecipe a largada).
  - Um som estridente de **BEEP** (Shot Timer Buzzer) soa e o cronômetro começa a correr em centésimos de segundo.
- **Registro de Splits:** A cada disparo detectado pelo evento da arma, registra-se o tempo decorrido do último tiro (*split*), permitindo análise de cadência.
- **Apuração de Misses:** Alvos que não receberem o número mínimo regulamentar de impactos (padrão: 2 tiros por alvo de papel) são computados automaticamente como **Miss (-10 pontos cada)**.
- **Cálculo do Hit Factor (HF):**
  $$\text{Hit Factor} = \frac{\text{Pontos Líquidos (Alphas + Charlies + Deltas) } - \text{ Penalidades (Misses + No-Shoots)}}{\text{Tempo Total de Prova (segundos)}}$$

---

## 🛠️ 2. Scripts Adicionais e Construtores

Para que o projeto pudesse ser jogado imediatamente sem a necessidade de pacotes externos ou configuração manual complexa, foram implementados módulos extras:

| Script | Caminho | Finalidade |
| :--- | :--- | :--- |
| `PlayerController.cs` | `Assets/Scripts/Player/` | Locomoção FPS em 1ª pessoa (andar, correr, mouse look com clamp vertical). |
| `IPSCUIManager.cs` | `Assets/Scripts/UI/` | HUD completo em tela: cronômetro dinâmico, último split, contador de munição e painel de resultado com Hit Factor. |
| `ProceduralAudio.cs` | `Assets/Scripts/Utilities/` | Síntese de áudio matemática em tempo de execução: bipe senoidal de 2.75 kHz, estampido de tiro 9mm, trava de ferrolho e clique seco de percussor. |
| `ProceduralMaterials.cs` | `Assets/Scripts/Utilities/` | Geração procedural de texturas de papelão com linhas de zonas IPSC e furos de bala transparentes. |
| `SceneGeneratorCore.cs` | `Assets/Scripts/Setup/` | Construtor que gera o estande de tiro completo (chão, barreiras de segurança, para-balas, alvos e pistola 3D funcional). |
| `IPSCSceneBuilder.cs` | `Assets/Scripts/Editor/` | Adiciona o menu **`IPSC Simulator > Montar Pista Completa (Auto-Setup)`** na barra superior do Unity Editor. |
| `IPSCAutoBootstrap.cs` | `Assets/Scripts/Setup/` | Gatilho para montar automaticamente o cenário caso a cena esteja vazia ao dar Play. |

---

## 🕹️ 3. Mapeamento de Teclas e Controles

| Entrada | Ação |
| :--- | :--- |
| **W, A, S, D** | Movimentação pelo estande de tiro |
| **Mouse (Eixos X/Y)** | Controle de mira e visão da câmera |
| **Botão Direito do Mouse (Hold)** | **Mira ADS**: Alinha alça, massa de mira e alvo |
| **Shift Esquerdo (Hold)** | **Prender a Respiração**: Amortece o balanço natural |
| **Botão Esquerdo do Mouse** | **Disparar**: Aciona recuo, split, furo no alvo e som |
| **R** | **Recarregar**: Alimenta 17 tiros e destrava o ferrolho |
| **T** | **Range Officer**: Dá *"Standby"*, aguarda delay e soa o **BEEP** |
| **Y** | **Finalizar Pista**: Para o cronômetro e gera o cálculo do **Hit Factor** |
| **U** | **Obrear Alvos**: Limpa todos os furos de bala e zera pontuações |
| **ESC** | Libera ou bloqueia a trava do cursor do mouse |

---

## 🌐 4. Integração com a Unity Asset Store

Durante o planejamento visual, foi orientado o uso de pacotes 3D gratuitos na **Unity Asset Store**:
- **Pacote Selecionado:** *`Pacote de Armas de Pistola Low-Poly Weapon Pack 1`* (da CASTELO BRAVO).
- **Motivo da Escolha:** Traz modelos de pistolas semiautomáticas (estilos Glock e 1911/2011) que se alinham perfeitamente à divisão esportiva de IPSC Production e Standard, permitindo que a armação, ferrolho móvel e mira sejam mapeados diretamente nos scripts desenvolvidos.

---

## 🚀 5. Preparação para o GitHub

Para subir este repositório no GitHub sem carregar arquivos temporários gigantescos do cache da Unity (que podem ultrapassar dezenas de gigabytes desnecessariamente), foi configurado um arquivo `.gitignore` específico para projetos Unity.

### O que sobe para o GitHub:
- `Assets/` (Scripts, Texturas, Modelos, Cenas, Sons).
- `ProjectSettings/` (Configurações de tags, layers, física e inputs).
- `Packages/manifest.json` (Gerenciador de pacotes da Unity).
- `README_PROJETO.md` e `HISTORICO_DESENVOLVIMENTO.md`.

### O que fica de fora (ignorado automaticamente pelo `.gitignore`):
- `Library/` (Cache gerado localmente pelo motor).
- `Temp/` e `Obj/` (Arquivos de compilação temporários).
- `Logs/` e `Build/`.
- Arquivos de solução do Visual Studio (`*.sln`, `*.csproj`).
