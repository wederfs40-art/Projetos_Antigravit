using UnityEngine;

namespace IPSCSimulator.Weapons
{
    /// <summary>
    /// Controla a movimentação visual procedural da arma e da câmera:
    /// 1. Alinhamento Perfeito de Mira (ADS): Desliza a arma suavemente para as coordenadas
    ///    exatas do WeaponData e ZERA o balanço de passos (Bobbing) e a inércia do mouse (Sway).
    /// 2. Padrão de Recuo Controlável (Spray Pattern estilo CS2): Aplica coice angular na câmera
    ///    e no viewmodel a cada tiro, permitindo que o jogador compense o spray puxando o mouse para baixo.
    /// </summary>
    public class WeaponProceduralMotion : MonoBehaviour
    {
        [Header("Configuração da Arma (ScriptableObject)")]
        [Tooltip("ScriptableObject com as coordenadas de ADS, FOV e curvas de recuo.")]
        [SerializeField] private WeaponData weaponData;

        [Header("Referências da Hierarquia")]
        [Tooltip("Câmera do jogador (onde o recuo angular e o zoom de FOV serão aplicados).")]
        [SerializeField] private Camera playerCamera;

        [Tooltip("Transform da arma ou suporte (se nulo, usa o próprio GameObject onde este script está anexado).")]
        [SerializeField] private Transform weaponTransform;

        [Tooltip("CharacterController do jogador para leitura de movimento e passada.")]
        [SerializeField] private CharacterController characterController;

        [Header("Posicionamento no Quadril (Hipfire)")]
        [Tooltip("Posição de descanso da arma quando não estiver mirando.")]
        [SerializeField] private Vector3 hipPosition = new Vector3(0.10f, -0.14f, 0.32f);

        [Tooltip("Rotação base da arma em graus.")]
        [SerializeField] private Vector3 hipRotation = Vector3.zero;

        [Header("Sockets de Mira Óptica (Iron Sights Alignment)")]
        [Tooltip("Ponto de referência (socket) na ponta da massa de mira (Front Sight).")]
        [SerializeField] private Transform frontSightSocket;

        [Tooltip("Ponto de referência (socket) no entalhe da alça de mira (Rear Sight).")]
        [SerializeField] private Transform rearSightSocket;

        [Tooltip("Distância confortável da alça de mira até a lente da câmera (Eye Relief em metros).")]
        [SerializeField] private float adsEyeRelief = 0.18f;

        [Tooltip("Alinhamento automático analítico que posiciona os sockets colineares com o centro do viewport.")]
        [SerializeField] private bool autoAlignSocketsInADS = true;

        [Header("Calibração Fina de Mira (ADS Tuning & Offset)")]
        [Tooltip("Offset adicional de posição para ajuste em tempo real (X, Y, Z em metros).")]
        public Vector3 adsPositionOffset = Vector3.zero;

        [Tooltip("Offset adicional de rotação Euler (Pitch no eixo X, Yaw no Y, Roll no Z em graus).")]
        public Vector3 adsRotationOffset = Vector3.zero;

        [Header("Painel Interativo de Calibração na Tela")]
        [Tooltip("Exibe janela com sliders na tela para ajuste visual e milimétrico da mira.")]
        public bool showDebugCalibrationUI = false;

        [Tooltip("Mantém a visada ADS travada (sem precisar segurar o botão direito) para calibrar.")]
        public bool forceLockADS = false;

        [Tooltip("Exibe um retículo guia no centro exato da tela (guia óptico de 1 pixel).")]
        public bool showScreenCenterGuide = true;

        public Vector3 AdsPositionOffset { get => adsPositionOffset; set => adsPositionOffset = value; }
        public Vector3 AdsRotationOffset { get => adsRotationOffset; set => adsRotationOffset = value; }

        private Vector3 _calibratedAdsPosition;
        private Vector3 _calibratedAdsRotation;
        private bool _hasCalibratedADS = false;

        [Tooltip("Campo de visão padrão da câmera no quadril.")]
        [SerializeField] private float hipFOV = 72f;

