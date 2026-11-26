using UnityEngine;

namespace IPSCSimulator.Weapons
{
    /// <summary>
    /// Script independente e completo para movimentação procedural de armas em primeira pessoa (FPS Viewmodel).
    /// Inclui:
    /// 1. Weapon Sway & Inércia (o atraso suave do modelo ao movimentar a câmera com o mouse)
    /// 2. Strafe Tilting (inclinação lateral ao se mover para a esquerda/direita com A/D)
    /// 3. Procedural Walk & Sprint Bobbing (balanço rítmico em curva de Lissajous baseado na passada)
    /// 4. Respiração suave no modo Idle (Idle Breathing)
    /// 5. Recuo procedural realista por física de mola (Spring-Damper Recoil em Z, Y, Pitch e Roll)
    /// 6. Mira precisa (ADS - Aim Down Sights) via botão direito do mouse
    /// </summary>
    [DisallowMultipleComponent]
    public class ProceduralWeaponViewmodel : MonoBehaviour
    {
        [Header("--- Posicionamento Base (Foco Visual da Alça e Massa de Mira) ---")]
        [Tooltip("Posição de repouso da arma no quadril (Hipfire)")]
        public Vector3 hipPosition = new Vector3(0.085f, -0.125f, 0.28f);
        [Tooltip("Posição da alça e massa de mira alinhadas nos olhos do atirador (ADS)")]
        public Vector3 adsPosition = new Vector3(0f, -0.046f, 0.16f);
        [Tooltip("Velocidade de transição para a mira ADS")]
        public float adsTransitionSpeed = 16f;

        [Header("--- Camera FOV Zoom (Foco Visual) ---")]
        [Tooltip("Campo de visão normal da câmera")]
        public float hipFOV = 70f;
        [Tooltip("Campo de visão aproximado ao mirar (zoom de foco)")]
        public float adsFOV = 48f;
        [Tooltip("Velocidade do zoom da câmera")]
        public float fovZoomSpeed = 14f;

        [Header("--- Mouse Sway (Inércia da Câmera) ---")]
        [Tooltip("Intensidade do deslocamento posicional ao mover o mouse")]
        public float swayAmount = 0.02f;
        [Tooltip("Limite máximo de deslocamento posicional")]
        public float maxSwayAmount = 0.05f;
        [Tooltip("Intensidade da rotação angular ao mover o mouse")]
        public float swayRotationAmount = 2.5f;
        [Tooltip("Limite máximo de rotação angular")]
        public float maxSwayRotation = 6f;
        [Tooltip("Suavidade da interpolação do sway")]
        public float swaySmoothness = 8f;

        [Header("--- Strafe Tilting (Inclinação ao Andar de Lado) ---")]
        [Tooltip("Ângulo de inclinação lateral ao pressionar A ou D")]
        public float strafeTiltAngle = 3.5f;
        [Tooltip("Velocidade da transição de inclinação")]
        public float strafeTiltSpeed = 6f;

        [Header("--- Bobbing (Balanço de Passadas) ---")]
        [Tooltip("Frequência da respiração quando parado")]
        public float idleBobSpeed = 1.5f;
        [Tooltip("Amplitude da respiração quando parado")]
        public float idleBobAmount = 0.003f;
        [Tooltip("Frequência das passadas ao caminhar")]
        public float walkBobSpeed = 8.5f;
        [Tooltip("Amplitude das passadas ao caminhar")]
        public float walkBobAmount = 0.015f;
        [Tooltip("Frequência ao correr")]
        public float sprintBobSpeed = 13.5f;
        [Tooltip("Amplitude ao correr")]
        public float sprintBobAmount = 0.035f;

