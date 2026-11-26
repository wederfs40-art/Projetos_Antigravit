using UnityEngine;

namespace IPSCSimulator.UI
{
    using IPSCSimulator.Match;
    using IPSCSimulator.Weapons;

    /// <summary>
    /// Gerencia a interface gráfica (IMGUI) do Menu Principal e do Menu de Pausa (ESC).
    /// Permite escolher entre Pistas de IPSC, Estandes de Precisão e Cenários Táticos estilo CS,
    /// além de selecionar o arsenal (Pistola 9mm, Revólver Dan Wesson .357 e SMG).
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        private Texture2D _panelBgTex;
        private Texture2D _btnNormalTex;
        private Texture2D _btnSelectedTex;
        private Texture2D _btnStartTex;

        private GUIStyle _titleStyle;
        private GUIStyle _subTitleStyle;
        private GUIStyle _sectionHeaderStyle;
        private GUIStyle _btnStyle;
        private GUIStyle _btnActiveStyle;
        private GUIStyle _btnStartStyle;
        private GUIStyle _infoBoxStyle;

        private GameEnvironmentType _tempEnv = GameEnvironmentType.Outdoor_Precision_Stand;
        private SelectedWeaponType _tempWeapon = SelectedWeaponType.Pistol_Beretta_M9T_Black;
        private WeaponCategory _selectedCategory = WeaponCategory.Pistols;

        private void Start()
        {
            if (TacticalGameManager.Instance != null)
            {
                _tempEnv = TacticalGameManager.Instance.SelectedEnvironment;
                _tempWeapon = TacticalGameManager.Instance.SelectedWeapon;
            }
        }

        private void OnGUI()
        {
            InitStyles();

            GameState state = (TacticalGameManager.Instance != null) 
                ? TacticalGameManager.Instance.CurrentState 
                : GameState.MainMenu;

            if (state == GameState.MainMenu)
            {
                DrawMainMenu();
            }
            else if (state == GameState.Paused)
            {
                DrawPauseMenu();
            }
        }