        [Header("Inércia do Mouse (Weapon Sway - Apenas no Hipfire)")]
        [SerializeField] private float swayAmount = 0.02f;
        [SerializeField] private float maxSwayAmount = 0.045f;
        [SerializeField] private float swaySmooth = 9.0f;
        [SerializeField] private float swayRotationAmount = 2.0f;

        [Header("Balanço de Caminhada (Weapon Bob - Apenas no Hipfire)")]
        [SerializeField] private float walkBobSpeed = 9.0f;
        [SerializeField] private float walkBobAmount = 0.016f;
        [SerializeField] private float sprintBobSpeed = 13.5f;
        [SerializeField] private float sprintBobAmount = 0.038f;

        [Header("Recuo da Câmera (Spray Pattern estilo CS2)")]
        [Tooltip("Rapidez com que a câmera sobe no disparo instantâneo.")]
        [SerializeField] private float cameraRecoilSnappiness = 24.0f;

        [Tooltip("Velocidade com que a câmera retorna à posição de mira inicial ao cessar os disparos.")]
        [SerializeField] private float cameraRecoilRecoverySpeed = 11.0f;

        [Tooltip("Multiplicador geral de recuo vertical e horizontal da câmera.")]
        [SerializeField] private float cameraRecoilMultiplier = 1.0f;

        [Tooltip("Tempo em segundos sem disparar para resetar o contador de spray da arma.")]
        [SerializeField] private float sprayResetTime = 0.30f;

        [Header("Recuo Físico da Arma (Viewmodel Kick)")]
        [SerializeField] private float weaponRecoilKickZ = 0.045f;
        [SerializeField] private float weaponRecoilKickY = 0.012f;
        [SerializeField] private float weaponRecoilPitch = 5.5f;
        [SerializeField] private float weaponRecoilSnappiness = 26.0f;
        [SerializeField] private float weaponRecoilReturnSpeed = 10.0f;

        // Estados internos
        private bool _isAiming;
        private float _adsProgress; // 0.0 = Hipfire, 1.0 = ADS Completo
        private float _bobTimer;
        private int _continuousShotCount = 0;
        private float _lastShotTime = 0f;

        // Vetores de Recuo
        private Vector2 _targetCameraRecoil;
        private Vector2 _currentCameraRecoil;
        private Vector3 _targetWeaponRecoilPos;
        private Vector3 _currentWeaponRecoilPos;
        private Vector3 _targetWeaponRecoilRot;
        private Vector3 _currentWeaponRecoilRot;

        // Vetores de Sway
        private Vector3 _currentSwayPos;
        private Quaternion _currentSwayRot = Quaternion.identity;

        public bool IsAiming => _isAiming;
        public WeaponData CurrentWeaponData
        {
            get => weaponData;
            set => weaponData = value;
        }

        private void OnEnable()
        {
            Weapon.OnWeaponFired += FireRecoil;
        }

        private void OnDisable()
        {
            Weapon.OnWeaponFired -= FireRecoil;
        }

        public void ConfigureStance(WeaponData data, Vector3 hipPos)
        {
            weaponData = data;
            hipPosition = hipPos;
            CalibrateADSFromSockets();
        }

        public void SetSightSockets(Transform frontSight, Transform rearSight, float eyeRelief = 0.18f)
        {
            frontSightSocket = frontSight;
            rearSightSocket = rearSight;
            adsEyeRelief = eyeRelief;
            CalibrateADSFromSockets();
        }

