# Histórico Completo de Desenvolvimento: Simulador de Tiro Esportivo (IPSC) & Combate Tático

> **Projeto:** Simulador de Tiro em Primeira Pessoa (FPS) Ultra Realista  
> **Motor Gráfico:** Unity 3D (C#) — Compatível com Unity 2022 LTS e Unity 6 (6000.x)  
> **Foco:** Balística Realista, Tiro Esportivo (Regras Oficiais IPSC / Clube de Tiro) e Combate Tático  
> **Repositório GitHub:** `JogoTiroEsportivo`  
> **Data de Atualização:** 25 de Novembro de 2026  

---

## 📌 Sumário Executivo

Este documento registra cronologicamente todas as decisões de arquitetura de software, sistemas implementados, códigos-fonte criados, ferramentas procedurais, resolução de problemas no motor gráfico e orientações de montagem desenvolvidas para este simulador. O objetivo foi criar uma base sólida, altamente modular e pronta para produção, que replique com fidelidade física o comportamento de armas de fogo, o cálculo balístico e as regras esportivas de uma pista de IPSC.

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
| `IPSCUIManager.cs` | `Assets/Scripts/UI/` | HUD completo em tela via IMGUI nativo: cronômetro dinâmico, último split, contador de munição e painel de resultado com Hit Factor. |
| `ProceduralAudio.cs` | `Assets/Scripts/Utilities/` | Síntese de áudio matemática em tempo de execução: bipe senoidal de 2.75 kHz, estampido de tiro 9mm, trava de ferrolho e clique seco de percussor. |
| `ProceduralMaterials.cs` | `Assets/Scripts/Utilities/` | Geração procedural de texturas de papelão com linhas de zonas IPSC e furos de bala transparentes. |
| `SceneGeneratorCore.cs` | `Assets/Scripts/Setup/` | Construtor que gera o estande de tiro completo (chão, barreiras de segurança, para-balas, alvos e pistola 3D funcional). |
| `IPSCSceneBuilder.cs` | `Assets/Scripts/Editor/` | Adiciona o menu **`IPSC Simulator > Montar Pista Completa (Auto-Setup)`** na barra superior do Unity Editor. |
| `IPSCAutoBootstrap.cs` | `Assets/Scripts/Setup/` | Gatilho para montar automaticamente o cenário caso a cena esteja vazia ao dar Play. |
| `INICIAR_JOGO.bat` | Raiz do projeto | Launcher seguro para inicializar a Unity com DirectX 11, limpando travas e garantindo estabilidade. |

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

Durante o planejamento visual, foi orientado o resgate e a importação de ativos 3D gratuitos na **Unity Asset Store**:
- **Pacotes Selecionados:**
  - *`Pacote de Armas de Pistola Low-Poly Weapon Pack 1`* (da CASTELO BRAVO): Pistolas semiautomáticas (estilos Glock e 1911/2011) ideais para divisões IPSC Production e Standard.
  - *`Low Poly SMG Weapons`*: Submetralhadoras / Carabinas 9mm para simular a divisão oficial **IPSC PCC (Pistol Caliber Carbine)**.
  - *`Dan Wesson Modelo 715`*: Revólver com massa de fibra ótica para divisão IPSC Revolver.
- **Ciclo de Vida de Ativos (Open in Unity vs My Assets):**
  - Clicar em *"Adicionar aos Meus Recursos"* ou *"Open in Unity"* vincula o pacote à licença da conta na nuvem da Unity.
  - Para que os modelos estejam fisicamente utilizáveis na cena, eles devem ser baixados e importados para o projeto via **`Window > Package Manager > Packages: My Assets`** dentro da Unity, surgindo então na pasta `Assets/`.

---

## 🔬 5. Diagnósticos Técnicos, Solução de Problemas & Estabilidade no Unity 6

Durante a integração no ambiente local do usuário (Windows 11, Unity 6000.6.10f1, NVIDIA GeForce GTX 1660 SUPER), foram superados quatro desafios críticos de infraestrutura:

### 1. Incompatibilidade com uGUI na Unity 6 (Erro CS0234)
- **Sintoma:** O Unity Editor impedia o Play com a mensagem: *"All compiler errors have to be fixed before you can enter playmode!"* e erros `CS0234: The type or namespace name 'UI' does not exist in the namespace 'UnityEngine'`.
- **Causa:** Na Unity 6, o pacote antigo `com.unity.ugui` não vem habilitado por padrão em projetos básicos.
- **Solução Arquitetural:** Em vez de forçar a instalação do pacote pesado uGUI, o `IPSCUIManager.cs` e o `SceneGeneratorCore.cs` foram refatorados para utilizar a biblioteca nativa `IMGUI` (`OnGUI`).
- **Resultado:** Zero dependências de pacotes externos, compilação em 3.4 segundos com **0 Erros** e compatibilidade imediata com qualquer versão da Unity (2021, 2022 ou 6.x).

### 2. Módulo WebGL / Web Build Support no Unity Hub
- **Verificação:** Foi solicitada a instalação do módulo WebGL. No Unity Hub (configurado em português), verificou-se que na Unity 6 a plataforma foi renomeada para **`Web Build Support`** e já estava plenamente instalada (5.62 GB), permitindo compilações futuras para Web/HTML5 diretamente pelo menu `File > Build Profiles`.

### 3. Falha de Importadores Paralelos (AssetImportWorkers) com DirectX 12
- **Sintoma:** Bloqueio da interface com a barra verde contínua (*"Hold on busy for... Waiting for Unity's code in UnityEditor.CoreModule.dll to finish executing"*), seguido pelo popup do *Unity Bug Reporter*.
- **Investigação de Logs:**
  - No `AssetImportWorkerHW1.log`: `d3d12: failed to create D3D12 device (0x887a0004). Crash!!!`
  - No `AssetImportWorkerHW2.log`: `AssetImportWorkerClient::OnTransportError - code=10061 error=Nenhuma conexão pôde ser feita porque a máquina de destino as recusou ativamente.`
- **Causa Raiz:** A Unity 6 inicializa por padrão com a API gráfica DirectX 12 (`<DX12>`). Em drivers NVIDIA GeForce GTX 1660 SUPER, os processos secundários de importação (*AssetImportWorkers*) em modo headless falham ao instanciar múltiplos dispositivos D3D12 concorrentes. Ao caírem, a comunicação por socket local (porta TCP 127.0.0.1) é recusada (erro 10061), travando a thread principal de interface.
- **Soluções Implementadas:**
  1. Criação do launcher `INICIAR_JOGO.bat` forçando inicialização em **DirectX 11** (`-force-d3d11`), eliminando a falha gráfica na raiz.
  2. Ajuste em `EditorSettings.asset` para evitar deadlocks de sincronização.

### 4. Esgotamento de Memória Virtual & Corrupção de Cache LMDB
- **Sintoma:** Cliques repetidos em "Open in Unity" no navegador instanciaram múltiplos processos `Unity.exe` concorrentes no mesmo projeto. O log registrou `GetLastError: Não há recursos de memória disponíveis suficientes para processar este comando`, restando apenas 424 MB de memória virtual.
- **Corrupção:** O encerramento forçado do processo anterior corrompeu o banco de dados LMDB na pasta `Library/` com ponteiros nulos (`Guid(000000000000)`).
- **Ação:**
  1. Finalização de todos os processos zumbis, liberando **4,5 GB de memória RAM**.
  2. Limpeza profunda das pastas corrompidas `Library/` e `Temp/` (incluindo o arquivo de bloqueio `UnityLockfile`).
  3. Com isso, na reinicialização o motor recria um banco de dados limpo a partir de `Assets/` sem qualquer loop de falhas.

### 5. Deadlock no Loop Principal (`Application.TickGlobalCallbacks`) e Crash do Burst JIT em DX12
- **Sintoma:** A interface do Unity Editor congelou indefinidamente com o diálogo modal *"Hold on (busy for 26:47)... Application.TickGlobalCallbacks / Waiting for Unity's code to finish executing"*. No Gerenciador de Tarefas, o processo principal apresentava 0% de CPU com o subprocesso *Unity Crash Handler* ativo e a janela do *Unity Bug Reporter* em segundo plano.
- **Diagnóstico Técnico via Logs:**
  - O arquivo `Logs/Editor.log` registrou uma exceção nativa fatal de violação de acesso:
    `Library\BurstCache\JIT\6502de7465e721a809806fc4a4921394.dll: ERROR: SymGetSymFromAddr64, GetLastError: 'Tentativa de acessar endereço inválido.'`
  - Esse crash ocorreu pela instabilidade entre o compilador JIT do **Burst** e o backend gráfico experimental **DirectX 12 (`<DX12>`)** da versão prévia **Unity 6.6 (`6000.6.10f1`)** na placa NVIDIA GeForce GTX 1660 SUPER.
  - Ao ocorrer a falha, o executável `UnityCrashHandler64.exe` pausou a thread de execução da engine para coletar o dump de memória, gerando um deadlock irreversível onde o contador de tempo subia indefinidamente sem que o processo pudesse se recuperar sozinho.
- **Ações Corretivas Executadas:**
  1. Finalização imediata de instâncias do `UnityCrashHandler64.exe` e processos filhos.
  2. Purga completa do diretório `Library/BurstCache/` para eliminar binários JIT corrompidos.
  3. Limpeza das travas de concorrência em `Temp/UnityLockfile`.
  4. Atualização do script `INICIAR_JOGO.bat` com parâmetro `-force-d3d11` (DirectX 11 estável).
  5. Instrução para parametrização nativa no Unity Hub com o argumento de inicialização estável.

---

## 🚀 6. Governança e Versionamento no GitHub

Todo o ecossistema do simulador está versionado sob a branch `main`:
- **Repositório:** [https://github.com/wederfs40-art/Projetos_Antigravit](https://github.com/wederfs40-art/Projetos_Antigravit)
- **Filtro `.gitignore`:** Protege o repositório contra pastas temporárias gigantescas geradas pelo motor (`Library/`, `Temp/`, `Logs/`, `Build/`, `UserSettings/`).
- **Arquivos Versionados:** Todos os scripts C#, prefabs, texturas procedurais, configurações de física/layers do projeto e documentações completas.