        private void DrawMainMenu()
        {
            // Fundo escuro cobrindo a tela
            GUI.color = new Color(0.08f, 0.08f, 0.10f, 0.95f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float panelWidth = Mathf.Min(920f, Screen.width - 40f);
            float panelHeight = Mathf.Min(780f, Screen.height - 30f);
            float startX = (Screen.width - panelWidth) * 0.5f;
            float startY = (Screen.height - panelHeight) * 0.5f;

            GUILayout.BeginArea(new Rect(startX, startY, panelWidth, panelHeight), _infoBoxStyle);

            // Cabeçalho
            GUILayout.Label("TACTICAL SHOOTER & IPSC SIMULATOR", _titleStyle);
            GUILayout.Label("Selecione o Ambiente de Treino e seu Armamento para Iniciar", _subTitleStyle);
            GUILayout.Space(10);

            // Seção 1: Seleção de Ambientes
            GUILayout.Label("1. SELECIONE O CENÁRIO / AMBIENTE:", _sectionHeaderStyle);
            GUILayout.Space(4);

            DrawEnvironmentButton(GameEnvironmentType.Outdoor_Precision_Stand, "⭐ Stand Aberto com Foco Visual (Ilustração)", "Estande aberto com cascalho claro, barranco de terra, vegetação e alvo silhueta focado na alça/massa de mira.");
            DrawEnvironmentButton(GameEnvironmentType.IPSC_Outdoor_Short, "🏆 Pista Curta IPSC (Speed Option)", "3 alvos de papelão + 2 poppers metálicos rápidos a 7m e 9m.");
            DrawEnvironmentButton(GameEnvironmentType.IPSC_Outdoor_Medium, "🏆 Pista Média IPSC (Labirinto Tático)", "Corredores com biombos de madeira, alvos No-Shoot e barris táticos.");
            DrawEnvironmentButton(GameEnvironmentType.IPSC_Outdoor_Long, "🏆 Pista Longa IPSC (Torneio Oficial 30m)", "Pista completa com 8 alvos de papelão, 4 poppers de aço e 3 boxes de tiro.");
            DrawEnvironmentButton(GameEnvironmentType.Indoor_Precision_Stand, "🎯 Estande Indoor de Precisão", "Estande fechado com cabines individuais acústicas e marcações de 5m a 25m.");
            DrawEnvironmentButton(GameEnvironmentType.Tactical_CS_Killhouse, "🏢 Armazém Tático CQB (Estilo Counter-Strike)", "Ambiente urbano com contêineres marítimos, caixas militares e alvos hostis.");
            DrawEnvironmentButton(GameEnvironmentType.Bodycam_ShootHouse, "🎥 Killhouse Tático em Compensado (Estilo BODYCAM / UNRECORD)", "Shoot house de madeira compensada com vigas estruturais, iluminação de galpão e câmera corporal.");

            GUILayout.Space(12);

            // Seção 2: Seleção de Armas (Menu em 2 Etapas: Categorias -> Grade de Variedades)
            GUILayout.Label("2. SELECIONE A CATEGORIA DO ARMAMENTO:", _sectionHeaderStyle);
            GUILayout.Space(3);

            GUILayout.BeginHorizontal();
            if (DrawCategoryTab(WeaponCategory.Pistols, "🔫 PISTOLAS")) _selectedCategory = WeaponCategory.Pistols;
            if (DrawCategoryTab(WeaponCategory.Revolvers, "💥 REVÓLVERES")) _selectedCategory = WeaponCategory.Revolvers;
            if (DrawCategoryTab(WeaponCategory.SubmachineGuns, "⚡ SUBMETRALHADORAS")) _selectedCategory = WeaponCategory.SubmachineGuns;
            if (DrawCategoryTab(WeaponCategory.AssaultRifles, "🎯 FUZIS DE ASSALTO")) _selectedCategory = WeaponCategory.AssaultRifles;
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label($"<b><color=#FFD700>ARSENAL DISPONÍVEL - {_selectedCategory.ToString().ToUpper()}</color></b> (Clique na arma para equipar):", _subTitleStyle);
            GUILayout.Space(4);

            // Grade de Armas da Categoria Selecionada (Estilo Buy Menu CS)
            DrawWeaponShowcaseGrid(_selectedCategory);

            GUILayout.FlexibleSpace();

            // Botão Principal de Iniciar
            if (GUILayout.Button("▶️  INICIAR SIMULAÇÃO / ENTRAR NO MAPA", _btnStartStyle, GUILayout.Height(52)))
            {
                if (TacticalGameManager.Instance != null)
                {
                    TacticalGameManager.Instance.StartGame(_tempEnv, _tempWeapon);
                }
            }

            GUILayout.EndArea();
        }

        private bool DrawCategoryTab(WeaponCategory cat, string title)
        {
            bool isSelected = (_selectedCategory == cat);
            GUIStyle style = isSelected ? _btnActiveStyle : _btnStyle;
            return GUILayout.Button(title, style, GUILayout.Height(36));
        }

        private void DrawEnvironmentButton(GameEnvironmentType env, string title, string description)
        {
            bool isSelected = (_tempEnv == env);
            GUIStyle style = isSelected ? _btnActiveStyle : _btnStyle;
            string text = isSelected 
                ? $"<b><color=#00FFAA>● {title}</color></b>  <color=#DDDDDD>{description}</color>" 
                : $"<b>{title}</b>  <color=#AAAAAA>{description}</color>";
            if (GUILayout.Button(text, style, GUILayout.Height(30)))
            {
                _tempEnv = env;
            }
            GUILayout.Space(2);
        }

        private void DrawWeaponShowcaseGrid(WeaponCategory cat)
        {
            switch (cat)
            {
                case WeaponCategory.Pistols:
                    GUILayout.BeginHorizontal();
                    DrawWeaponCard(SelectedWeaponType.Pistol_Beretta_M9T_Black, "BERETTA M9T (BLACK)", "9x19mm", "17 Tiros", "Semi-Auto", "Pistola militar de alta precisão");
                    DrawWeaponCard(SelectedWeaponType.Pistol_Beretta_M9T_Tan, "BERETTA M9T (TAN)", "9x19mm", "17 Tiros", "Semi-Auto", "Camuflagem desértica operacional");
                    GUILayout.EndHorizontal();
                    GUILayout.Space(4);
                    GUILayout.BeginHorizontal();
                    DrawWeaponCard(SelectedWeaponType.Pistol_Tactical_Glock, "GLOCK 17 COMBAT", "9x19mm", "17 Tiros", "Striker-Fired", "Perfil baixo e visada rápida");
                    DrawWeaponCard(SelectedWeaponType.Pistol_Combat_Custom, "COLT 1911 CUSTOM", ".45 ACP", "15 Tiros", "Ação Simples", "Alto impacto e recuo firme");
                    GUILayout.EndHorizontal();
                    break;

                case WeaponCategory.Revolvers:
                    GUILayout.BeginHorizontal();
                    DrawWeaponCard(SelectedWeaponType.Revolver_DanWesson_357, "DAN WESSON 715", ".357 MAG", "6 Tiros", "Ação Dupla", "Cano pesado ventilado fotorrealista");
                    DrawWeaponCard(SelectedWeaponType.Revolver_Superhawk_Tactical, "SUPERHAWK TACTICAL", ".44 MAG", "6 Tiros", "Ação Dupla", "Trilho tático e poder devastador");
                    GUILayout.EndHorizontal();
                    break;

                case WeaponCategory.SubmachineGuns:
                    GUILayout.BeginHorizontal();
                    DrawWeaponCard(SelectedWeaponType.SMG_Strongarm_Tactical, "MP5 STRONGARM", "9mm AUTO", "30 Tiros", "Automático", "Cadência de 800 RPM e baixo recuo");
                    DrawWeaponCard(SelectedWeaponType.SMG_MP_Vector, "UMP-45 TACTICAL", ".45 AUTO", "30 Tiros", "Automático", "Poder de parada em combate urbano");
                    DrawWeaponCard(SelectedWeaponType.SMG_P90_Compact, "P90 BULLPUP", "5.7x28mm", "50 Tiros", "Automático", "Carregador estendido de 50 projéteis");
                    GUILayout.EndHorizontal();
                    break;

                case WeaponCategory.AssaultRifles:
                    GUILayout.BeginHorizontal();
                    DrawWeaponCard(SelectedWeaponType.Rifle_AKM_Tactical, "AKM 7.62x39mm", "7.62mm", "30 Tiros", "Automático", "Alta penetração e clássico russo");
                    GUILayout.EndHorizontal();
                    break;
            }
        }

        private void DrawWeaponCard(SelectedWeaponType weapon, string name, string caliber, string capacity, string fireMode, string description)
        {
            bool isSelected = (_tempWeapon == weapon);
            GUIStyle cardStyle = isSelected ? _btnActiveStyle : _btnStyle;

            string statusTag = isSelected ? "<b><color=#00FFAA>[✓ EQUIPADA]</color></b>" : "<color=#AAAAAA>[ CLIQUE P/ EQUIPAR ]</color>";
            string cardText = $"<b><color=#FF4444>{name}</color></b>\n" +
                              $"<color=#DDDDDD>Calibre: {caliber}  |  Capacidade: {capacity}  |  {fireMode}</color>\n" +
                              $"<i><color=#999999>{description}</color></i>\n" +
                              $"{statusTag}";

            if (GUILayout.Button(cardText, cardStyle, GUILayout.Height(62)))
            {
                _tempWeapon = weapon;
            }
        }

        private void DrawPauseMenu()
        {
            // Escurecimento do fundo
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float boxW = 420f;
            float boxH = 340f;
            float x = (Screen.width - boxW) * 0.5f;
            float y = (Screen.height - boxH) * 0.5f;

            GUILayout.BeginArea(new Rect(x, y, boxW, boxH), _infoBoxStyle);

            GUILayout.Label("JOGO PAUSADO", _titleStyle);
            GUILayout.Label("Pressione [ESC] para continuar", _subTitleStyle);
            GUILayout.Space(20);

            if (GUILayout.Button("▶️  CONTINUAR", _btnStartStyle, GUILayout.Height(45)))
            {
                if (TacticalGameManager.Instance != null) TacticalGameManager.Instance.ResumeGame();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("🔄  REINICIAR PISTA ATUAL", _btnStyle, GUILayout.Height(42)))
            {
                if (TacticalGameManager.Instance != null) TacticalGameManager.Instance.RestartCurrentMap();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("📋  MENU PRINCIPAL / TROCAR CENÁRIO", _btnStyle, GUILayout.Height(42)))
            {
                if (TacticalGameManager.Instance != null) TacticalGameManager.Instance.ReturnToMainMenu();
            }

            GUILayout.EndArea();
        }

        private void InitStyles()
        {
            if (_panelBgTex == null)
            {
                _panelBgTex = MakeColorTex(new Color(0.12f, 0.13f, 0.16f, 0.96f));
                _btnNormalTex = MakeColorTex(new Color(0.18f, 0.20f, 0.24f, 1f));
                _btnSelectedTex = MakeColorTex(new Color(0.16f, 0.45f, 0.85f, 1f));
                _btnStartTex = MakeColorTex(new Color(0.15f, 0.65f, 0.35f, 1f));

                _titleStyle = new GUIStyle
                {
                    fontSize = 24,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                _titleStyle.normal.textColor = new Color(0.95f, 0.85f, 0.25f);

                _subTitleStyle = new GUIStyle
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Italic,
                    alignment = TextAnchor.MiddleCenter
                };
                _subTitleStyle.normal.textColor = new Color(0.75f, 0.75f, 0.78f);

                _sectionHeaderStyle = new GUIStyle
                {
                    fontSize = 15,
                    fontStyle = FontStyle.Bold
                };
                _sectionHeaderStyle.normal.textColor = Color.white;

                _infoBoxStyle = new GUIStyle
                {
                    padding = new RectOffset(20, 20, 20, 20)
                };
                _infoBoxStyle.normal.background = _panelBgTex;

                _btnStyle = new GUIStyle
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(12, 12, 6, 6)
                };
                _btnStyle.normal.background = _btnNormalTex;
                _btnStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);

                _btnActiveStyle = new GUIStyle(_btnStyle);
                _btnActiveStyle.normal.background = _btnSelectedTex;
                _btnActiveStyle.normal.textColor = Color.white;
                _btnActiveStyle.fontStyle = FontStyle.Bold;

                _btnStartStyle = new GUIStyle
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                _btnStartStyle.normal.background = _btnStartTex;
                _btnStartStyle.normal.textColor = Color.white;
            }
        }

        private Texture2D MakeColorTex(Color color)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }
    }
}
