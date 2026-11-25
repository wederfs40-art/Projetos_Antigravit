using UnityEngine;

namespace IPSCSimulator.UI
{
    using IPSCSimulator.Match;
    using IPSCSimulator.Weapons;

    /// <summary>
    /// Gerencia a exibição visual em tela (HUD) do Shot Timer, Splits, Munição e Resultado Final.
    /// Utiliza IMGUI nativo (OnGUI) para garantir 100% de compatibilidade em qualquer versão da Unity
    /// sem depender de pacotes externos como uGUI ou TextMeshPro.
    /// </summary>
    public class IPSCUIManager : MonoBehaviour
    {
        private string _timerStatus = "Pressione [T] para Standby";
        private string _currentTimeStr = "00.00s";
        private string _lastSplitStr = "Split: --";
        private string _ammoStr = "17 / 17";
        private bool _showResult = false;
        private StageScoreResult _lastResult;

        private GUIStyle _timerStyle;
        private GUIStyle _statusStyle;
        private GUIStyle _splitStyle;
        private GUIStyle _ammoStyle;
        private GUIStyle _resultBoxStyle;
        private GUIStyle _resultTextStyle;
        private GUIStyle _controlsHelpStyle;
        private Texture2D _blackBgTex;

        private void OnEnable()
        {
            Weapon.OnAmmoChanged += UpdateAmmoDisplay;
            RangeOfficerTimer.OnTimerStateChanged += UpdateTimerStateDisplay;
            RangeOfficerTimer.OnShotRegistered += UpdateShotDisplay;
            RangeOfficerTimer.OnStageFinished += ShowFinalScoreSummary;
        }

        private void OnDisable()
        {
            Weapon.OnAmmoChanged -= UpdateAmmoDisplay;
            RangeOfficerTimer.OnTimerStateChanged -= UpdateTimerStateDisplay;
            RangeOfficerTimer.OnShotRegistered -= UpdateShotDisplay;
            RangeOfficerTimer.OnStageFinished -= ShowFinalScoreSummary;
        }

        private void Update()
        {
            if (RangeOfficerTimer.Instance != null && RangeOfficerTimer.Instance.State == TimerState.Running)
            {
                _currentTimeStr = $"{RangeOfficerTimer.Instance.CurrentTime:F2}s";
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

            // 2. Split do último tiro (ao lado do cronômetro)
            GUI.Label(new Rect(timerRect.x + timerPanelW + 15f, 40f, 220f, 32f), _lastSplitStr, _splitStyle);

            // 3. Munição no Carregador (canto inferior direito)
            float ammoW = 150f;
            float ammoH = 60f;
            Rect ammoRect = new Rect(screenW - ammoW - 25f, screenH - ammoH - 25f, ammoW, ammoH);
            GUI.Box(ammoRect, GUIContent.none, _resultBoxStyle);
            GUI.Label(ammoRect, _ammoStr, _ammoStyle);

            // 4. Guia de Controles (canto inferior esquerdo)
            float helpW = 380f;
            float helpH = 75f;
            Rect helpRect = new Rect(20f, screenH - helpH - 20f, helpW, helpH);
            GUI.Box(helpRect, GUIContent.none, _resultBoxStyle);
            string helpText = " [T] Standby / Iniciar Pista   |  [Y] Encerrar Pista\n [R] Recarregar Arma          |  [U] Obrear Alvos\n [Botão Dir.] Mira ADS        |  [Shift] Prender Respiração";
            GUI.Label(new Rect(helpRect.x + 8f, helpRect.y + 8f, helpW - 16f, helpH - 16f), helpText, _controlsHelpStyle);

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
    }
}