        /// <summary>
        /// Calcula matematicamente a posição e rotação local do WeaponHolder para que
        /// a massa de mira (Front Sight) e o entalhe da alça de mira (Rear Sight)
        /// fiquem colineares com o eixo Z frontal da câmera (X=0, Y=0 no centro do viewport).
        /// </summary>
        public void CalibrateADSFromSockets()
        {
            if (frontSightSocket == null || rearSightSocket == null || weaponTransform == null)
            {
                _calibratedAdsPosition = (weaponData != null) ? weaponData.AdsLocalPosition : new Vector3(0f, -0.024f, 0.18f);
                _calibratedAdsRotation = (weaponData != null) ? weaponData.AdsLocalRotation : new Vector3(-3.2f, 0f, 0f);
                _hasCalibratedADS = true;
                return;
            }

            // Calcula o vetor da alça de mira até a massa de mira no espaço local do weaponTransform
            Vector3 frontLocal = weaponTransform.InverseTransformPoint(frontSightSocket.position);
            Vector3 rearLocal = weaponTransform.InverseTransformPoint(rearSightSocket.position);
            Vector3 sightDirLocal = (frontLocal - rearLocal).normalized;

            if (sightDirLocal.sqrMagnitude < 0.0001f)
            {
                sightDirLocal = Vector3.forward;
            }

            // 1. Rotação necessária para apontar a reta de visada exatamente em Vector3.forward (câmera frontal)
            Quaternion alignRot = Quaternion.FromToRotation(sightDirLocal, Vector3.forward);

            // 2. Posição necessária para posicionar o entalhe da alça de mira (rearLocal) em (0, 0, adsEyeRelief):
            // CameraSpace: TargetPos + alignRot * rearLocal = (0, 0, adsEyeRelief)
            // TargetPos = (0, 0, adsEyeRelief) - (alignRot * rearLocal)
            Vector3 targetHolderPos = new Vector3(0f, 0f, adsEyeRelief) - (alignRot * rearLocal);
            Vector3 targetEuler = alignRot.eulerAngles;

            if (targetEuler.x > 180f) targetEuler.x -= 360f;
            if (targetEuler.y > 180f) targetEuler.y -= 360f;
            if (targetEuler.z > 180f) targetEuler.z -= 360f;

            _calibratedAdsPosition = targetHolderPos;
            _calibratedAdsRotation = targetEuler;
            _hasCalibratedADS = true;
        }

        private void Awake()
        {
            if (weaponTransform == null) weaponTransform = transform;
            if (playerCamera == null) playerCamera = GetComponentInParent<Camera>() ?? Camera.main;
            if (characterController == null) characterController = GetComponentInParent<CharacterController>();

            if (playerCamera != null)
            {
                hipFOV = playerCamera.fieldOfView;
            }
        }

        private void Update()
        {
            HandleADSInput();
            HandleSwayAndBobbing();
            HandleWeaponAndCameraRecoil();
            HandleLiveTuning();
            ApplyProceduralTransform();
        }

