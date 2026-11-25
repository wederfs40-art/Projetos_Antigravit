# 🎯 Simulador de Tiro Esportivo (IPSC) & Combate Tático - Unity C#

![Unity](https://img.shields.io/badge/Unity-2022.3%2B%20%7C%206.x-black?style=for-the-badge&logo=unity)
![C#](https://img.shields.io/badge/C%23-10.0%2B-blue?style=for-the-badge&logo=c-sharp)
![IPSC](https://img.shields.io/badge/Esporte-IPSC%20%2F%20IDPA%20%2F%20Tiro%20Esportivo-red?style=for-the-badge)

Simulador de tiro em primeira pessoa (FPS) ultra realista focado em **tiro esportivo com regras regulamentares da IPSC** e estande de combate tático, construído em arquitetura modular limpa em **C# na Unity**.

---

## 📋 Funcionalidades Principais

1. **Balística Realista & Anti-Tunneling:**
   - Projétil nasce fisicamente na ponta do cano da arma (*Muzzle Point*), gerando o efeito real de **Height Over Bore** (desvio vertical entre o ponto de mira e o cano em alvos próximos).
   - Detecção contínua de colisão por Raycast entre a posição anterior e atual para evitar que balas rápidas atravessem alvos (*anti-tunneling*).
   - Queda balística progressiva por gravidade com velocidade inicial em m/s (padrão 380 m/s para 9mm).

2. **Mecânica de Armamento & Recuo:**
   - Contagem precisa de munição no carregador (17 tiros).
   - Trava mecânica aberta do ferrolho (*Slide Lock*) após o último disparo, com cliques secos (*Dry Fire*) em câmara vazia e recarga funcional.
   - Visada precisa (*ADS - Aim Down Sights*) alinhando alça e massa de mira na linha de visão.
   - Balanço natural de respiração (*Weapon Sway* harmônico) atenuado ao segurar o `Shift` para prender a respiração.
   - Recuo procedural físico com elevação do cano e amortecimento elástico de retorno.

3. **Alvos de Papelão IPSC & Obreamento:**
   - Zonas métricas oficiais: **Alpha (+5 pts)**, **Charlie (+3 pts)**, **Delta (+1 pt)** e alvos **No-Shoot (-10 pts)**.
   - Decalques de orifícios de bala instanciados no ponto exato do impacto com orientação pela normal da superfície.
   - Sistema de **Obrear Alvos** (`ObrearAlvo`), removendo todos os furos para reiniciar a pista.

4. **Shot Timer Oficial & Range Officer:**
   - Procedimento de pista: Comando *"Standby..."*, delay aleatório de 1 a 4 segundos e **BEEP** do Shot Timer.
   - Registro de tempo em centésimos de segundo e cálculo do **Split** (intervalo entre tiros).
   - Verificação automática de penalidades de **Miss (-10 pts)** para alvos que não receberem o número mínimo de impactos regulamentares.
   - Cálculo e exibição da métrica oficial do esporte:
     $$\text{Hit Factor} = \frac{\text{Pontos Líquidos}}{\text{Tempo Total (s)}}$$

5. **Auto-Setup em 1 Clique & Áudio Sintetizado:**
   - Botão no menu da Unity para gerar o estande 3D completo em segundos.
   - Áudios sintetizados matematicamente (Buzzer de 2.75 kHz, tiros 9mm, percussor e ferrolho) sem dependência de arquivos externos.

---

## 🎮 Controles no Jogo

| Tecla / Ação | Ação no Jogo |
| :--- | :--- |
| **W, A, S, D** | Locomoção no estande |
| **Mouse** | Mirar e girar a visão |
| **Botão Direito (Segurar)** | **ADS (Mira Precisa)** |
| **Shift Esquerdo (Segurar)** | **Prender a Respiração** (estabiliza a mira) |
| **Botão Esquerdo** | **Disparar** |
| **R** | **Recarregar** (17 munições e fecha ferrolho) |
| **T** | **Range Officer**: Dá *"Standby"* e dispara o **BEEP** |
| **Y** | **Encerrar Pista**: Calcula e exibe o **Hit Factor** |
| **U** | **Obrear**: Limpa os furos de bala dos alvos |

---

## 📂 Estrutura do Código-Fonte

```text
Assets/Scripts/
├── Ballistics/
│   └── Bullet.cs             # Balística externa e colisão contínua
├── Weapons/
│   ├── Weapon.cs             # Mecânica de tiro, cano, ferrolho e munição
│   └── WeaponHandling.cs     # ADS, balanço de respiração e recuo procedural
├── Targets/
│   ├── IPSCTargetZone.cs     # Colisores específicos (Alpha, Charlie, Delta, No-Shoot)
│   └── IPSCTarget.cs         # Gestão de furos de bala e obreamento
├── Match/
│   └── RangeOfficerTimer.cs  # Shot Timer oficial, splits e Hit Factor
├── Player/
│   └── PlayerController.cs   # Controlador de movimentação e câmera FPS
├── UI/
│   └── IPSCUIManager.cs       # Interface visual (HUD e janela final)
├── Utilities/
│   ├── ProceduralAudio.cs    # Síntese matemática de áudios (bipe e tiros)
│   └── ProceduralMaterials.cs# Texturas procedurais de alvos e decalques
└── Setup/ & Editor/
    ├── SceneGeneratorCore.cs # Construtor do estande de tiro completo
    ├── IPSCAutoBootstrap.cs  # Inicializador automático de cena
    └── IPSCSceneBuilder.cs   # Menu superior do Unity Editor (Auto-Setup)
```

---

## 📖 Documentação Completa

Para a história detalhada de decisões arquiteturais, matemática das fórmulas e passo a passo de desenvolvimento, consulte:
- [HISTORICO_DESENVOLVIMENTO.md](HISTORICO_DESENVOLVIMENTO.md)
