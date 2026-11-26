using UnityEngine;

namespace IPSCSimulator.UI
{
    using IPSCSimulator.Match;
    using IPSCSimulator.Setup;
    using IPSCSimulator.Targets;
    using IPSCSimulator.Weapons;

    /// <summary>
    /// Gerencia a exibição visual em tela (HUD) do Shot Timer, Splits, Munição, Feedback de Impacto e Arsenal.
    /// Utiliza IMGUI nativo (OnGUI) para garantir 100% de compatibilidade em qualquer versão da Unity.
    /// </summary>
    public class IPSCUIManager : MonoBehaviour
    {
        private string _timerStatus = "Pressione [T] para Standby";
        private string _currentTimeStr = "00.00s";
        private string _lastSplitStr = "Split: --";
        private string _ammoStr = "17 / 17";
        private bool _showResult = false;
        private StageScoreResult _lastResult;

        private string _hitFeedbackStr = "";
        private Color _hitFeedbackColor = Color.white;
        private float _hitFeedbackTimer = 0f;

        private bool _showBuyMenu = false;
        private WeaponCategory _buyMenuCategory = WeaponCategory.Pistols;

        private GUIStyle _timerStyle;
        private GUIStyle _statusStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _splitStyle;
        private GUIStyle _ammoStyle;
        private GUIStyle _resultBoxStyle;
        private GUIStyle _resultTextStyle;
        private GUIStyle _controlsHelpStyle;
        private GUIStyle _hitFeedbackStyle;
        private GUIStyle _arsenalStyle;
        private Texture2D _blackBgTex;

        private void OnEnable()
        {
            Weapon.OnAmmoChanged += UpdateAmmoDisplay;
            RangeOfficerTimer.OnTimerStateChanged += UpdateTimerStateDisplay;
            RangeOfficerTimer.OnShotRegistered += UpdateShotDisplay;
            RangeOfficerTimer.OnStageFinished += ShowFinalScoreSummary;
            IPSCTarget.OnTargetZoneHitGlobal += ShowHitFeedback;
        }

        private void OnDisable()
        {
            Weapon.OnAmmoChanged -= UpdateAmmoDisplay;
            RangeOfficerTimer.OnTimerStateChanged -= UpdateTimerStateDisplay;
            RangeOfficerTimer.OnShotRegistered -= UpdateShotDisplay;
            RangeOfficerTimer.OnStageFinished -= ShowFinalScoreSummary;
            IPSCTarget.OnTargetZoneHitGlobal -= ShowHitFeedback;
        }

        private void Update()
        {
            if (RangeOfficerTimer.Instance != null && RangeOfficerTimer.Instance.State == TimerState.Running)
            {
                _currentTimeStr = $"{RangeOfficerTimer.Instance.CurrentTime:F2}s";
            }

            if (_hitFeedbackTimer > 0f)
            {
                _hitFeedbackTimer -= Time.deltaTime;
                if (_hitFeedbackTimer <= 0f) _hitFeedbackStr = "";
            }

            // Tecla [B]: Abre ou fecha o Menu de Arsenal / Buy Menu estilo Counter-Strike
            if (Input.GetKeyDown(KeyCode.B))
            {
                _showBuyMenu = !_showBuyMenu;
                if (_showBuyMenu)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }

        private void ShowHitFeedback(string targetName, IPSCZoneType zone, bool isNoShoot)
        {
            _hitFeedbackTimer = 1.4f;
            if (isNoShoot)
            {
                _hitFeedbackStr = $"⛔ PENALIDADE! {targetName} NO-SHOOT (-10 pts)";
                _hitFeedbackColor = new Color(1f, 0.2f, 0.2f, 1f);
            }
            else
            {
                switch (zone)
                {
                    case IPSCZoneType.Alpha:
                        _hitFeedbackStr = $"🎯 {targetName} - ALPHA (+5 pts)";
                        _hitFeedbackColor = new Color(0.2f, 1f, 0.3f, 1f);
                        break;
                    case IPSCZoneType.Charlie:
                        _hitFeedbackStr = $"🎯 {targetName} - CHARLIE (+3 pts)";
                        _hitFeedbackColor = new Color(1f, 0.8f, 0.2f, 1f);
                        break;
                    case IPSCZoneType.Delta:
                        _hitFeedbackStr = $"🎯 {targetName} - DELTA (+1 pt)";
                        _hitFeedbackColor = new Color(0.4f, 0.8f, 1f, 1f);
                        break;
                }
            }
        }

        private void UpdateAmmoDisplay(int current, int max)
        {
            _ammoStr = $"{current} / {max}";
        }

        private void UpdateTimerStateDisplay(TimerState state)
        {
            switch (state)
            {
                case TimerState.Idle:
                    _timerStatus = "Aguardando início [T]";
                    _showResult = false;
                    break;
                case TimerState.Standby:
                    _timerStatus = "STANDBY...";
                    _showResult = false;
                    break;
                case TimerState.Running:
                    _timerStatus = "PISTA ABERTA (EM EXECUÇÃO)";
                    _showResult = false;
                    break;
                case TimerState.Finished:
                    _timerStatus = "PISTA FINALIZADA - [U] para Obrear";
                    break;
            }
        }

        private void UpdateShotDisplay(float totalTime, float split)
        {
            _lastSplitStr = $"Split: {split:F2}s";
        }

        private void ShowFinalScoreSummary(StageScoreResult result)
        {
            _lastResult = result;
            _showResult = true;
        }

        private void InitStyles()
        {
            if (_blackBgTex == null)
            {
                _blackBgTex = new Texture2D(1, 1);
                _blackBgTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.78f));
                _blackBgTex.Apply();
            }