        public void ToggleDebugCalibrationUI(bool open)
        {
            showDebugCalibrationUI = open;
            if (showDebugCalibrationUI)
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

        private void HandleLiveTuning()
        {
            // F1: Alterna painel visual de sliders na tela
            if (Input.GetKeyDown(KeyCode.F1))
            {
                ToggleDebugCalibrationUI(!showDebugCalibrationUI);
            }

            // F2: Trava/destrava visada ADS para calibrar sem segurar o mouse
            if (Input.GetKeyDown(KeyCode.F2))
            {
                forceLockADS = !forceLockADS;
                Debug.Log($"<color=#00FFAA>[ADS Lock]</color> {(forceLockADS ? "ATIVADO" : "DESATIVADO")}");
            }

            if (!_isAiming && !forceLockADS) return;

            // Micro-calibração via teclado enquanto mira (Shift acelera o passo):
            float moveStep = (Input.GetKey(KeyCode.LeftShift) ? 0.08f : 0.02f) * Time.deltaTime;
            float rotStep = (Input.GetKey(KeyCode.LeftShift) ? 20f : 6f) * Time.deltaTime;

            if (Input.GetKey(KeyCode.UpArrow)) adsPositionOffset.y += moveStep;
            if (Input.GetKey(KeyCode.DownArrow)) adsPositionOffset.y -= moveStep;
            if (Input.GetKey(KeyCode.LeftArrow)) adsPositionOffset.x -= moveStep;
            if (Input.GetKey(KeyCode.RightArrow)) adsPositionOffset.x += moveStep;
            if (Input.GetKey(KeyCode.RightBracket)) adsPositionOffset.z += moveStep;
            if (Input.GetKey(KeyCode.LeftBracket)) adsPositionOffset.z -= moveStep;

            if (Input.GetKey(KeyCode.PageUp)) adsRotationOffset.x -= rotStep;
            if (Input.GetKey(KeyCode.PageDown)) adsRotationOffset.x += rotStep;
            if (Input.GetKey(KeyCode.Home)) adsRotationOffset.y -= rotStep;
            if (Input.GetKey(KeyCode.End)) adsRotationOffset.y += rotStep;
            if (Input.GetKey(KeyCode.Insert)) adsRotationOffset.z -= rotStep;
            if (Input.GetKey(KeyCode.Delete)) adsRotationOffset.z += rotStep;

            if (Input.GetKeyDown(KeyCode.P))
            {
                Vector3 baseAdsPos = _hasCalibratedADS ? _calibratedAdsPosition : ((weaponData != null) ? weaponData.AdsLocalPosition : new Vector3(0f, -0.024f, 0.18f));
                Vector3 baseAdsRot = _hasCalibratedADS ? _calibratedAdsRotation : ((weaponData != null) ? weaponData.AdsLocalRotation : new Vector3(-3.2f, 0f, 0f));
                Vector3 currentAdsPos = baseAdsPos + adsPositionOffset;
                Vector3 currentAdsRot = baseAdsRot + adsRotationOffset;
                string log = $"adsPos = new Vector3({currentAdsPos.x:F4}f, {currentAdsPos.y:F4}f, {currentAdsPos.z:F4}f);\nadsRot = new Vector3({currentAdsRot.x:F2}f, {currentAdsRot.y:F2}f, {currentAdsRot.z:F2}f);";
                GUIUtility.systemCopyBuffer = log;
                Debug.Log($"<color=#00FFAA>[Calibração ADS Copiada para Clipboard!]</color>\n{log}");
            }
        }

        // ========================================================================================
        // 1. ALINHAMENTO PERFEITO DE MIRA (ADS) & SUPRESSÃO DE BOB/SWAY
        // ========================================================================================
        private void HandleADSInput()
        {
            // Ativa visada com botão direito do mouse ou pelo toggle de calibração
            _isAiming = Input.GetMouseButton(1) || forceLockADS;

            float targetProgress = _isAiming ? 1.0f : 0.0f;
            float transitionSpeed = (weaponData != null) ? weaponData.AdsSpeed : 14.0f;

            _adsProgress = Mathf.MoveTowards(_adsProgress, targetProgress, Time.deltaTime * transitionSpeed);

            // Zoom suave de FOV da câmera para criar foco visual no alvo
            if (playerCamera != null)
            {
                float targetFOV = _isAiming 
                    ? ((weaponData != null) ? weaponData.AdsCameraFOV : 48.0f) 
                    : hipFOV;

                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * transitionSpeed);
            }
        }

