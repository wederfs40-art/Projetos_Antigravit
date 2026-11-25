# Projetos Antigravit - Simulador de Tiro Esportivo (IPSC) & Combate Tático

Repositório central de desenvolvimento integrado de simuladores 3D com a plataforma Antigravit, Unity 6 e GitHub.

---

## 🎯 Projeto Principal: `JogoTiroEsportivo`

Simulador de tiro em primeira pessoa (FPS) de alta fidelidade balística, com regras oficiais do tiro esportivo (**IPSC**), estandes de precisão de clube e cenários de combate tático em curta distância (**CQB / Killhouse estilo Counter-Strike**).

### 🚀 Principais Funcionalidades

1. **Menu Principal & Seletor de Modos:**
   - Tela de abertura e menu de pausa (`ESC`) permitindo alternar cenários e armas a qualquer instante.
2. **5 Ambientes 3D:**
   - **IPSC Outdoor - Pista Curta:** 12 tiros, shooting box zebrada, tambores de 55 galões, mesa de segurança e alvos de papelão.
   - **IPSC Outdoor - Pista Média:** 24 tiros, percurso dinâmico com labirinto de barricadas de madeira ripada e múltiplos ângulos.
   - **IPSC Outdoor - Pista Longa:** 32 tiros, estande aberto de 30 metros, para-balas elevados e poppers de aço distantes.
   - **Estande de Precisão Indoor:** Galpão fechado de clube com piso de concreto polido escuro, paredes acústicas, 4 baias individuais de tiro (cabines 1 a 4) e linhas de 5m a 25m.
   - **Killhouse Tático CS-Style:** Cenário CQB inspirado em Counter-Strike com contêineres de carga de 6m (vermelhos e azuis), caixas militares de suprimentos, tambores de óleo, esquinas de varredura e silhuetas hostis com reféns civis (*No-Shoot*).
3. **Arsenal com 3 Categorias de Armas:**
   - **Pistola Tática 9mm:** 17 disparos, semiautomática, massa de fibra ótica de alta visibilidade.
   - **Revólver Dan Wesson 715 .357 Magnum:** 6 disparos, alto impacto, recuo expressivo e áudio encorpado.
   - **Submetralhadora Tática (SMG):** 30 disparos, cadência totalmente automática de 550 RPM para combate contínuo.
4. **Balística Realista & Efeitos Físicos:**
   - Projétil com trajetória física anti-tunneling (*Raycast contínuo*) e gravidade real.
   - Ponto de impacto dependente da distância (*Height-Over-Bore*).
   - Recuo procedural e balanço harmônico de respiração atenuado com tecla `Shift`.
   - Alvos de papelão com zonas oficiais (Alpha +5, Charlie +3, Delta +1, No-Shoot -10) e decalques reais de furos de bala.
   - Pratos metálicos reativos (*IPSC Steel Poppers*) com queda física articulada (-85°) e áudio procedural de impacto em aço.
   - Cronômetro oficial de pista (Shot Timer) com *Standby*, *Buzzer Beep*, splits por tiro e cálculo automático de **Hit Factor (HF)**.

---

## 📖 Documentação Detalhada

- [Manual do Projeto e Guia Rápido](file:///d:/weder/IAs/Antigravit/Projetos_Antigravit/JogoTiroEsportivo/README_PROJETO.md)
- [Histórico Completo de Desenvolvimento e Solução de Problemas Técnicos](file:///d:/weder/IAs/Antigravit/Projetos_Antigravit/JogoTiroEsportivo/HISTORICO_DESENVOLVIMENTO.md)

---

## 🎮 Controles

- **W, A, S, D:** Movimentação
- **Mouse:** Olhar / Mirar
- **Botão Direito (Segurar):** Mira ADS (Aim Down Sights)
- **Shift Esquerdo (Segurar):** Prender a Respiração (estabilização)
- **Botão Esquerdo:** Disparar (Semi / Automático contínuo)
- **R:** Recarregar carregador / tambor
- **T:** Comando do RO (*Standby* + *BEEP*)
- **Y:** Encerrar Pista e calcular Hit Factor
- **U:** Obrear alvos (limpar furos) e levantar poppers de aço
- **ESC:** Menu de Pausa / Configuração de Mapas e Armas
