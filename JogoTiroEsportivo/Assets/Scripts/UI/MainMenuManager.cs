using UnityEngine;

namespace IPSCSimulator.UI
{
    using IPSCSimulator.Match;

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

        private GameEnvironmentType _tempEnv = GameEnvironmentType.IPSC_Outdoor_Short;
        private SelectedWeaponType _tempWeapon = SelectedWeaponType.Pistol_9mm;

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

            float panelWidth = Mathf.Min(860f, Screen.width - 40f);
            float panelHeight = Mathf.Min(680f, Screen.height - 40f);
            float startX = (Screen.width - panelWidth) * 0.5f;
            float startY = (Screen.height - panelHeight) * 0.5f;

            GUILayout.BeginArea(new Rect(startX, startY, panelWidth, panelHeight), _infoBoxStyle);

            // Cabeçalho
            GUILayout.Label("TACTICAL SHOOTER & IPSC SIMULATOR", _titleStyle);
            GUILayout.Label("Selecione o Ambiente de Treino e seu Armamento para Iniciar", _subTitleStyle);
            GUILayout.Space(15);

            // Seção 1: Seleção de Ambientes
            GUILayout.Label("1. SELECIONE O CENÁRIO / AMBIENTE:", _sectionHeaderStyle);
            GUILayout.Space(5);

            DrawEnvironmentButton(GameEnvironmentType.IPSC_Outdoor_Short, "🏆 Pista Curta IPSC (Speed Option)", "3 alvos de papelão + 2 poppers metálicos rápidos a 7m e 9m.");
            DrawEnvironmentButton(GameEnvironmentType.IPSC_Outdoor_Medium, "🏆 Pista Média IPSC (Labirinto Tático)", "Corredores com biombos de madeira, alvos No-Shoot e barris táticos.");
            DrawEnvironmentButton(GameEnvironmentType.IPSC_Outdoor_Long, "🏆 Pista Longa IPSC (Torneio Oficial 30m)", "Pista completa com 8 alvos de papelão, 4 poppers de aço e 3 boxes de tiro.");
            DrawEnvironmentButton(GameEnvironmentType.Indoor_Precision_Stand, "🎯 Estande Indoor de Precisão", "Estande fechado com cabines individuais acústicas e marcações de 5m a 25m.");
            DrawEnvironmentButton(GameEnvironmentType.Tactical_CS_Killhouse, "🏢 Armazém Tático CQB (Estilo Counter-Strike)", "Ambiente urbano com contêineres marítimos, caixas militares e alvos hostis.");
            DrawEnvironmentButton(GameEnvironmentType.Bodycam_ShootHouse, "🎥 Killhouse Tático em Compensado (Estilo BODYCAM / UNRECORD)", "Shoot house de madeira compensada com vigas estruturais, iluminação de galpão e câmera corporal.");

            GUILayout.Space(15);

            // Seção 2: Seleção de Armas
            GUILayout.Label("2. SELECIONE SEU ARMAMENTO (ARSENAL):", _sectionHeaderStyle);
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            DrawWeaponButton(SelectedWeaponType.Pistol_9mm, "🔫 Pistola 9mm Tática", "17 munições | Cadência média | Recuo equilibrado");
            DrawWeaponButton(SelectedWeaponType.Revolver_DanWesson, "💥 Revólver Dan Wesson 715", ".357 Magnum | 6 tiros | Alto impacto e som potente");
            DrawWeaponButton(SelectedWeaponType.SMG_Tactical, "⚡ Submetralhadora SMG", "30 munições | Disparo contínuo | Alta cadência");
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            // Botão Principal de Iniciar
            if (GUILayout.Button("▶️  INICIAR SIMULAÇÃO / ENTRAR NO MAPA", _btnStartStyle, GUILayout.Height(55)))
            {
                if (TacticalGameManager.Instance != null)
                {
                    TacticalGameManager.Instance.StartGame(_tempEnv, _tempWeapon);
                }
            }

            GUILayout.EndArea();
        }

        private void DrawEnvironmentButton(GameEnvironmentType env, string title, string description)
        {
            bool isSelected = (_tempEnv == env);
            GUIStyle style = isSelected ? _btnActiveStyle : _btnStyle;

            if (GUILayout.Button($"{title}\n  {description}", style, GUILayout.Height(42)))
            {
                _tempEnv = env;
            }
        }

        private void DrawWeaponButton(SelectedWeaponType weapon, string title, string specs)
        {
            bool isSelected = (_tempWeapon == weapon);
            GUIStyle style = isSelected ? _btnActiveStyle : _btnStyle;

            if (GUILayout.Button($"{title}\n{specs}", style, GUILayout.Height(52)))
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