        [Header("--- Procedural Recoil (Física de Coice) ---")]
        [Tooltip("Empurrão da arma para trás em direção ao peito")]
        public float recoilKickZ = 0.06f;
        [Tooltip("Empurrão da arma para cima")]
        public float recoilKickY = 0.015f;
        [Tooltip("Elevação do cano da arma (Pitch em graus)")]
        public float recoilPitch = 7f;
        [Tooltip("Dispersão horizontal aleatória do coice (Yaw)")]
        public float recoilYaw = 1.5f;
        [Tooltip("Torção lateral aleatória da arma ao disparar (Roll)")]
        public float recoilRoll = 1.2f;
        [Tooltip("Velocidade de explosão inicial do coice (Snappiness)")]
        public float recoilSnappiness = 22f;
        [Tooltip("Velocidade de recuperação da mola para a posição original")]
        public float recoilReturnSpeed = 9f;

        [Header("--- Teste Automático ---")]
        [Tooltip("Se ativado, dispara o recuo ao clicar com o Botão Esquerdo do Mouse")]
        public bool testFireWithLeftClick = true;

        // Estados internos
        private Vector3 _currentRecoilPos;
        private Vector3 _targetRecoilPos;
        private Vector3 _currentRecoilRot;
        private Vector3 _targetRecoilRot;

        private Vector3 _swayPos;
        private Quaternion _swayRot;
        private float _currentTiltZ;

        private float _bobCycle;
        private bool _isAiming;
        private CharacterController _controller;
        private Camera _playerCamera;

        public bool IsAiming => _isAiming;

        private void Awake()
        {
            _controller = GetComponentInParent<CharacterController>();
            _playerCamera = GetComponentInParent<Camera>();
            if (_playerCamera == null) _playerCamera = Camera.main;
            if (_playerCamera != null) hipFOV = _playerCamera.fieldOfView;
        }

        private void Update()
        {
            HandleAimInput();
            HandleFOVZoom();
            HandleSway();
            HandleStrafeTilt();
            HandleBobbing();
            HandleRecoilPhysics();
            ApplyCompositeTransform();

            if (testFireWithLeftClick && Input.GetMouseButtonDown(0))
            {
                FireRecoil();
            }
        }

        private void HandleFOVZoom()
        {
            if (_playerCamera != null)
            {
                float targetFOV = _isAiming ? adsFOV : hipFOV;
                _playerCamera.fieldOfView = Mathf.Lerp(_playerCamera.fieldOfView, targetFOV, Time.deltaTime * fovZoomSpeed);
            }
        }

        private void HandleAimInput()
        {
            _isAiming = Input.GetMouseButton(1);
        }

        /// <summary>
        /// Calcula a inércia realista da arma contra o movimento da câmera.
        /// </summary>
        private void HandleSway()
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            float aimDamping = _isAiming ? 0.25f : 1.0f;

            // Deslocamento posicional
            float moveX = Mathf.Clamp(-mouseX * swayAmount * aimDamping, -maxSwayAmount, maxSwayAmount);
            float moveY = Mathf.Clamp(-mouseY * swayAmount * aimDamping, -maxSwayAmount, maxSwayAmount);
            Vector3 targetSway = new Vector3(moveX, moveY, 0f);

            _swayPos = Vector3.Lerp(_swayPos, targetSway, Time.deltaTime * swaySmoothness);

            // Rotação angular (Pitch, Yaw e Roll sutil)
            float rotPitch = Mathf.Clamp(-mouseY * swayRotationAmount * aimDamping, -maxSwayRotation, maxSwayRotation);
            float rotYaw = Mathf.Clamp(mouseX * swayRotationAmount * aimDamping, -maxSwayRotation, maxSwayRotation);
            float rotRoll = Mathf.Clamp(-mouseX * (swayRotationAmount * 0.6f) * aimDamping, -maxSwayRotation, maxSwayRotation);

            Quaternion targetRot = Quaternion.Euler(rotPitch, rotYaw, rotRoll);
            _swayRot = Quaternion.Slerp(_swayRot, targetRot, Time.deltaTime * swaySmoothness);
        }

