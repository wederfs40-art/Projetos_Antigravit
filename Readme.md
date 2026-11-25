# Projetos Antigravit - Simulador de Tiro Esportivo (IPSC) & Combate Tático

Repositório central de desenvolvimento integrado de simuladores 3D com a plataforma Antigravit, Unity 6 e GitHub.

---

## 🎯 Projeto Principal: `JogoTiroEsportivo`

Simulador de tiro em primeira pessoa (FPS) de alta fidelidade balística, com regras oficiais do tiro esportivo (**IPSC**), estandes de precisão de clube e cenários de combate tático em curta distância (**CQB / Killhouse estilo Counter-Strike**).

### 🚀 Principais Funcionalidades

1. **Menu Principal & Seletor de Modos:**
   - Tela de abertura e menu de pausa (`ESC`) permitindo alternar cenários e armas a qualquer instante.
2. **6 Ambientes 3D:**
   - **IPSC Outdoor - Pista Curta:** 12 tiros, shooting box zebrada, tambores de 55 galões, mesa de segurança e alvos de papelão.
   - **IPSC Outdoor - Pista Média:** 24 tiros, percurso dinâmico com labirinto de barricadas de madeira ripada e múltiplos ângulos.
   - **IPSC Outdoor - Pista Longa:** 32 tiros, estande aberto de 30 metros, para-balas elevados e poppers de aço distantes.
   - **Estande de Precisão Indoor:** Galpão fechado de clube com piso de concreto polido escuro, paredes acústicas, 4 baias individuais de tiro (cabines 1 a 4) e linhas de 5m a 25m.
   - **Killhouse Tático CS-Style:** Cenário CQB inspirado em Counter-Strike com contêineres de carga de 6m (vermelhos e azuis), caixas militares de suprimentos, tambores de óleo, esquinas de varredura e silhuetas hostis com reféns civis (*No-Shoot*).
   - **Killhouse Policial em Compensado (Estilo BODYCAM):** Galpão industrial com treliças de aço estruturais no teto, lâmpadas fluorescentes industriais suspensas e labirinto de paredes em compensado/OSB cru sobre vigas de madeira 2x4.
3. **Arsenal com 3 Categorias de Armas & Braços Táticos:**
   - **Braços e Luvas Táticas em 1ª Pessoa (`AttachTacticalHands`):** Antebraços com mangas de combate camufladas e luvas táticas com reforço nos nós dos dedos, empunhando a arma realisticamente a duas mãos (fim da arma flutuando no ar).
   - **Pistola Tática 9mm:** 17 disparos, semiautomática, massa de fibra ótica de alta visibilidade.
   - **Revólver Dan Wesson 715 .357 Magnum:** 6 disparos, alto impacto, recuo expressivo e áudio encorpado.
   - **Submetralhadora Tática (SMG):** 30 disparos, cadência totalmente automática de 550 RPM para combate contínuo.
4. **Balística Realista, Ação Mecânica & Efeitos Físicos:**
   - **Câmera Corporal Tática (Bodycam):** Free Aim (zona morta de mira), inércia de movimentação do colete e carimbo `REC ● BODYCAM-04`.
   - **Ação Mecânica Real:** Recoil kick físico translacional com elevação do cano (`muzzleClimb`), ejeção física de cápsulas/estojos de latão com quique no chão, flash dinâmico no cano e animação de recarregamento com troca de carregador (`[R]`).
   - Projétil com trajetória física anti-tunneling (*Raycast contínuo*) e gravidade real.
   - Ponto de impacto dependente da distância (*Height-Over-Bore*).
   - Alvos reativos com animação de vibração ao impacto, furos de bala visíveis instantâneos e som de rasgo de papelão.
   - Pratos metálicos reativos (*IPSC Steel Poppers*) com queda física articulada (-85°) e áudio procedural de impacto em aço.
   - Cronômetro oficial de pista (Shot Timer) com *Standby*, *Buzzer Beep*, splits por tiro e cálculo automático de **Hit Factor (HF)**.
   - Hotbar visual de arsenal e troca instantânea de armas em tempo real (`[1]`, `[2]`, `[3]` ou scroll do mouse).

---

## 📖 Documentação Detalhada

- [Manual do Projeto e Guia Rápido](file:///d:/weder/IAs/Antigravit/Projetos_Antigravit/JogoTiroEsportivo/README_PROJETO.md)
- [Histórico Completo de Desenvolvimento e Solução de Problemas Técnicos](file:///d:/weder/IAs/Antigravit/Projetos_Antigravit/JogoTiroEsportivo/HISTORICO_DESENVOLVIMENTO.md)

---

## 🎮 Controles

- **W, A, S, D:** Movimentação
- **Mouse:** Olhar / Mirar (com Free Aim de câmera corporal)
- **[1], [2], [3]** ou **Scroll do Mouse:** Alternar instantaneamente de arma
- **Botão Direito (Segurar):** Mira ADS (Aim Down Sights)
- **Shift Esquerdo (Movimento):** Tactical Sprint (corrida com arma em high ready) ou estabilização
- **I:** Inspecionar Arma (Weapon Inspect - giro de 3 fases)
- **Botão Esquerdo:** Disparar (Semi / Automático contínuo)
- **R:** Recarregar carregador (com ejeção e inserção de magazine e slide rack)
- **T:** Comando do RO (*Standby* + *BEEP*)
- **Y:** Encerrar Pista e calcular Hit Factor
- **U:** Obrear alvos (limpar furos) e levantar poppers de aço
- **ESC:** Menu de Pausa / Configuração de Mapas e Armas
