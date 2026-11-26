using UnityEngine;

namespace IPSCSimulator.Weapons
{
    /// <summary>
    /// Controla a visada precisa (ADS), balanço natural de respiração (weapon sway),
    /// estabilização ao prender a respiração (Shift) e recuo procedural da arma.
    /// </summary>
    public class WeaponHandling : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] private Weapon weapon;
        [SerializeField] private Transform weaponTransform;

        [Header("Posicionamento (ADS e Hipfire)")]
        [Tooltip("Posição relativa da arma em tiro instintivo / tronco.")]
        [SerializeField] private Vector3 hipPosition = new Vector3(0.2f, -0.2f, 0.4f);
        [Tooltip("Posição em que a alça/massa de mira fica no centro exato da câmera.")]
        [SerializeField] private Vector3 adsPosition = new Vector3(0f, -0.12f, 0.3f);
        [SerializeField] private float adsSpeed = 14f;

        [Header("Respiração (Sway)")]
        [SerializeField] private float normalSwaySpeed = 1.8f;
        [SerializeField] private float normalSwayAmount = 0.008f;
        [SerializeField] private float breathHoldMultiplier = 0.2f;

        [Header("Recuo Procedural")]
        [Tooltip("Coice para trás (eixo Z) e leve desvio nos eixos X/Y.")]
        [SerializeField] private Vector3 recoilKickPosition = new Vector3(0.015f, 0.035f, -0.11f);
        [Tooltip("Elevação do cano (Pitch) e variação lateral (Yaw/Roll).")]
        [SerializeField] private Vector3 recoilKickRotation = new Vector3(-14f, 3.5f, 2f);
        [SerializeField] private float recoilSnappiness = 26f;
        [SerializeField] private float recoilReturnSpeed = 12f;

        // Estados internos
        private bool _isAiming;
        private bool _isHoldingBreath;
        private float _swayTimer;
        private Vector3 _currentRecoilPosition;
        private Vector3 _targetRecoilPosition;
        private Vector3 _currentRecoilRotation;
        private Vector3 _targetRecoilRotation;
        private ProceduralViewmodelMotion _proceduralMotion;

        [Header("Disparo e Cadência")]
        [SerializeField] private bool isAutomatic = false;
        [SerializeField] private float fireRate = 0.11f; // ~550 disparos por minuto para SMG
        private float _lastFireTime = 0f;

        public bool IsAiming => _isAiming;
        public bool IsHoldingBreath => _isHoldingBreath;

        public void ConfigureHandling(bool auto, float recoilMult, Vector3 adsPos)
        {
            isAutomatic = auto;
            adsPosition = adsPos;
            recoilKickPosition *= recoilMult;
            recoilKickRotation *= recoilMult;
        }

        private void Start()
        {
            _proceduralMotion = GetComponent<ProceduralViewmodelMotion>();
        }

        private void OnEnable()
        {
            Weapon.OnWeaponFired += ApplyRecoil;
        }

        private void OnDisable()
        {
            Weapon.OnWeaponFired -= ApplyRecoil;
        }

        private void Update()
        {
            HandleInputs();

            // Se ProceduralViewmodelMotion estiver ativo, ele assume o controle unificado da postura, sway e recuo
            if (_proceduralMotion == null)
            {
                ProcessSwayAndBreathing();
                ProcessRecoilRecovery();
                UpdateWeaponTransform();
            }
        }

        private void HandleInputs()
        {
            // Botão direito: Mira (ADS)
            _isAiming = Input.GetMouseButton(1);

            // Shift esquerdo: Prende a respiração para estabilizar a mira
            _isHoldingBreath = Input.GetKey(KeyCode.LeftShift);

            // Botão esquerdo: Disparo (Semi-Automático ou Automático Contínuo para SMG)
            if (weapon != null)
            {
                if (isAutomatic)
                {
                    if (Input.GetMouseButton(0) && Time.time >= _lastFireTime + fireRate)
                    {
                        _lastFireTime = Time.time;
                        weapon.TryFire();
                    }
                }
                else
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        weapon.TryFire();
                    }
                }
            }

            // Tecla R: Recarregar tático com animação
            if (Input.GetKeyDown(KeyCode.R) && weapon != null)
            {
                weapon.StartReload();
            }

            // Seleção rápida de armas (Teclas 1, 2, 3 e Scroll do Mouse)
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                IPSCSimulator.Match.TacticalGameManager.Instance?.ChangeWeaponInGame(IPSCSimulator.Match.SelectedWeaponType.Pistol_9mm);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                IPSCSimulator.Match.TacticalGameManager.Instance?.ChangeWeaponInGame(IPSCSimulator.Match.SelectedWeaponType.Revolver_DanWesson);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                IPSCSimulator.Match.TacticalGameManager.Instance?.ChangeWeaponInGame(IPSCSimulator.Match.SelectedWeaponType.SMG_Tactical);
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0.05f)
            {
                IPSCSimulator.Match.TacticalGameManager.Instance?.CycleWeapon(1);
            }
            else if (scroll < -0.05f)
            {
                IPSCSimulator.Match.TacticalGameManager.Instance?.CycleWeapon(-1);
            }
        }

        private void ProcessSwayAndBreathing()
        {
            float speed = normalSwaySpeed;
            float amount = normalSwayAmount;

            if (_isAiming)
            {
                amount *= 0.6f;
            }

            if (_isHoldingBreath)
            {
                amount *= breathHoldMultiplier;
                speed *= 0.5f;
            }

            _swayTimer += Time.deltaTime * speed;
        }

        private void ProcessRecoilRecovery()
        {
            // Retorna suavemente as posições e rotações de recuo para zero (amortecimento elástico)
            _targetRecoilPosition = Vector3.Lerp(_targetRecoilPosition, Vector3.zero, recoilReturnSpeed * Time.deltaTime);
            _currentRecoilPosition = Vector3.Lerp(_currentRecoilPosition, _targetRecoilPosition, recoilSnappiness * Time.deltaTime);

            _targetRecoilRotation = Vector3.Lerp(_targetRecoilRotation, Vector3.zero, recoilReturnSpeed * Time.deltaTime);
            _currentRecoilRotation = Vector3.Lerp(_currentRecoilRotation, _targetRecoilRotation, recoilSnappiness * Time.deltaTime);
        }

        private void UpdateWeaponTransform()
        {
            if (weaponTransform == null) return;

            // 1. Interpolação entre posição de quadril e ADS
            Vector3 targetBasePosition = _isAiming ? adsPosition : hipPosition;

            // 2. Balanço harmônico (Lissajous para respiração natural)
            float swayMult = _isHoldingBreath ? breathHoldMultiplier : 1f;
            float swayX = Mathf.Sin(_swayTimer) * normalSwayAmount * swayMult;
            float swayY = Mathf.Cos(_swayTimer * 2f) * (normalSwayAmount * 0.5f) * swayMult;
            Vector3 swayOffset = new Vector3(swayX, swayY, 0f);

            // 3. Aplica posição combinada com recuo procedural
            weaponTransform.localPosition = Vector3.Lerp(
                weaponTransform.localPosition,
                targetBasePosition + swayOffset + _currentRecoilPosition,
                adsSpeed * Time.deltaTime
            );

            // 4. Aplica rotação de recuo
            weaponTransform.localRotation = Quaternion.Euler(_currentRecoilRotation);
        }

        /// <summary>
        /// Aplica o coice mecânico ao disparar.
        /// </summary>
        private void ApplyRecoil()
        {
            float adsFactor = _isAiming ? 0.7f : 1f;

            // Coice para trás (empurrão)
            _targetRecoilPosition += new Vector3(
                Random.Range(-recoilKickPosition.x, recoilKickPosition.x),
                Random.Range(recoilKickPosition.y * 0.5f, recoilKickPosition.y),
                recoilKickPosition.z * adsFactor
            );

            // Elevação do cano e torção
            _targetRecoilRotation += new Vector3(
                recoilKickRotation.x,
                Random.Range(-recoilKickRotation.y, recoilKickRotation.y),
                Random.Range(-recoilKickRotation.z, recoilKickRotation.z)
            );
        }
    }
}