        // ========================================================================================
        // 2. INÉRCIA (SWAY) E BALANÇO (BOB) — REDUZIDOS A ZERO NO ADS
        // ========================================================================================
        private void HandleSwayAndBobbing()
        {
            // Fator crítico: No ADS (_adsProgress == 1), stabilityMultiplier é exatamente ZERO (0)!
            // A mira fica 100% cravada e estática no centro da tela.
            float stabilityMultiplier = Mathf.Clamp01(1.0f - _adsProgress);

            // A) Weapon Sway (Inércia da visão do mouse)
            float mouseX = Input.GetAxisRaw("Mouse X");
            float mouseY = Input.GetAxisRaw("Mouse Y");

            float moveX = Mathf.Clamp(-mouseX * swayAmount, -maxSwayAmount, maxSwayAmount) * stabilityMultiplier;
            float moveY = Mathf.Clamp(-mouseY * swayAmount, -maxSwayAmount, maxSwayAmount) * stabilityMultiplier;
            Vector3 targetSwayPos = new Vector3(moveX, moveY, 0f);

            float rotX = Mathf.Clamp(mouseY * swayRotationAmount, -6f, 6f) * stabilityMultiplier;
            float rotY = Mathf.Clamp(-mouseX * swayRotationAmount, -6f, 6f) * stabilityMultiplier;
            Quaternion targetSwayRot = Quaternion.Euler(rotX, rotY, rotY * 0.5f);

            _currentSwayPos = Vector3.Lerp(_currentSwayPos, targetSwayPos, Time.deltaTime * swaySmooth);
            _currentSwayRot = Quaternion.Slerp(_currentSwayRot, targetSwayRot, Time.deltaTime * swaySmooth);

            // B) Weapon Bobbing (Passos do operador)
            float speed = (characterController != null) ? characterController.velocity.magnitude : 0f;

            if (speed > 0.1f && stabilityMultiplier > 0.01f)
            {
                bool isSprinting = speed > 4.5f;
                float bobFreq = isSprinting ? sprintBobSpeed : walkBobSpeed;
                float bobAmp = (isSprinting ? sprintBobAmount : walkBobAmount) * stabilityMultiplier;

                _bobTimer += Time.deltaTime * bobFreq;

                // Curva de Lissajous em "8" para passada orgânica natural
                float bobX = Mathf.Cos(_bobTimer * 0.5f) * bobAmp;
                float bobY = Mathf.Sin(_bobTimer) * (bobAmp * 0.7f);

                _currentSwayPos += new Vector3(bobX, bobY, 0f);
            }
            else
            {
                _bobTimer = 0f;
            }
        }

        // ========================================================================================
        // 3. RECUO PROCEDURAL E SPRAY PATTERN COMPENSÁVEL (ESTILO CS2)
        // ========================================================================================
        /// <summary>
        /// Acionado externamente a cada tiro disparado pela arma.
        /// Aplica o padrão de recuo da arma na câmera e na mecânica do viewmodel.
        /// </summary>
        public void FireRecoil()
        {
            // Reseta a sequência de spray caso o tempo entre disparos exceda o limiar
            if (Time.time - _lastShotTime > sprayResetTime)
            {
                _continuousShotCount = 0;
            }

            _lastShotTime = Time.time;

            // 1. Consulta as curvas de spray do WeaponData
            float normalizedShot = Mathf.Clamp01(_continuousShotCount / 18f);

            float verticalKick = (weaponData != null && weaponData.VerticalRecoilCurve != null)
                ? weaponData.VerticalRecoilCurve.Evaluate(normalizedShot)
                : Random.Range(1.2f, 2.0f);

            float horizontalKick = (weaponData != null && weaponData.HorizontalRecoilCurve != null)
                ? weaponData.HorizontalRecoilCurve.Evaluate(normalizedShot)
                : Random.Range(-0.35f, 0.35f);

            _continuousShotCount++;

            // 2. Aplica coice angular na câmera (X negativo eleva a mira para cima)
            _targetCameraRecoil.x -= verticalKick * cameraRecoilMultiplier;
            _targetCameraRecoil.y += horizontalKick * cameraRecoilMultiplier;

            // 3. Aplica coice mecânico no modelo da arma (para trás no ombro e cano para cima)
            _targetWeaponRecoilPos += new Vector3(0f, weaponRecoilKickY, -weaponRecoilKickZ);
            _targetWeaponRecoilRot += new Vector3(-weaponRecoilPitch, horizontalKick * 3f, 0f);
        }

