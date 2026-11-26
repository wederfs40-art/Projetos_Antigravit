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
        [SerializeField] private Vector3 hipPosition = new Vector3(0.085f, -0.125f, 0.28f);

        [Tooltip("Rotação base da arma em graus.")]
        [SerializeField] private Vector3 hipRotation = Vector3.zero;

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
            ApplyProceduralTransform();
        }

        // ========================================================================================
        // 1. ALINHAMENTO PERFEITO DE MIRA (ADS) & SUPRESSÃO DE BOB/SWAY
        // ========================================================================================
        private void HandleADSInput()
        {
            // Ativa visada com botão direito do mouse
            _isAiming = Input.GetMouseButton(1);

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

            // Posições base do WeaponData
            Vector3 adsPos = (weaponData != null) ? weaponData.AdsLocalPosition : new Vector3(0f, -0.046f, 0.16f);
            Vector3 adsRot = (weaponData != null) ? weaponData.AdsLocalRotation : Vector3.zero;

            // Transição linear suave entre Hipfire e ADS
            Vector3 basePos = Vector3.Lerp(hipPosition, adsPos, _adsProgress);
            Vector3 baseRot = Vector3.Lerp(hipRotation, adsRot, _adsProgress);

            // Combina com o Sway/Bob e o Recuo físico
            weaponTransform.localPosition = basePos + _currentSwayPos + _currentWeaponRecoilPos;
            weaponTransform.localRotation = Quaternion.Euler(baseRot + _currentWeaponRecoilRot) * _currentSwayRot;
        }
    }
}