            if (_timerStyle == null)
            {
                _timerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 32,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                _timerStyle.normal.textColor = Color.white;

                _statusStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                _statusStyle.normal.textColor = Color.yellow;

                _titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                _titleStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);

                _splitStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft
                };
                _splitStyle.normal.textColor = Color.cyan;

                _ammoStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 28,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                _ammoStyle.normal.textColor = Color.white;

                _resultBoxStyle = new GUIStyle(GUI.skin.box);
                _resultBoxStyle.normal.background = _blackBgTex;

                _resultTextStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 17,
                    alignment = TextAnchor.UpperLeft
                };
                _resultTextStyle.normal.textColor = Color.white;

                _controlsHelpStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    alignment = TextAnchor.UpperLeft
                };
                _controlsHelpStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f, 0.95f);

                _hitFeedbackStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };

                _arsenalStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
            }
        }

        private void OnGUI()
        {
            InitStyles();

            float screenW = Screen.width;
            float screenH = Screen.height;

            // 1. Painel Superior Central: Shot Timer
            float timerPanelW = 340f;
            float timerPanelH = 80f;
            Rect timerRect = new Rect((screenW - timerPanelW) * 0.5f, 20f, timerPanelW, timerPanelH);
            GUI.Box(timerRect, GUIContent.none, _resultBoxStyle);

            GUI.Label(new Rect(timerRect.x, timerRect.y + 6f, timerPanelW, 22f), _timerStatus, _statusStyle);
            GUI.Label(new Rect(timerRect.x, timerRect.y + 28f, timerPanelW, 44f), _currentTimeStr, _timerStyle);

            // 1.1 Feedback Visual de Impacto no Alvo
            if (!string.IsNullOrEmpty(_hitFeedbackStr))
            {
                _hitFeedbackStyle.normal.textColor = _hitFeedbackColor;
                Rect hitRect = new Rect((screenW - 460f) * 0.5f, timerRect.y + timerPanelH + 6f, 460f, 30f);
                GUI.Box(hitRect, GUIContent.none, _resultBoxStyle);
                GUI.Label(hitRect, _hitFeedbackStr, _hitFeedbackStyle);
            }

            // 2. Split do último tiro (ao lado do cronômetro)
            GUI.Label(new Rect(timerRect.x + timerPanelW + 15f, 40f, 220f, 32f), _lastSplitStr, _splitStyle);

            // 3. Munição no Carregador (canto inferior direito)
            float ammoW = 150f;
            float ammoH = 60f;
            Rect ammoRect = new Rect(screenW - ammoW - 25f, screenH - ammoH - 25f, ammoW, ammoH);
            GUI.Box(ammoRect, GUIContent.none, _resultBoxStyle);
            GUI.Label(ammoRect, _ammoStr, _ammoStyle);

            // 4. Guia de Controles (canto inferior esquerdo)
            float helpW = 420f;
            float helpH = 80f;
            Rect helpRect = new Rect(20f, screenH - helpH - 20f, helpW, helpH);
            GUI.Box(helpRect, GUIContent.none, _resultBoxStyle);
            string helpText = " [T] Iniciar Cronômetro  |  [Y] Encerrar Pista\n [R] Recarregar  |  [U] Obrear  |  [B] Arsenal CS\n [1, 2, 3] Troca Rápida  |  [Botão Dir.] Mira ADS";
            GUI.Label(new Rect(helpRect.x + 8f, helpRect.y + 8f, helpW - 16f, helpH - 16f), helpText, _controlsHelpStyle);

            // 4.1 Barra de Arsenal no Rodapé Central
            float arsW = 620f;
            float arsH = 34f;
            Rect arsRect = new Rect((screenW - arsW) * 0.5f, screenH - arsH - 20f, arsW, arsH);
            GUI.Box(arsRect, GUIContent.none, _resultBoxStyle);

            SelectedWeaponType curW = (TacticalGameManager.Instance != null) ? TacticalGameManager.Instance.SelectedWeapon : SelectedWeaponType.Pistol_9mm;
            string opt1 = (curW == SelectedWeaponType.Pistol_Beretta_M9T_Black || curW == SelectedWeaponType.Pistol_9mm) ? "<b><color=#00FFAA>[1] 🔫 Pistola</color></b>" : "<color=#888888>[1] Pistola</color>";
            string opt2 = (curW == SelectedWeaponType.Revolver_DanWesson_357 || curW == SelectedWeaponType.Revolver_DanWesson) ? "<b><color=#FFAA00>[2] 🤠 .357</color></b>" : "<color=#888888>[2] .357</color>";
            string opt3 = (curW == SelectedWeaponType.Rifle_AKM_Tactical || curW == SelectedWeaponType.SMG_Tactical) ? "<b><color=#00CCFF>[3] ⚡ AKM/SMG</color></b>" : "<color=#888888>[3] AKM/SMG</color>";
            string optBuy = "<b><color=#FFD700>[B] 🛒 ARSENAL SHOWCASE</color></b>";

            _arsenalStyle.normal.textColor = Color.white;
            GUI.Label(arsRect, $"{opt1}    {opt2}    {opt3}    {optBuy}", _arsenalStyle);

            // 4.2 Modal do Arsenal / Buy Menu (Estilo Counter-Strike)
            if (_showBuyMenu)
            {
                DrawBuyMenuModal(screenW, screenH);
            }

            // 5. Painel de Resultados (Centro da tela no fim de pista)
            if (_showResult)
            {
                float resW = 480f;
                float resH = 350f;
                Rect resRect = new Rect((screenW - resW) * 0.5f, (screenH - resH) * 0.5f, resW, resH);
                GUI.Box(resRect, GUIContent.none, _resultBoxStyle);

                string summary =
                    "               <b>=== SUMÁRIO IPSC ===</b>\n\n" +
                    $"  Tempo Total da Pista:  {_lastResult.TotalTime:F2} segundos\n" +
                    $"  Disparos Efetuados:  {_lastResult.ShotsFired}\n" +
                    $"  Zonas:  {_lastResult.AlphaCount} Alpha (5pts)  |  {_lastResult.CharlieCount} Charlie (3pts)  |  {_lastResult.DeltaCount} Delta (1pt)\n" +
                    $"  Penalidades:  {_lastResult.MissCount} Misses  |  {_lastResult.NoShootCount} No-Shoots (-{_lastResult.Penalties} pts)\n" +
                    $"  Pontos Líquidos:  {_lastResult.FinalPoints} pts\n\n" +
                    $"  <b><color=#00FF88>HIT FACTOR FINAL:  {_lastResult.HitFactor:F4}</color></b>\n\n" +
                    "  <i>Pressione [U] para Obrear os alvos ou [T] para nova tentativa.</i>";

                GUI.Label(new Rect(resRect.x + 20f, resRect.y + 16f, resW - 40f, resH - 32f), summary, _resultTextStyle);
            }
        }

        private void DrawBuyMenuModal(float screenW, float screenH)
        {
            // Fundo escuro semi-transparente
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.DrawTexture(new Rect(0, 0, screenW, screenH), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float modalW = Mathf.Min(880f, screenW - 40f);
            float modalH = Mathf.Min(560f, screenH - 40f);
            Rect modalRect = new Rect((screenW - modalW) * 0.5f, (screenH - modalH) * 0.5f, modalW, modalH);

            GUILayout.BeginArea(modalRect, _resultBoxStyle);

            GUILayout.Label("🛒 <b><color=#FFD700>ARSENAL SHOWCASE - SELEÇÃO DE ARMAMENTO</color></b>", _titleStyle);
            GUILayout.Label("Selecione a categoria e clique na arma para equipar instantaneamente em combate [Tecla B para fechar]", _controlsHelpStyle);
            GUILayout.Space(12);

            // Abas de Categorias
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🔫 PISTOLAS", GUILayout.Height(36))) _buyMenuCategory = WeaponCategory.Pistols;
            if (GUILayout.Button("💥 REVÓLVERES", GUILayout.Height(36))) _buyMenuCategory = WeaponCategory.Revolvers;
            if (GUILayout.Button("⚡ SUBMETRALHADORAS", GUILayout.Height(36))) _buyMenuCategory = WeaponCategory.SubmachineGuns;
            if (GUILayout.Button("🎯 FUZIS DE ASSALTO", GUILayout.Height(36))) _buyMenuCategory = WeaponCategory.AssaultRifles;
            GUILayout.EndHorizontal();

            GUILayout.Space(12);
            GUILayout.Label($"<b>MODELOS DISPONÍVEIS: {_buyMenuCategory.ToString().ToUpper()}</b>", _statusStyle);
            GUILayout.Space(8);

            SelectedWeaponType curWeapon = (TacticalGameManager.Instance != null) ? TacticalGameManager.Instance.SelectedWeapon : SelectedWeaponType.Pistol_9mm;

            // Grade de Armas da Categoria
            switch (_buyMenuCategory)
            {
                case WeaponCategory.Pistols:
                    GUILayout.BeginHorizontal();
                    DrawBuyCard(SelectedWeaponType.Pistol_Beretta_M9T_Black, "BERETTA M9T (BLACK)", "9x19mm", "17 Tiros", curWeapon);
                    DrawBuyCard(SelectedWeaponType.Pistol_Beretta_M9T_Tan, "BERETTA M9T (TAN)", "9x19mm", "17 Tiros", curWeapon);
                    GUILayout.EndHorizontal();
                    GUILayout.Space(6);
                    GUILayout.BeginHorizontal();
                    DrawBuyCard(SelectedWeaponType.Pistol_Tactical_Glock, "GLOCK 17 COMBAT", "9x19mm", "17 Tiros", curWeapon);
                    DrawBuyCard(SelectedWeaponType.Pistol_Combat_Custom, "COLT 1911 CUSTOM", ".45 ACP", "15 Tiros", curWeapon);
                    GUILayout.EndHorizontal();
                    break;

                case WeaponCategory.Revolvers:
                    GUILayout.BeginHorizontal();
                    DrawBuyCard(SelectedWeaponType.Revolver_DanWesson_357, "DAN WESSON 715", ".357 MAG", "6 Tiros", curWeapon);
                    DrawBuyCard(SelectedWeaponType.Revolver_Superhawk_Tactical, "SUPERHAWK TACTICAL", ".44 MAG", "6 Tiros", curWeapon);
                    GUILayout.EndHorizontal();
                    break;

                case WeaponCategory.SubmachineGuns:
                    GUILayout.BeginHorizontal();
                    DrawBuyCard(SelectedWeaponType.SMG_Strongarm_Tactical, "MP5 STRONGARM", "9mm AUTO", "30 Tiros", curWeapon);
                    DrawBuyCard(SelectedWeaponType.SMG_MP_Vector, "UMP-45 TACTICAL", ".45 AUTO", "30 Tiros", curWeapon);
                    DrawBuyCard(SelectedWeaponType.SMG_P90_Compact, "P90 BULLPUP", "5.7x28mm", "50 Tiros", curWeapon);
                    GUILayout.EndHorizontal();
                    break;

                case WeaponCategory.AssaultRifles:
                    GUILayout.BeginHorizontal();
                    DrawBuyCard(SelectedWeaponType.Rifle_AKM_Tactical, "AKM 7.62x39mm RUSSIAN", "7.62mm", "30 Tiros", curWeapon);
                    GUILayout.EndHorizontal();
                    break;
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("FECHAR ARSENAL [ESC / B]", GUILayout.Height(38)))
            {
                _showBuyMenu = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            GUILayout.EndArea();
        }

        private void DrawBuyCard(SelectedWeaponType weapon, string name, string caliber, string capacity, SelectedWeaponType current)
        {
            bool isCurrent = (weapon == current);
            string btnText = isCurrent 
                ? $"<b><color=#FF3333>{name}</color></b>\n<color=#FFFFFF>{caliber} | {capacity}</color>\n<b><color=#00FFAA>[✓ EQUIPADA]</color></b>" 
                : $"<b><color=#FF3333>{name}</color></b>\n<color=#DDDDDD>{caliber} | {capacity}</color>\n<color=#FFD700>[ CLIQUE P/ EQUIPAR ]</color>";

            if (GUILayout.Button(btnText, GUILayout.Height(64)))
            {
                SceneGeneratorCore.SwitchEquippedWeapon(weapon);
                if (TacticalGameManager.Instance != null)
                {
                    TacticalGameManager.Instance.SelectedWeapon = weapon;
                }
                _showBuyMenu = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}