        private void HandleWeaponAndCameraRecoil()
        {
            // Mecânica CS2 de Compensação de Spray:
            // Se o jogador estiver puxando o mouse para baixo (mouseY > 0), deduz proporcionalmente do recuo acumulado
            float mouseY = Input.GetAxisRaw("Mouse Y");
            if (mouseY > 0f && _targetCameraRecoil.x < 0f)
            {
                _targetCameraRecoil.x = Mathf.Min(0f, _targetCameraRecoil.x + (mouseY * 0.45f));
            }

            // A) Interpolação e recuperação da Câmera
            _currentCameraRecoil = Vector2.Lerp(_currentCameraRecoil, _targetCameraRecoil, Time.deltaTime * cameraRecoilSnappiness);
            _targetCameraRecoil = Vector2.Lerp(_targetCameraRecoil, Vector2.zero, Time.deltaTime * cameraRecoilRecoverySpeed);

            if (playerCamera != null)
            {
                // Aplica a rotação do recuo de forma aditiva na câmera
                playerCamera.transform.localRotation = Quaternion.Euler(_currentCameraRecoil.x, _currentCameraRecoil.y, 0f);
            }

            // B) Interpolação e recuperação do Viewmodel da Arma
            _currentWeaponRecoilPos = Vector3.Lerp(_currentWeaponRecoilPos, _targetWeaponRecoilPos, Time.deltaTime * weaponRecoilSnappiness);
            _targetWeaponRecoilPos = Vector3.Lerp(_targetWeaponRecoilPos, Vector3.zero, Time.deltaTime * weaponRecoilReturnSpeed);

            _currentWeaponRecoilRot = Vector3.Lerp(_currentWeaponRecoilRot, _targetWeaponRecoilRot, Time.deltaTime * weaponRecoilSnappiness);
            _targetWeaponRecoilRot = Vector3.Lerp(_targetWeaponRecoilRot, Vector3.zero, Time.deltaTime * weaponRecoilReturnSpeed);
        }

        // ========================================================================================
        // 4. APLICAÇÃO FINAL DA TRANSFORMAÇÃO DA ARMA
        // ========================================================================================
        private void ApplyProceduralTransform()
        {
            if (weaponTransform == null) return;

            // Se calibrado pelos sockets de massa/alça, utiliza o alinhamento analítico; senão, utiliza o WeaponData
            if (autoAlignSocketsInADS && !_hasCalibratedADS)
            {
                CalibrateADSFromSockets();
            }

            Vector3 baseAdsPos = _hasCalibratedADS ? _calibratedAdsPosition : ((weaponData != null) ? weaponData.AdsLocalPosition : new Vector3(0f, -0.024f, 0.18f));
            Vector3 baseAdsRot = _hasCalibratedADS ? _calibratedAdsRotation : ((weaponData != null) ? weaponData.AdsLocalRotation : new Vector3(-3.2f, 0f, 0f));

            Vector3 adsPos = baseAdsPos + adsPositionOffset;
            Vector3 adsRot = baseAdsRot + adsRotationOffset;

            // Transição linear suave entre Hipfire e ADS
            Vector3 basePos = Vector3.Lerp(hipPosition, adsPos, _adsProgress);
            Vector3 baseRot = Vector3.Lerp(hipRotation, adsRot, _adsProgress);

            // Combina com o Sway/Bob e o Recuo físico
            weaponTransform.localPosition = basePos + _currentSwayPos + _currentWeaponRecoilPos;
            weaponTransform.localRotation = Quaternion.Euler(baseRot + _currentWeaponRecoilRot) * _currentSwayRot;
        }

        private void OnDrawGizmos()
        {
            if (frontSightSocket != null && rearSightSocket != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(frontSightSocket.position, 0.003f);
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(rearSightSocket.position, 0.003f);
                Gizmos.color = Color.green;
                Vector3 dir = (frontSightSocket.position - rearSightSocket.position).normalized;
                Gizmos.DrawLine(rearSightSocket.position, rearSightSocket.position + dir * 10f);
            }
        }

        // ========================================================================================
        // 5. PAINEL INTERATIVO DE CALIBRAÇÃO VISUAL (SLIDERS EM TEMPO REAL NA TELA)
        // ========================================================================================
        private Rect _debugWindowRect = new Rect(20, 50, 440, 490);
        private Texture2D _guiBgTexture;
        private Texture2D _crossTexture;
        private GUIStyle _windowStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _toggleStyle;
        private bool _guiStylesInitialized = false;

