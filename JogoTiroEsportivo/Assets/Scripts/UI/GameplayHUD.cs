using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace IPSCSimulator.UI
{
    using IPSCSimulator.Match;
    using IPSCSimulator.Targets;
    using IPSCSimulator.Weapons;

    /// <summary>
    /// HUD de jogo fotorrealista e minimalista estilo Counter-Strike 2.
    /// Utiliza Unity Canvas UGUI e TextMeshPro para máxima nitidez:
    /// 1. Dynamic Crosshair (4 traços com expansão por movimento/tiro e fade-out no ADS).
    /// 2. Contador de Munição translúcido no canto inferior direito.
    /// 3. Painel superior central de Cronômetro (Shot Timer) e Placar de Alvos.
    /// </summary>
    public class GameplayHUD : MonoBehaviour
    {
        public static GameplayHUD Instance { get; private set; }

        [Header("1. Mira Dinâmica (Dynamic Crosshair CS2)")]
        [Tooltip("CanvasGroup que contém as 4 linhas para controle de transparência (Fade no ADS).")]
        [SerializeField] private CanvasGroup crosshairCanvasGroup;
        [SerializeField] private RectTransform crosshairTop;
        [SerializeField] private RectTransform crosshairBottom;
        [SerializeField] private RectTransform crosshairLeft;
        [SerializeField] private RectTransform crosshairRight;

        [Tooltip("Distância base das linhas do centro em repouso (pixels).")]
        [SerializeField] private float crosshairBaseGap = 7f;

        [Tooltip("Expansão máxima da mira ao correr ou disparar (pixels).")]
        [SerializeField] private float crosshairMaxGap = 36f;

        [Tooltip("Velocidade com que a mira abre e fecha acompanhando o movimento.")]
        [SerializeField] private float crosshairSmoothSpeed = 16f;

        [Header("2. Contador de Munição (Canto Inferior Direito)")]
        [SerializeField] private CanvasGroup ammoCanvasGroup;
        [SerializeField] private TextMeshProUGUI currentAmmoText;
        [SerializeField] private TextMeshProUGUI reserveAmmoText;
        [SerializeField] private TextMeshProUGUI weaponNameText;

        [Header("3. Cronômetro e Placar da Pista (Topo Central)")]
        [SerializeField] private CanvasGroup timerCanvasGroup;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI timerStatusText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI splitText;

        [Header("Referências Opcionais (Auto-detectadas)")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private WeaponProceduralMotion proceduralMotion;

        // Estados internos
        private float _targetCrosshairGap;
        private float _currentCrosshairGap;
        private bool _isAimingADS = false;
        private int _totalTargetsCount = 0;
        private int _currentHitsCount = 0;
        private int _currentScorePoints = 0;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);

            _currentCrosshairGap = crosshairBaseGap;
            _targetCrosshairGap = crosshairBaseGap;

            if (characterController == null)
            {
                characterController = FindAnyObjectByType<CharacterController>();
            }

            if (proceduralMotion == null)
            {
                proceduralMotion = FindAnyObjectByType<WeaponProceduralMotion>();
            }
        }

        private void OnEnable()
        {
            // Inscrição em eventos públicos do jogo
            Weapon.OnAmmoChanged += HandleAmmoChanged;
            Weapon.OnWeaponFired += HandleWeaponFired;
            RangeOfficerTimer.OnTimerStateChanged += HandleTimerStateChanged;
            RangeOfficerTimer.OnShotRegistered += HandleShotRegistered;
            IPSCTarget.OnTargetZoneHitGlobal += HandleTargetZoneHit;
        }

        private void OnDisable()
        {
            Weapon.OnAmmoChanged -= HandleAmmoChanged;
            Weapon.OnWeaponFired -= HandleWeaponFired;
            RangeOfficerTimer.OnTimerStateChanged -= HandleTimerStateChanged;
            RangeOfficerTimer.OnShotRegistered -= HandleShotRegistered;
            IPSCTarget.OnTargetZoneHitGlobal -= HandleTargetZoneHit;
        }

        private void Start()
        {
            // Conta alvos na cena
            IPSCTarget[] targets = FindObjectsByType<IPSCTarget>(FindObjectsSortMode.None);
            _totalTargetsCount = (targets != null) ? targets.Length : 0;
            UpdateScoreDisplay();
        }

        private void Update()
        {
            UpdateCrosshairMotion();
            UpdateTimerClock();
        }

        // ========================================================================================
        // 1. MIRA DINÂMICA (CROSSHAIR ESTILO CS2)
        // ========================================================================================
        private void UpdateCrosshairMotion()
        {
            // Detecta se o jogador está em ADS
            if (proceduralMotion != null)
            {
                _isAimingADS = proceduralMotion.IsAiming;
            }
            else
            {
                _isAimingADS = Input.GetMouseButton(1);
            }

            // A) Suavização de Fade no ADS: desaparece no botão direito, reaparece no quadril
            if (crosshairCanvasGroup != null)
            {
                float targetAlpha = _isAimingADS ? 0.0f : 1.0f;
                crosshairCanvasGroup.alpha = Mathf.MoveTowards(crosshairCanvasGroup.alpha, targetAlpha, Time.deltaTime * 12f);
            }

            // B) Expansão por velocidade de movimento
            float speed = 0f;
            if (characterController != null)
            {
                speed = characterController.velocity.magnitude;
            }

            float movementFactor = Mathf.Clamp01(speed / 5.5f);
            float desiredGap = Mathf.Lerp(crosshairBaseGap, crosshairMaxGap, movementFactor);

            // Se o alvo não estiver recebendo coice de disparo recente, recupera para a abertura do movimento
            _targetCrosshairGap = Mathf.Max(desiredGap, Mathf.MoveTowards(_targetCrosshairGap, desiredGap, Time.deltaTime * 35f));

            _currentCrosshairGap = Mathf.Lerp(_currentCrosshairGap, _targetCrosshairGap, Time.deltaTime * crosshairSmoothSpeed);

            ApplyCrosshairGap(_currentCrosshairGap);
        }

        private void ApplyCrosshairGap(float gap)
        {
            if (crosshairTop != null) crosshairTop.anchoredPosition = new Vector2(0f, gap);
            if (crosshairBottom != null) crosshairBottom.anchoredPosition = new Vector2(0f, -gap);
            if (crosshairLeft != null) crosshairLeft.anchoredPosition = new Vector2(-gap, 0f);
            if (crosshairRight != null) crosshairRight.anchoredPosition = new Vector2(gap, 0f);
        }

        // ========================================================================================
        // 2. MÉTODOS PÚBLICOS DE ATUALIZAÇÃO
        // ========================================================================================
        /// <summary>
        /// Atualiza o mostrador minimalista de munição (pente / reserva).
        /// </summary>
        public void UpdateAmmo(int current, int reserve)
        {
            if (currentAmmoText != null)
            {
                currentAmmoText.text = current.ToString("D2");
                // Fica vermelho sutil quando estiver com menos de 4 balas
                currentAmmoText.color = (current <= 3) ? new Color(1f, 0.25f, 0.25f, 1f) : Color.white;
            }

            if (reserveAmmoText != null)
            {
                reserveAmmoText.text = $" / {reserve}";
            }
        }

        /// <summary>
        /// Atualiza o nome da arma ativa no HUD inferior.
        /// </summary>
        public void UpdateWeaponName(string name)
        {
            if (weaponNameText != null)
            {
                weaponNameText.text = name.ToUpper();
            }
        }

        /// <summary>
        /// Atualiza o cronômetro central da pista.
        /// </summary>
        public void UpdateTimer(float timeSeconds, string statusText, Color statusColor)
        {
            if (timerText != null)
            {
                timerText.text = $"{timeSeconds:00.00}s";
            }

            if (timerStatusText != null)
            {
                timerStatusText.text = statusText.ToUpper();
                timerStatusText.color = statusColor;
            }
        }

        /// <summary>
        /// Atualiza o placar de alvos atingidos e pontos.
        /// </summary>
        public void UpdateScore(int hits, int totalTargets, int score)
        {
            _currentHitsCount = hits;
            _totalTargetsCount = totalTargets;
            _currentScorePoints = score;
            UpdateScoreDisplay();
        }

        /// <summary>
        /// Atualiza o mostrador de split (tempo entre os dois últimos tiros).
        /// </summary>
        public void UpdateSplit(float splitSeconds)
        {
            if (splitText != null)
            {
                splitText.text = $"SPLIT: {splitSeconds:F2}s";
            }
        }

        /// <summary>
        /// Força a mira a se expandir momentaneamente (coice de disparo).
        /// </summary>
        public void TriggerCrosshairKick(float kickPixels = 12f)
        {
            _targetCrosshairGap = Mathf.Min(crosshairMaxGap * 1.35f, _targetCrosshairGap + kickPixels);
        }

        // ========================================================================================
        // 3. LISTENERS DE EVENTOS INTERNOS DO JOGO
        // ========================================================================================
        private void HandleAmmoChanged(int current, int max)
        {
            UpdateAmmo(current, max);
        }

        private void HandleWeaponFired()
        {
            TriggerCrosshairKick(10f);
        }

        private void HandleTimerStateChanged(TimerState state)
        {
            switch (state)
            {
                case TimerState.Idle:
                    UpdateTimer(0f, "STANDBY [T]", new Color(1f, 0.85f, 0.2f));
                    break;
                case TimerState.Standby:
                    UpdateTimer(0f, "ATENÇÃO...", new Color(1f, 0.5f, 0.1f));
                    break;
                case TimerState.Running:
                    UpdateTimer(0f, "PISTA ABERTA", new Color(0f, 1f, 0.65f));
                    break;
                case TimerState.Finished:
                    UpdateTimer(RangeOfficerTimer.Instance != null ? RangeOfficerTimer.Instance.CurrentTime : 0f, "FINALIZADA [U]", new Color(0.3f, 0.8f, 1f));
                    break;
            }
        }

        private void HandleShotRegistered(float totalTime, float split)
        {
            UpdateSplit(split);
        }

        private void HandleTargetZoneHit(string targetName, IPSCZoneType zone, bool isNoShoot)
        {
            _currentHitsCount++;

            switch (zone)
            {
                case IPSCZoneType.Alpha: _currentScorePoints += 5; break;
                case IPSCZoneType.Charlie: _currentScorePoints += 3; break;
                case IPSCZoneType.Delta: _currentScorePoints += 1; break;
                case IPSCZoneType.NoShoot: _currentScorePoints -= 10; break;
            }

            UpdateScoreDisplay();
        }

        private void UpdateTimerClock()
        {
            if (RangeOfficerTimer.Instance != null && RangeOfficerTimer.Instance.State == TimerState.Running)
            {
                if (timerText != null)
                {
                    timerText.text = $"{RangeOfficerTimer.Instance.CurrentTime:00.00}s";
                }
            }
        }

        private void UpdateScoreDisplay()
        {
            if (scoreText != null)
            {
                scoreText.text = $"ALVOS: {_currentHitsCount}/{_totalTargetsCount * 2}  |  PONTOS: {_currentScorePoints}";
            }
        }

        // ========================================================================================
        // 4. CRIADOR PROCEDURAL COMPLETO DO CANVAS CS2 (EXECUÇÃO AUTOMÁTICA EM CENA)
        // ========================================================================================
        /// <summary>
        /// Constrói a estrutura completa de Canvas e elementos TextMeshPro proceduralmente.
        /// Garante que o HUD esteja 100% funcional sem necessidade de montagem manual no editor.
        /// </summary>
        public static GameplayHUD CreateProceduralHUD(Transform parent = null)
        {
            // Remove instâncias antigas duplicadas
            GameplayHUD existing = FindAnyObjectByType<GameplayHUD>();
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }

            GameObject canvasObj = new GameObject("CS2_Gameplay_HUD_Canvas");
            if (parent != null) canvasObj.transform.SetParent(parent);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            GameplayHUD hud = canvasObj.AddComponent<GameplayHUD>();

            // ------------------------------------------------------------------------------------
            // A) MIRA DINÂMICA (CROSSHAIR NO CENTRO)
            // ------------------------------------------------------------------------------------
            GameObject crosshairRoot = new GameObject("DynamicCrosshair");
            crosshairRoot.transform.SetParent(canvasObj.transform, false);
            RectTransform chRect = crosshairRoot.AddComponent<RectTransform>();
            chRect.anchorMin = new Vector2(0.5f, 0.5f);
            chRect.anchorMax = new Vector2(0.5f, 0.5f);
            chRect.sizeDelta = new Vector2(100f, 100f);
            chRect.anchoredPosition = Vector2.zero;

            CanvasGroup chCg = crosshairRoot.AddComponent<CanvasGroup>();
            hud.crosshairCanvasGroup = chCg;

            Color chColor = new Color(0.92f, 0.96f, 1.0f, 0.95f);
            float lineW = 2.0f;
            float lineL = 10.0f;

            hud.crosshairTop = CreateCrosshairLine(crosshairRoot.transform, "Line_Top", new Vector2(lineW, lineL), chColor);
            hud.crosshairBottom = CreateCrosshairLine(crosshairRoot.transform, "Line_Bottom", new Vector2(lineW, lineL), chColor);
            hud.crosshairLeft = CreateCrosshairLine(crosshairRoot.transform, "Line_Left", new Vector2(lineL, lineW), chColor);
            hud.crosshairRight = CreateCrosshairLine(crosshairRoot.transform, "Line_Right", new Vector2(lineL, lineW), chColor);

            // ------------------------------------------------------------------------------------
            // B) CONTADOR DE MUNIÇÃO (CANTO INFERIOR DIREITO)
            // ------------------------------------------------------------------------------------
            GameObject ammoBox = new GameObject("AmmoPanel_BottomRight");
            ammoBox.transform.SetParent(canvasObj.transform, false);
            RectTransform ammoRect = ammoBox.AddComponent<RectTransform>();
            ammoRect.anchorMin = new Vector2(1f, 0f);
            ammoRect.anchorMax = new Vector2(1f, 0f);
            ammoRect.pivot = new Vector2(1f, 0f);
            ammoRect.sizeDelta = new Vector2(210f, 75f);
            ammoRect.anchoredPosition = new Vector2(-35f, 30f);

            // Fundo translúcido escuro
            Image ammoBg = ammoBox.AddComponent<Image>();
            ammoBg.color = new Color(0.04f, 0.05f, 0.08f, 0.72f);
            hud.ammoCanvasGroup = ammoBox.AddComponent<CanvasGroup>();

            // Texto Munição Atual (Grande e destacado)
            GameObject curAmmoObj = new GameObject("Text_CurrentAmmo");
            curAmmoObj.transform.SetParent(ammoBox.transform, false);
            RectTransform curRect = curAmmoObj.AddComponent<RectTransform>();
            curRect.anchorMin = new Vector2(0f, 0.3f);
            curRect.anchorMax = new Vector2(0.55f, 1f);
            curRect.anchoredPosition = new Vector2(15f, 0f);
            TextMeshProUGUI curText = curAmmoObj.AddComponent<TextMeshProUGUI>();
            curText.text = "17";
            curText.fontSize = 42;
            curText.fontStyle = FontStyles.Bold;
            curText.alignment = TextAlignmentOptions.MidlineRight;
            curText.color = Color.white;
            hud.currentAmmoText = curText;

            // Texto Reserva
            GameObject resAmmoObj = new GameObject("Text_ReserveAmmo");
            resAmmoObj.transform.SetParent(ammoBox.transform, false);
            RectTransform resRect = resAmmoObj.AddComponent<RectTransform>();
            resRect.anchorMin = new Vector2(0.55f, 0.35f);
            resRect.anchorMax = new Vector2(1f, 0.95f);
            resRect.anchoredPosition = new Vector2(5f, -2f);
            TextMeshProUGUI resText = resAmmoObj.AddComponent<TextMeshProUGUI>();
            resText.text = " / 17";
            resText.fontSize = 24;
            resText.fontStyle = FontStyles.Normal;
            resText.alignment = TextAlignmentOptions.MidlineLeft;
            resText.color = new Color(0.7f, 0.75f, 0.82f, 0.9f);
            hud.reserveAmmoText = resText;

            // Nome da Arma abaixo da contagem
            GameObject nameObj = new GameObject("Text_WeaponName");
            nameObj.transform.SetParent(ammoBox.transform, false);
            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(1f, 0.35f);
            nameRect.anchoredPosition = new Vector2(0f, 6f);
            TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = "PISTOLA 9MM";
            nameText.fontSize = 13;
            nameText.fontStyle = FontStyles.Bold;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = new Color(0.95f, 0.78f, 0.25f, 0.95f);
            hud.weaponNameText = nameText;

            // ------------------------------------------------------------------------------------
            // C) PAINEL SUPERIOR CENTRAL: CRONÔMETRO E PLACAR
            // ------------------------------------------------------------------------------------
            GameObject timerPanel = new GameObject("TimerPanel_TopCenter");
            timerPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform tpRect = timerPanel.AddComponent<RectTransform>();
            tpRect.anchorMin = new Vector2(0.5f, 1f);
            tpRect.anchorMax = new Vector2(0.5f, 1f);
            tpRect.pivot = new Vector2(0.5f, 1f);
            tpRect.sizeDelta = new Vector2(380f, 85f);
            tpRect.anchoredPosition = new Vector2(0f, -20f);

            Image tpBg = timerPanel.AddComponent<Image>();
            tpBg.color = new Color(0.05f, 0.06f, 0.09f, 0.78f);
            hud.timerCanvasGroup = timerPanel.AddComponent<CanvasGroup>();

            // Status da pista (ex: STANDBY, PISTA ABERTA)
            GameObject stObj = new GameObject("Text_Status");
            stObj.transform.SetParent(timerPanel.transform, false);
            RectTransform stRect = stObj.AddComponent<RectTransform>();
            stRect.anchorMin = new Vector2(0f, 0.65f);
            stRect.anchorMax = new Vector2(1f, 1f);
            stRect.anchoredPosition = new Vector2(0f, -4f);
            TextMeshProUGUI stText = stObj.AddComponent<TextMeshProUGUI>();
            stText.text = "PRESSIONE [T] PARA INICIAR";
            stText.fontSize = 13;
            stText.fontStyle = FontStyles.Bold;
            stText.alignment = TextAlignmentOptions.Center;
            stText.color = new Color(1f, 0.85f, 0.2f);
            hud.timerStatusText = stText;

            // Dígitos do Cronômetro
            GameObject clockObj = new GameObject("Text_Clock");
            clockObj.transform.SetParent(timerPanel.transform, false);
            RectTransform clockRect = clockObj.AddComponent<RectTransform>();
            clockRect.anchorMin = new Vector2(0f, 0.25f);
            clockRect.anchorMax = new Vector2(1f, 0.75f);
            clockRect.anchoredPosition = Vector2.zero;
            TextMeshProUGUI clockText = clockObj.AddComponent<TextMeshProUGUI>();
            clockText.text = "00.00s";
            clockText.fontSize = 32;
            clockText.fontStyle = FontStyles.Bold;
            clockText.alignment = TextAlignmentOptions.Center;
            clockText.color = Color.white;
            hud.timerText = clockText;

            // Placar de alvos e Split
            GameObject scoreObj = new GameObject("Text_Score");
            scoreObj.transform.SetParent(timerPanel.transform, false);
            RectTransform scRect = scoreObj.AddComponent<RectTransform>();
            scRect.anchorMin = new Vector2(0f, 0f);
            scRect.anchorMax = new Vector2(1f, 0.30f);
            scRect.anchoredPosition = new Vector2(0f, 4f);
            TextMeshProUGUI scText = scoreObj.AddComponent<TextMeshProUGUI>();
            scText.text = "ALVOS: 0/0  |  PONTOS: 0";
            scText.fontSize = 12;
            scText.alignment = TextAlignmentOptions.Center;
            scText.color = new Color(0.85f, 0.90f, 0.95f, 0.9f);
            hud.scoreText = scText;

            // Split lateral ao cronômetro
            GameObject splitObj = new GameObject("Text_SplitSide");
            splitObj.transform.SetParent(timerPanel.transform, false);
            RectTransform splitRect = splitObj.AddComponent<RectTransform>();
            splitRect.anchorMin = new Vector2(1f, 0.25f);
            splitRect.anchorMax = new Vector2(1f, 0.75f);
            splitRect.pivot = new Vector2(0f, 0.5f);
            splitRect.anchoredPosition = new Vector2(15f, 0f);
            splitRect.sizeDelta = new Vector2(160f, 30f);
            TextMeshProUGUI spText = splitObj.AddComponent<TextMeshProUGUI>();
            spText.text = "SPLIT: --";
            spText.fontSize = 16;
            spText.fontStyle = FontStyles.Bold;
            spText.alignment = TextAlignmentOptions.MidlineLeft;
            spText.color = new Color(0.2f, 0.9f, 1f, 0.9f);
            hud.splitText = spText;

            return hud;
        }

        private static RectTransform CreateCrosshairLine(Transform parent, string name, Vector2 size, Color color)
        {
            GameObject lineObj = new GameObject(name);
            lineObj.transform.SetParent(parent, false);
            RectTransform rt = lineObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;

            Image img = lineObj.AddComponent<Image>();
            img.color = color;

            return rt;
        }
    }
}
