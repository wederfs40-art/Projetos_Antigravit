using UnityEngine;

namespace IPSCSimulator.Weapons
{
    using IPSCSimulator.Player;

    /// <summary>
    /// Gerencia a física secundária procedural do viewmodel em 1ª pessoa:
    /// 1. Weapon Sway (atraso e inércia do mouse em posição e rotação)
    /// 2. Walk & Sprint Bobbing (balanço natural de passos em curvas de Lissajous)
    /// 3. Procedural Recoil (coice translacional e angular com amortecimento por mola)
    /// 4. Transições de estados (Idle, Caminhada, Corrida Tática e Mira ADS)
    /// </summary>
    public class ProceduralViewmodelMotion : MonoBehaviour
    {
        [Header("Weapon Sway (Inércia da Câmera)")]
        [SerializeField] private float swayAmount = 0.022f;
        [SerializeField] private float maxSwayAmount = 0.06f;
        [SerializeField] private float swaySmooth = 8.5f;
        [SerializeField] private float swayRotationAmount = 2.4f;
        [SerializeField] private float maxSwayRotation = 6.0f;

        [Header("Bobbing (Balanço de Passada e Respiração)")]
        [SerializeField] private float idleBreathSpeed = 1.4f;
        [SerializeField] private float idleBreathAmount = 0.0035f;
        [SerializeField] private float walkBobSpeed = 8.5f;
        [SerializeField] private float walkBobAmount = 0.018f;
        [SerializeField] private float sprintBobSpeed = 13.5f;
        [SerializeField] private float sprintBobAmount = 0.042f;

        [Header("Procedural Recoil (Física de Coice)")]
        [SerializeField] private float recoilKickZ = 0.055f;
        [SerializeField] private float recoilKickY = 0.015f;
        [SerializeField] private float recoilPitch = 7.5f;
        [SerializeField] private float recoilYawSpread = 1.2f;
        [SerializeField] private float recoilSnappiness = 24.0f;
        [SerializeField] private float recoilReturnSpeed = 9.0f;

        [Header("Posicionamento Base")]
        [SerializeField] private Vector3 hipPosition = new Vector3(0.18f, -0.20f, 0.40f);
        [SerializeField] private Vector3 adsPosition = new Vector3(0f, -0.040f, 0.30f);
        [SerializeField] private Vector3 sprintPosition = new Vector3(0.12f, -0.15f, 0.32f);
        [SerializeField] private Vector3 sprintRotation = new Vector3(-25f, 32f, -18f); // High ready tático

        private Vector3 _currentRecoilPos;
        private Vector3 _targetRecoilPos;
        private Vector3 _currentRecoilRot;
        private Vector3 _targetRecoilRot;

        private Vector3 _swayPosition;
        private Quaternion _swayRotation;

        private float _bobTimer;
        private CharacterController _characterController;
        private bool _isAiming;
        private bool _isSprinting;
        private bool _isInspecting;

        public bool IsAiming => _isAiming;
        public bool IsSprinting => _isSprinting;
        public bool IsInspecting => _isInspecting;

        public void SetInspecting(bool inspecting) => _isInspecting = inspecting;

        private void Start()
        {
            _characterController = GetComponentInParent<CharacterController>();
            Weapon.OnWeaponFired += ApplyFireRecoil;
        }

        private void OnDestroy()
        {
            Weapon.OnWeaponFired -= ApplyFireRecoil;
        }

        private void Update()
        {
            HandleInputs();
            CalculateSway();
            CalculateBobbing();
            CalculateRecoil();
            ApplyCompositeTransform();
        }

        private void HandleInputs()
        {
            _isAiming = Input.GetMouseButton(1) && !_isSprinting && !_isInspecting;
            
            bool hasMovement = _characterController != null && _characterController.velocity.magnitude > 0.4f;
            _isSprinting = Input.GetKey(KeyCode.LeftShift) && hasMovement && !_isAiming && !_isInspecting;
        }

        /// <summary>
        /// Calcula a inércia e o atraso do mouse simulando o peso físico do viewmodel.
        /// </summary>
        private void CalculateSway()
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            float aimDamp = _isAiming ? 0.22f : 1.0f;

            // Deslocamento posicional (contra o movimento)
            float moveX = Mathf.Clamp(-mouseX * swayAmount * aimDamp, -maxSwayAmount, maxSwayAmount);
            float moveY = Mathf.Clamp(-mouseY * swayAmount * aimDamp, -maxSwayAmount, maxSwayAmount);
            Vector3 targetSwayPos = new Vector3(moveX, moveY, 0f);

            _swayPosition = Vector3.Lerp(_swayPosition, targetSwayPos, Time.deltaTime * swaySmooth);

            // Rotação angular (pitch, yaw, roll)
            float rotPitch = Mathf.Clamp(-mouseY * swayRotationAmount * aimDamp, -maxSwayRotation, maxSwayRotation);
            float rotYaw = Mathf.Clamp(mouseX * swayRotationAmount * aimDamp, -maxSwayRotation, maxSwayRotation);
            float rotRoll = Mathf.Clamp(-mouseX * (swayRotationAmount * 0.75f) * aimDamp, -maxSwayRotation, maxSwayRotation);