        private void InitGUIStyles()
        {
            if (_guiStylesInitialized) return;

            _guiBgTexture = new Texture2D(1, 1);
            _guiBgTexture.SetPixel(0, 0, new Color(0.06f, 0.08f, 0.12f, 0.94f));
            _guiBgTexture.Apply();

            _windowStyle = new GUIStyle(GUI.skin.window);
            _windowStyle.normal.background = _guiBgTexture;
            _windowStyle.onNormal.background = _guiBgTexture;
            _windowStyle.normal.textColor = Color.white;
            _windowStyle.fontSize = 12;
            _windowStyle.fontStyle = FontStyle.Bold;

            _headerStyle = new GUIStyle(GUI.skin.label);
            _headerStyle.fontSize = 11;
            _headerStyle.fontStyle = FontStyle.Bold;
            _headerStyle.normal.textColor = new Color(0.35f, 0.85f, 1f);

            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.fontSize = 11;
            _labelStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);

            _valueStyle = new GUIStyle(GUI.skin.label);
            _valueStyle.fontSize = 11;
            _valueStyle.fontStyle = FontStyle.Bold;
            _valueStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);
            _valueStyle.alignment = TextAnchor.MiddleRight;

            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.fontSize = 11;
            _buttonStyle.fontStyle = FontStyle.Bold;

            _toggleStyle = new GUIStyle(GUI.skin.toggle);
            _toggleStyle.fontSize = 11;
            _toggleStyle.normal.textColor = Color.white;