        /// <summary>
        /// Inclina a arma sutilmente na diagonal ao se mover lateralmente (Strafe).
        /// </summary>
        private void HandleStrafeTilt()
        {
            float strafeInput = Input.GetAxisRaw("Horizontal"); // A (-1) e D (+1)
            float targetTilt = -strafeInput * strafeTiltAngle * (_isAiming ? 0.3f : 1.0f);
            _currentTiltZ = Mathf.Lerp(_currentTiltZ, targetTilt, Time.deltaTime * strafeTiltSpeed);
        }

        /// <summary>
        /// Calcula a oscilação de passos em curva de Lissajous ("formato de 8").
        /// </summary>
        private void HandleBobbing()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            bool isMoving = (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f);
            if (_controller != null)
            {
                isMoving = _controller.velocity.magnitude > 0.3f;
            }

            bool isSprinting = isMoving && Input.GetKey(KeyCode.LeftShift) && !_isAiming;
            float aimMultiplier = _isAiming ? 0.15f : 1.0f;

            float speed = isMoving ? (isSprinting ? sprintBobSpeed : walkBobSpeed) : idleBobSpeed;
            float amount = isMoving ? (isSprinting ? sprintBobAmount : walkBobAmount) : idleBobAmount;
            amount *= aimMultiplier;

            _bobCycle += Time.deltaTime * speed;

            float bobX = Mathf.Sin(_bobCycle) * amount;
            float bobY = Mathf.Abs(Mathf.Cos(_bobCycle * 2f)) * amount * 1.2f;

            _swayPos += new Vector3(bobX, bobY, 0f);
        }

        /// <summary>
        /// Atualiza o recuo amortecido por mola elástica (Spring-Damper).
        /// </summary>
        private void HandleRecoilPhysics()
        {
            _currentRecoilPos = Vector3.Lerp(_currentRecoilPos, _targetRecoilPos, Time.deltaTime * recoilSnappiness);
            _currentRecoilRot = Vector3.Lerp(_currentRecoilRot, _targetRecoilRot, Time.deltaTime * recoilSnappiness);

            _targetRecoilPos = Vector3.Lerp(_targetRecoilPos, Vector3.zero, Time.deltaTime * recoilReturnSpeed);
            _targetRecoilRot = Vector3.Lerp(_targetRecoilRot, Vector3.zero, Time.deltaTime * recoilReturnSpeed);
        }

        /// <summary>
        /// Dispara o impulso físico do recuo. Pode ser chamado pelo script de atirar da arma.
        /// </summary>
        public void FireRecoil()
        {
            float aimDamping = _isAiming ? 0.45f : 1.0f;

            float kickZ = recoilKickZ * aimDamping;
            float kickY = recoilKickY * aimDamping;
            float pitch = recoilPitch * aimDamping;
            float randomYaw = Random.Range(-recoilYaw, recoilYaw) * aimDamping;
            float randomRoll = Random.Range(-recoilRoll, recoilRoll) * aimDamping;

            // Empurrão para trás em Z e leve elevação em Y
            _targetRecoilPos += new Vector3(Random.Range(-0.003f, 0.003f), kickY, -kickZ);

            // Cano sobe (pitch negativo) com leve dispersão lateral
            _targetRecoilRot += new Vector3(-pitch, randomYaw, randomRoll);
        }

        /// <summary>
        /// Combina postura base, sway, bobbing, tilt e recoil na transformação local.
        /// </summary>
        private void ApplyCompositeTransform()
        {
            Vector3 basePosition = _isAiming ? adsPosition : hipPosition;
            float aimDamping = _isAiming ? 0.08f : 1.0f;

            Vector3 finalPosition = basePosition + (_swayPos * aimDamping) + _currentRecoilPos;
            Quaternion finalRotation = Quaternion.Slerp(Quaternion.identity, _swayRot, aimDamping) * Quaternion.Euler(_currentRecoilRot) * Quaternion.Euler(0f, 0f, _currentTiltZ * aimDamping);

            transform.localPosition = Vector3.Lerp(transform.localPosition, finalPosition, Time.deltaTime * adsTransitionSpeed);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, finalRotation, Time.deltaTime * adsTransitionSpeed);
        }
    }
}