            Quaternion targetSwayRot = Quaternion.Euler(rotPitch, rotYaw, rotRoll);
            _swayRotation = Quaternion.Slerp(_swayRotation, targetSwayRot, Time.deltaTime * swaySmooth);
        }

        /// <summary>
        /// Balanço de respiração estática e passadas dinâmicas em curvas de Lissajous.
        /// </summary>
        private void CalculateBobbing()
        {
            float speed = (_characterController != null) ? _characterController.velocity.magnitude : 0f;
            float aimDamp = _isAiming ? 0.15f : 1.0f;

            if (speed > 0.4f)
            {
                // Em movimento (Caminhada ou Tactical Sprint)
                float currentBobSpeed = _isSprinting ? sprintBobSpeed : walkBobSpeed;
                float currentBobAmount = (_isSprinting ? sprintBobAmount : walkBobAmount) * aimDamp;

                _bobTimer += Time.deltaTime * currentBobSpeed;

                // Curva de Lissajous em 8
                float horizontalBob = Mathf.Sin(_bobTimer) * currentBobAmount;
                float verticalBob = Mathf.Abs(Mathf.Cos(_bobTimer * 2f)) * currentBobAmount * 1.25f;
                float rollBob = Mathf.Cos(_bobTimer) * (currentBobAmount * 18f);

                _swayPosition += new Vector3(horizontalBob, verticalBob, 0f);
                _swayRotation *= Quaternion.Euler(0, 0, rollBob);
            }
            else
            {
                // Parado (Idle Breathing)
                _bobTimer += Time.deltaTime * idleBreathSpeed;
                float breathY = Mathf.Sin(_bobTimer) * idleBreathAmount * aimDamp;
                float breathPitch = Mathf.Cos(_bobTimer * 0.5f) * 0.45f * aimDamp;

                _swayPosition += new Vector3(0, breathY, 0);
                _swayRotation *= Quaternion.Euler(breathPitch, 0, 0);
            }
        }

        /// <summary>
        /// Amortecimento por mola elástica do recuo (Spring-Damper Physics).
        /// </summary>
        private void CalculateRecoil()
        {
            // Interpola a posição e rotação atuais em direção ao alvo com velocidade snappiness
            _currentRecoilPos = Vector3.Lerp(_currentRecoilPos, _targetRecoilPos, Time.deltaTime * recoilSnappiness);
            _currentRecoilRot = Vector3.Lerp(_currentRecoilRot, _targetRecoilRot, Time.deltaTime * recoilSnappiness);

            // Recupera suavemente para a posição de repouso (zero)
            _targetRecoilPos = Vector3.Lerp(_targetRecoilPos, Vector3.zero, Time.deltaTime * recoilReturnSpeed);
            _targetRecoilRot = Vector3.Lerp(_targetRecoilRot, Vector3.zero, Time.deltaTime * recoilReturnSpeed);
        }

        /// <summary>
        /// Aplica o impulso de recuo físico a cada tiro disparado.
        /// </summary>
        public void ApplyFireRecoil()
        {
            float aimMultiplier = _isAiming ? 0.45f : 1.0f;

            float kickZ = recoilKickZ * aimMultiplier;
            float kickY = recoilKickY * aimMultiplier;
            float pitch = recoilPitch * aimMultiplier;
            float randomYaw = Random.Range(-recoilYawSpread, recoilYawSpread) * aimMultiplier;
            float randomRoll = Random.Range(-recoilYawSpread * 0.8f, recoilYawSpread * 0.8f) * aimMultiplier;

            // Empurrão físico para trás e leve elevação
            _targetRecoilPos += new Vector3(Random.Range(-0.005f, 0.005f) * aimMultiplier, kickY, -kickZ);
            _targetRecoilRot += new Vector3(-pitch, randomYaw, randomRoll);
        }

        /// <summary>
        /// Combina postura base, ADS/Sprint, Sway, Bobbing e Recoil na posição local do viewmodel.
        /// </summary>
        private void ApplyCompositeTransform()
        {
            Vector3 baseTargetPos = hipPosition;
            Quaternion baseTargetRot = Quaternion.identity;

            if (_isAiming)
            {
                baseTargetPos = adsPosition;
            }
            else if (_isSprinting)
            {
                baseTargetPos = sprintPosition;
                baseTargetRot = Quaternion.Euler(sprintRotation);
            }

            float transitionSpeed = _isAiming ? 14f : 8f;

            // Posição final composta
            Vector3 targetPosition = baseTargetPos + _swayPosition + _currentRecoilPos;
            Quaternion targetRotation = baseTargetRot * _swayRotation * Quaternion.Euler(_freeAimOffset.y, _freeAimOffset.x, 0f) * Quaternion.Euler(_currentRecoilRot);

            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * transitionSpeed);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * transitionSpeed);
        }

        private Vector2 _freeAimOffset;

        public void SetFreeAimOffset(float x, float y)
        {
            _freeAimOffset = new Vector2(x, y);
        }

        public void ConfigureStancePositions(Vector3 hip, Vector3 ads)
        {
            hipPosition = hip;
            adsPosition = ads;
        }
    }
}
