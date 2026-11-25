# Simulador de Tiro Esportivo (IPSC) & Tático - Unity C#

Projeto completo com arquitetura de simulação balística realista, manuseio de arma, pontuação IPSC com alvos de papelão e cronômetro oficial de tiro (Shot Timer).

---

## ⚡ Como Rodar o Jogo em 1 Clique (Auto-Setup)

Você **não precisa modelar nem configurar nada manualmente**:

1. Abra o **Unity Hub**, clique em **Add** (ou crie um novo projeto 3D) e selecione a pasta `JogoTiroEsportivo`.
2. No menu superior da Unity, clique em:
   👉 **`IPSC Simulator` > `Montar Pista Completa (Auto-Setup)`**
3. **Pronto!** O script gera instantaneamente na sua cena:
   - Todo o estande de tiro (chão de cascalho, para-balas de areia, paredes laterais e shooting box).
   - 3 Alvos de papelão IPSC (7m, 12m e 16m) com colisores das zonas Alpha, Charlie e Delta.
   - 1 Alvo No-Shoot com aviso de penalidade.
   - O jogador com câmera em 1ª pessoa.
   - A pistola 3D funcional com massa de mira em fibra ótica, entalhe de alça de mira e ferrolho móvel.
   - Áudios sintetizados de tiro real 9mm, percussor e bipe do Shot Timer.
   - Interface (HUD) com cronômetro, splits, munição e tela final de Hit Factor.
4. Aperte o botão **PLAY** no topo da Unity!

---

## 🎮 Controles e Teclas da Pista

| Tecla / Ação | Função |
| :--- | :--- |
| **W, A, S, D** | Movimentação pelo estande |
| **Mouse** | Olhar / Mirar a visão |
| **Botão Direito (Segurar)** | **ADS (Aim Down Sights)**: Alinha alça, massa de mira e alvo perfeitamente |
| **Shift Esquerdo (Segurar)** | **Prender a Respiração**: Estabiliza o balanço do corpo |
| **Botão Esquerdo** | **Disparar**: Registra split, aciona coice/recuo procedural e gera furo no alvo |
| **R** | **Recarregar**: Alimenta 17 tiros e destrava o ferrolho aberto |
| **T** | **Comando do RO (Range Officer)**: Dá *"Standby"*, aguarda de 1 a 4s e soa o **BEEP!** |
| **Y** | **Encerrar Pista**: Para o cronômetro, soma pontos/penalidades e exibe o **Hit Factor** |
| **U** | **Obrear Alvos**: Remove todos os furos de bala e zera os alvos para a próxima série |
| **ESC** | Libera/trava o cursor do mouse |

---

## 📁 Estrutura de Arquivos Criados

- `Assets/Scripts/Ballistics/Bullet.cs` - Balística contínua por Raycast (anti-tunneling) e gravidade.
- `Assets/Scripts/Weapons/Weapon.cs` - Cano no Muzzle Point (Height-Over-Bore), munição e slide lock.
- `Assets/Scripts/Weapons/WeaponHandling.cs` - Visada precisa, respiração harmônica e recuo elástico.
- `Assets/Scripts/Targets/IPSCTarget.cs` & `IPSCTargetZone.cs` - Zonas pontuadas e obreamento.
- `Assets/Scripts/Match/RangeOfficerTimer.cs` - Cronometragem oficial em centésimos, splits e cálculo do Hit Factor.
- `Assets/Scripts/Player/PlayerController.cs` - Locomoção em 1ª pessoa.
- `Assets/Scripts/UI/IPSCUIManager.cs` - HUD gráfico do cronômetro e relatório.
- `Assets/Scripts/Utilities/ProceduralAudio.cs` - Síntese matemática de áudios (bipe IPSC e tiros).
- `Assets/Scripts/Utilities/ProceduralMaterials.cs` - Geração de texturas de papelão e furos de bala.
- `Assets/Scripts/Setup/SceneGeneratorCore.cs` & `IPSCAutoBootstrap.cs` - Construtores de cena.
- `Assets/Scripts/Editor/IPSCSceneBuilder.cs` - Botão de menu no Editor da Unity.