            _guiStylesInitialized = true;
        }

        private void OnGUI()
        {
            // Guia óptico de centro de tela (ponto/cruz de 1 pixel central)
            if (showScreenCenterGuide && (_isAiming || forceLockADS || showDebugCalibrationUI))
            {
                DrawScreenCenterCross();
            }

            if (!showDebugCalibrationUI)
            {
                if (GUI.Button(new Rect(10, 10, 185, 26), "🔧 CALIBRAR MIRA [F1]"))
                {
                    ToggleDebugCalibrationUI(true);
                }
                return;
            }

            InitGUIStyles();
            _debugWindowRect = GUI.Window(999, _debugWindowRect, DrawCalibrationWindow, "CALIBRAÇÃO DE MIRA ADS (CS2 SIGHT TUNER)", _windowStyle);
        }

        private void DrawCalibrationWindow(int windowID)
        {
            string wName = (weaponData != null) ? weaponData.WeaponName : "Arma Atual";
            GUILayout.Label($"Arma Ativa: <b><color=#FFD700>{wName}</color></b>", _headerStyle);

            GUILayout.BeginHorizontal();
            forceLockADS = GUILayout.Toggle(forceLockADS, " Travar Mira ADS [F2]", _toggleStyle);
            showScreenCenterGuide = GUILayout.Toggle(showScreenCenterGuide, " Retículo Central", _toggleStyle);
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("── POSIÇÃO OFFSET (METROS / MILÍMETROS) ──", _headerStyle);
            adsPositionOffset.x = DrawSliderRow("Pos X (Lateral):", adsPositionOffset.x, -0.06f, 0.06f, 0.0005f, "m");
            adsPositionOffset.y = DrawSliderRow("Pos Y (Altura):", adsPositionOffset.y, -0.06f, 0.06f, 0.0005f, "m");
            adsPositionOffset.z = DrawSliderRow("Pos Z (Distância):", adsPositionOffset.z, -0.12f, 0.12f, 0.0010f, "m");

            GUILayout.Space(6);
            GUILayout.Label("── ROTAÇÃO OFFSET (GRAUS EULER) ──", _headerStyle);
            adsRotationOffset.x = DrawSliderRow("Rot X (Pitch):", adsRotationOffset.x, -15f, 15f, 0.10f, "°");
            adsRotationOffset.y = DrawSliderRow("Rot Y (Yaw):", adsRotationOffset.y, -10f, 10f, 0.10f, "°");
            adsRotationOffset.z = DrawSliderRow("Rot Z (Roll):", adsRotationOffset.z, -10f, 10f, 0.10f, "°");

            GUILayout.Space(6);
            Vector3 baseAdsPos = _hasCalibratedADS ? _calibratedAdsPosition : ((weaponData != null) ? weaponData.AdsLocalPosition : new Vector3(0f, -0.024f, 0.18f));
            Vector3 baseAdsRot = _hasCalibratedADS ? _calibratedAdsRotation : ((weaponData != null) ? weaponData.AdsLocalRotation : new Vector3(-3.2f, 0f, 0f));
            Vector3 finalPos = baseAdsPos + adsPositionOffset;
            Vector3 finalRot = baseAdsRot + adsRotationOffset;

            GUILayout.Label($"<b>Posição Final:</b> X={finalPos.x:F4} | Y={finalPos.y:F4} | Z={finalPos.z:F4}", _labelStyle);
            GUILayout.Label($"<b>Rotação Final:</b> Pitch={finalRot.x:F2}° | Yaw={finalRot.y:F2}° | Roll={finalRot.z:F2}°", _labelStyle);

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("📋 COPIAR VALORES C#", _buttonStyle, GUILayout.Height(26)))
            {
                string code = $"adsPos = new Vector3({finalPos.x:F4}f, {finalPos.y:F4}f, {finalPos.z:F4}f);\nadsRot = new Vector3({finalRot.x:F2}f, {finalRot.y:F2}f, {finalRot.z:F2}f);";
                GUIUtility.systemCopyBuffer = code;
                Debug.Log($"<color=#00FFAA>[Calibração ADS Copiada para Clipboard!]</color>\n{code}");
            }

            if (GUILayout.Button("🔄 RESETAR", _buttonStyle, GUILayout.Height(26), GUILayout.Width(85)))
            {
                adsPositionOffset = Vector3.zero;
                adsRotationOffset = Vector3.zero;
            }

            if (GUILayout.Button("✕ FECHAR (F1)", _buttonStyle, GUILayout.Height(26), GUILayout.Width(100)))
            {
                ToggleDebugCalibrationUI(false);
            }
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0, 0, 10000, 24));
        }

        private float DrawSliderRow(string label, float value, float min, float max, float step, string unit)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _labelStyle, GUILayout.Width(110));
            if (GUILayout.Button("-", GUILayout.Width(22), GUILayout.Height(18)))
            {
                value = Mathf.Round((value - step) * 10000f) / 10000f;
            }
            value = GUILayout.HorizontalSlider(value, min, max, GUILayout.MinWidth(140));
            if (GUILayout.Button("+", GUILayout.Width(22), GUILayout.Height(18)))
            {
                value = Mathf.Round((value + step) * 10000f) / 10000f;
            }
            GUILayout.Label($"{value:F4}{unit}", _valueStyle, GUILayout.Width(75));
            GUILayout.EndHorizontal();
            return value;
        }

        private void DrawScreenCenterCross()
        {
            if (_crossTexture == null)
            {
                _crossTexture = new Texture2D(1, 1);
                _crossTexture.SetPixel(0, 0, new Color(0.2f, 1f, 0.4f, 0.85f));
                _crossTexture.Apply();
            }

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            float len = 8f;
            float gap = 2f;

            // Linhas horizontais (esquerda e direita)
            GUI.DrawTexture(new Rect(cx - len - gap, cy - 0.5f, len, 1f), _crossTexture);
            GUI.DrawTexture(new Rect(cx + gap, cy - 0.5f, len, 1f), _crossTexture);

            // Linhas verticais (cima e baixo)
            GUI.DrawTexture(new Rect(cx - 0.5f, cy - len - gap, 1f, len), _crossTexture);
            GUI.DrawTexture(new Rect(cx - 0.5f, cy + gap, 1f, len), _crossTexture);

            // Ponto central de 1 pixel
            GUI.DrawTexture(new Rect(cx - 0.5f, cy - 0.5f, 1f, 1f), _crossTexture);
        }

        private void OnDestroy()
        {
            if (_guiBgTexture != null) Destroy(_guiBgTexture);
            if (_crossTexture != null) Destroy(_crossTexture);
        }
    }
}
