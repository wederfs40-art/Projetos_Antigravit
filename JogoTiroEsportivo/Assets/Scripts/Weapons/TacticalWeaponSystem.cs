using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace IPSCSimulator.Weapons
{
    using IPSCSimulator.Ballistics;
    using IPSCSimulator.Targets;
    using IPSCSimulator.Utilities;

    /// <summary>
    /// Sistema de Armas Táticas AAA estilo Call of Duty.
    /// Controla o WeaponHolder (objeto pai contendo o Rig de Braços FPS + Arma acoplada à mão direita),
    /// visada ótica perfeita (ADS) via socket aimPoint centralizado no Viewport (0.5, 0.5),
    /// zeragem balística por scroll wheel (25m, 50m, 100m, 200m), weapon sway e recuo físico procedural.
    /// </summary>
    public class TacticalWeaponSystem : MonoBehaviour
    {
        // ========================================================================================
        // 1. ESTRUTURA DO VIEWMODEL E HIERARQUIA DE BRAÇOS (WEAPON HOLDER)
        // ========================================================================================
        [Header("1. Estrutura do Viewmodel e Hierarquia")]
        [Tooltip("Transform do WeaponHolder (pai direto contendo os braços FPS e a arma). Se nulo, usa o próprio Transform.")]
        [SerializeField] private Transform weaponHolder;

        [Tooltip("Câmera principal em primeira pessoa do operador.")]
        [SerializeField] private Camera playerCamera;

        [Tooltip("Ponto de referência de visada na arma (Aim Point) posicionado no alinhamento exato entre alça e massa de mira.")]
        [SerializeField] private Transform aimPoint;

        [Tooltip("Ponto de disparo na boca do cano (Muzzle Point) para efeitos visuais de clarão e fumaça.")]
        [SerializeField] private Transform muzzlePoint;

        [Tooltip("Posição relativa da arma em postura de descanso/pronto-baixo (Hipfire).")]
        [SerializeField] private Vector3 hipPosition = new Vector3(0.12f, -0.16f, 0.32f);

        [Tooltip("Rotação relativa da arma em postura de descanso/pronto-baixo (em graus).")]
        [SerializeField] private Vector3 hipRotation = Vector3.zero;

        [Tooltip("Distância confortável da alça de mira até a lente da câmera no modo ADS (em metros).")]
        [SerializeField] private float adsEyeRelief = 0.20f;

        [Tooltip("Velocidade da transição suave entre Hipfire e ADS.")]
        [SerializeField] private float adsSpeed = 16.0f;

        [Tooltip("Campo de visão (FOV) da câmera ao mirar (efeito de foco visual e aproximação COD).")]
        [SerializeField] private float adsCameraFOV = 50.0f;
        private float _defaultCameraFOV = 72.0f;

        // ========================================================================================
        // 2. CALIBRAÇÃO DO DESENVOLVEDOR (DEV CALIBRATION OFFSETS)
        // ========================================================================================
        [Header("2. Calibração do Desenvolvedor (Offsets)")]
        [Tooltip("Offset fino de posição (X, Y, Z) para ajuste individual da arma na visada ADS.")]
        public Vector3 calibrationPosOffset = Vector3.zero;

        [Tooltip("Offset fino de rotação (Pitch no X, Yaw no Y, Roll no Z) para nivelar o ferrolho com o horizonte.")]
        public Vector3 calibrationRotOffset = Vector3.zero;

        [Tooltip("Comprimento da linha de guia visual de mira desenhada na Scene view via Gizmos.")]
        [SerializeField] private float debugGizmoLineLength = 50.0f;

        // ========================================================================================
        // 3. SISTEMA DE ZERAGEM BALÍSTICA (ZEROING ESTILO CALL OF DUTY / TARKOV)
        // ========================================================================================
        [Header("3. Zeragem pelo Jogador (Zeroing)")]
        [Tooltip("Distâncias de zeragem selecionáveis pelo operador ao rodar o scroll do mouse durante a mira.")]
        [SerializeField] private int[] zeroDistances = new int[] { 25, 50, 100, 200 };

        [Tooltip("Índice da distância atual de zeragem selecionada.")]
        [SerializeField] private int currentZeroIndex = 0; // Padrão: 25m

        [Tooltip("Inclinação sutil do Pitch (em graus) adicionada a cada nível de zeragem para compensar a queda da bala.")]
        [SerializeField] private float pitchStepPerZero = 0.08f;

        [Tooltip("Elemento de texto TextMeshPro na interface para exibir a zeragem atual (ex: 'ZERO: 50m').")]
        [SerializeField] private TextMeshProUGUI zeroingTextUI;

        // ========================================================================================
        // 4. BALANÇO SUAVE DO MOUSE (WEAPON SWAY ESTILO AAA)
        // ========================================================================================
        [Header("4. Inércia e Balanço da Arma (Weapon Sway)")]
        [SerializeField] private float swayAmount = 0.022f;
        [SerializeField] private float maxSwayAmount = 0.045f;
        [SerializeField] private float swaySmoothSpeed = 8.5f;
        [SerializeField] private float swayTiltAmount = 2.2f;

        // ========================================================================================
        // 5. RECUO PROCEDURAL REALISTA (PROCEDURAL RECOIL KICK)
        // ========================================================================================
        [Header("5. Recuo Físico Procedural (Procedural Recoil)")]
        [Tooltip("Coice físico da arma para trás em direção ao peito do operador (eixo Z).")]
        [SerializeField] private float recoilKickZ = 0.038f;

        [Tooltip("Salto vertical da arma durante o disparo instantâneo (eixo Y).")]
        [SerializeField] private float recoilKickY = 0.010f;

        [Tooltip("Elevação angular do cano (Pitch) no momento do tiro.")]
        [SerializeField] private float recoilKickPitch = 3.8f;

        [Tooltip("Variação aleatória lateral da arma ao atirar (Yaw/Roll).")]
        [SerializeField] private float recoilKickYawRandom = 0.5f;

        [Tooltip("Rapidez do coice imediato da arma.")]
        [SerializeField] private float recoilSnappiness = 26.0f;

        [Tooltip("Velocidade com que a arma retorna à posição de descanso ou mira.")]
        [SerializeField] private float recoilReturnSpeed = 12.0f;

        // ========================================================================================
        // 6. BALÍSTICA, DISPARO E EFEITOS
        // ========================================================================================
        [Header("6. Balística e Disparo (Raycast Centralizado)")]
        [SerializeField] private float fireRate = 0.12f; // Cadência
        [SerializeField] private bool isAutomatic = false;
        [SerializeField] private float bulletVelocity = 380f;
        [SerializeField] private float maxRaycastDistance = 350f;
        [SerializeField] private LayerMask hitLayers = ~0;
        [SerializeField] private GameObject bulletHoleDecalPrefab;
        [SerializeField] private GameObject bulletPrefab;

        [Header("Efeitos Visuais e Sonoros")]
        [SerializeField] private ParticleSystem muzzleFlash;
        [SerializeField] private AudioSource fireAudioSource;
        [SerializeField] private AudioClip fireSound;

        // Estados internos
        private bool _isAiming = false;
        private float _adsProgress = 0.0f;
        private float _lastFireTime = 0.0f;

        private Vector3 _currentSwayPos;
        private Quaternion _currentSwayRot = Quaternion.identity;

        private Vector3 _targetRecoilPos;
        private Vector3 _currentRecoilPos;
        private Vector3 _targetRecoilRot;
        private Vector3 _currentRecoilRot;

        public bool IsAiming => _isAiming;
        public float ADSProgress => _adsProgress;
        public int CurrentZeroDistance => (zeroDistances != null && zeroDistances.Length > 0) ? zeroDistances[currentZeroIndex] : 25;

        // ========================================================================================
        // CICLO DE VIDA (INITIALIZATION)
        // ========================================================================================
        private void Awake()
        {
            if (weaponHolder == null) weaponHolder = transform;
            if (playerCamera == null) playerCamera = GetComponentInParent<Camera>() ?? Camera.main;

            if (playerCamera != null)
            {
                _defaultCameraFOV = playerCamera.fieldOfView;
            }

            UpdateZeroingUI();
        }

        private void Start()
        {
            UpdateZeroingUI();
        }

        private void Update()
        {
            HandleAimInput();
            HandleZeroingInput();
            HandleWeaponSway();
            HandleProceduralRecoilPhysics();
            HandleFiringInput();
            ApplyProceduralTransform();
        }

        // ========================================================================================
        // 1. CONTROLE DE MIRA (ADS) E ZOOM DE FOV
        // ========================================================================================
        private void HandleAimInput()
        {
            // Mira ativa com botão direito do mouse
            _isAiming = Input.GetMouseButton(1);

            float targetProgress = _isAiming ? 1.0f : 0.0f;
            _adsProgress = Mathf.MoveTowards(_adsProgress, targetProgress, Time.deltaTime * adsSpeed);

            // Zoom óptico da câmera (Call of Duty Focus)
            if (playerCamera != null)
            {
                float targetFOV = Mathf.Lerp(_defaultCameraFOV, adsCameraFOV, _adsProgress);
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * adsSpeed);
            }
        }

        // ========================================================================================
        // 2. SISTEMA DE ZERAGEM (ZEROING COM SCROLL DO MOUSE)
        // ========================================================================================
        private void HandleZeroingInput()
        {
            // Apenas alterna a zeragem enquanto o operador estiver ativamente em ADS
            if (!_isAiming || zeroDistances == null || zeroDistances.Length <= 1) return;

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.04f)
            {
                if (scroll > 0f)
                {
                    // Aumenta distância de zeragem (ex: 25 -> 50 -> 100 -> 200m)
                    currentZeroIndex = Mathf.Min(currentZeroIndex + 1, zeroDistances.Length - 1);
                }
                else
                {
                    // Diminui distância de zeragem
                    currentZeroIndex = Mathf.Max(currentZeroIndex - 1, 0);
                }

                UpdateZeroingUI();
            }
        }

        private void UpdateZeroingUI()
        {
            if (zeroingTextUI != null)
            {
                zeroingTextUI.text = $"ZERO: <b><color=#FFD700>{CurrentZeroDistance}m</color></b>";
            }
        }

        // ========================================================================================
        // 3. INÉRCIA DO MOUSE (WEAPON SWAY)
        // ========================================================================================
        private void HandleWeaponSway()
        {
            // Reduz o sway em 85% durante a visada ADS para estabilidade de tiro precisa
            float swayMultiplier = Mathf.Lerp(1.0f, 0.15f, _adsProgress);

            float mouseX = Input.GetAxis("Mouse X") * swayAmount * swayMultiplier;
            float mouseY = Input.GetAxis("Mouse Y") * swayAmount * swayMultiplier;

            mouseX = Mathf.Clamp(mouseX, -maxSwayAmount, maxSwayAmount);
            mouseY = Mathf.Clamp(mouseY, -maxSwayAmount, maxSwayAmount);

            Vector3 targetSwayPos = new Vector3(-mouseX, -mouseY, 0f);
            _currentSwayPos = Vector3.Lerp(_currentSwayPos, targetSwayPos, Time.deltaTime * swaySmoothSpeed);

            Quaternion targetSwayRot = Quaternion.Euler(mouseY * swayTiltAmount, -mouseX * swayTiltAmount, -mouseX * swayTiltAmount * 1.5f);
            _currentSwayRot = Quaternion.Slerp(_currentSwayRot, targetSwayRot, Time.deltaTime * swaySmoothSpeed);
        }

        // ========================================================================================
        // 4. COICE FÍSICO PROCEDURAL (PROCEDURAL RECOIL)
        // ========================================================================================
        private void HandleProceduralRecoilPhysics()
        {
            _currentRecoilPos = Vector3.Lerp(_currentRecoilPos, _targetRecoilPos, Time.deltaTime * recoilSnappiness);
            _targetRecoilPos = Vector3.Lerp(_targetRecoilPos, Vector3.zero, Time.deltaTime * recoilReturnSpeed);

            _currentRecoilRot = Vector3.Lerp(_currentRecoilRot, _targetRecoilRot, Time.deltaTime * recoilSnappiness);
            _targetRecoilRot = Vector3.Lerp(_targetRecoilRot, Vector3.zero, Time.deltaTime * recoilReturnSpeed);
        }

        public void ApplyRecoilKick()
        {
            // Reduz o coice percebido no ADS proporcionalmente para controle tático
            float adsRecoilDamp = Mathf.Lerp(1.0f, 0.65f, _adsProgress);

            _targetRecoilPos += new Vector3(
                UnityEngine.Random.Range(-0.003f, 0.003f),
                recoilKickY * adsRecoilDamp,
                -recoilKickZ * adsRecoilDamp
            );

            _targetRecoilRot += new Vector3(
                -recoilKickPitch * adsRecoilDamp,
                UnityEngine.Random.Range(-recoilKickYawRandom, recoilKickYawRandom) * adsRecoilDamp,
                UnityEngine.Random.Range(-recoilKickYawRandom * 0.5f, recoilKickYawRandom * 0.5f) * adsRecoilDamp
            );
        }

        // ========================================================================================
        // 5. CÁLCULO DINÂMICO E ANALÍTICO DE MIRA (AIMPOINT NO CENTRO DA CÂMERA)
        // ========================================================================================
        private void ApplyProceduralTransform()
        {
            if (weaponHolder == null) return;

            Vector3 targetAdsPos;
            Vector3 targetAdsRot;

            // Inclinação angular decorrente da zeragem atual selecionada (compensa queda balística)
            float zeroPitch = currentZeroIndex * pitchStepPerZero;

            if (aimPoint != null && playerCamera != null)
            {
                // Calcula a orientação e posição local do WeaponHolder para que o aimPoint fique
                // rigorosamente colinear com o centro óptico da câmera (Viewport 0.5, 0.5).
                Vector3 aimLocalPosInHolder = weaponHolder.InverseTransformPoint(aimPoint.position);
                Quaternion aimLocalRotInHolder = Quaternion.Inverse(weaponHolder.rotation) * aimPoint.rotation;

                // Rotação necessária no holder para apontar o cano perfeitamente paralelo ao horizonte da câmera
                Quaternion alignRot = Quaternion.Inverse(aimLocalRotInHolder);

                // Aplica a compensação de zeragem e o offset de calibração do desenvolvedor
                Quaternion calibratedRot = Quaternion.Euler(calibrationRotOffset.x - zeroPitch, calibrationRotOffset.y, calibrationRotOffset.z) * alignRot;

                // Posição para colocar o aimPoint exatamente na linha focal da câmera (X=0, Y=0 no viewport central):
                Vector3 desiredAimPosInCam = new Vector3(0f, 0f, adsEyeRelief) + calibrationPosOffset;
                targetAdsPos = desiredAimPosInCam - (calibratedRot * aimLocalPosInHolder);

                Vector3 euler = calibratedRot.eulerAngles;
                if (euler.x > 180f) euler.x -= 360f;
                if (euler.y > 180f) euler.y -= 360f;
                if (euler.z > 180f) euler.z -= 360f;
                targetAdsRot = euler;
            }
            else
            {
                // Fallback padrão com calibração
                targetAdsPos = new Vector3(0f, -0.024f, adsEyeRelief) + calibrationPosOffset;
                targetAdsRot = new Vector3(-3.2f - zeroPitch, 0f, 0f) + calibrationRotOffset;
            }

            // Interpolação suave de postura entre Hipfire e ADS
            Vector3 finalStancePos = Vector3.Lerp(hipPosition, targetAdsPos, _adsProgress);
            Vector3 finalStanceRot = Vector3.Lerp(hipRotation, targetAdsRot, _adsProgress);

            // Aplica as camadas acumuladas de Sway inercial e Recuo físico procedural
            weaponHolder.localPosition = finalStancePos + _currentSwayPos + _currentRecoilPos;
            weaponHolder.localRotation = Quaternion.Euler(finalStanceRot + _currentRecoilRot) * _currentSwayRot;
        }

        // ========================================================================================
        // 6. DISPARO REALISTA VIA RAYCAST DO CENTRO DA TELA (VIEWPORT 0.5, 0.5)
        // ========================================================================================
        private void HandleFiringInput()
        {
            // Bloqueia disparos se o cursor do mouse estiver destravado (interagindo com menus/UI)
            if (Cursor.lockState != CursorLockMode.Locked) return;

            bool wantsFire = isAutomatic ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
            if (wantsFire && Time.time >= _lastFireTime + fireRate)
            {
                _lastFireTime = Time.time;
                ExecuteTacticalShot();
            }
        }

        public void ExecuteTacticalShot()
        {
            if (playerCamera == null) playerCamera = GetComponentInParent<Camera>() ?? Camera.main;

            // 1. Raycast sai rigorosamente do centro exato da câmera (Viewport 0.5, 0.5)
            Ray centerRay = (playerCamera != null)
                ? playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
                : new Ray(transform.position, transform.forward);

            Vector3 shootOrigin = centerRay.origin;
            Vector3 shootDir = centerRay.direction;

            // 2. Colisão e impacto contra superfícies e alvos
            RaycastHit[] hits = Physics.RaycastAll(shootOrigin, shootDir, maxRaycastDistance, hitLayers, QueryTriggerInteraction.Collide);
            if (hits != null && hits.Length > 0)
            {
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider == null || Bullet.IsPlayerOrWeapon(hit.collider)) continue;

                    Bullet.ProcessDirectHit(hit, shootDir, bulletHoleDecalPrefab);
                    break;
                }
            }

            // 3. Instancia projétil físico visual se configurado
            if (bulletPrefab != null)
            {
                Vector3 spawnPos = (muzzlePoint != null) ? muzzlePoint.position : (shootOrigin + shootDir * 0.25f);
                GameObject bulletObj = Instantiate(bulletPrefab, spawnPos, Quaternion.LookRotation(shootDir));
                bulletObj.SetActive(true);
                Bullet bScript = bulletObj.GetComponent<Bullet>();
                if (bScript != null)
                {
                    bScript.Initialize(bulletVelocity, shootDir, 1f, bulletHoleDecalPrefab, hitLayers);
                }
            }

            // 4. Efeitos de Muzzle Flash e Som
            if (muzzleFlash != null) muzzleFlash.Play();
            if (fireAudioSource != null && fireSound != null)
            {
                fireAudioSource.PlayOneShot(fireSound);
            }

            // 5. Aplica o recuo procedural AAA
            ApplyRecoilKick();
        }

        // ========================================================================================
        // 7. GIZMOS VISUAIS NA SCENE VIEW (LINHA VERMELHA DE ALINHAMENTO)
        // ========================================================================================
        private void OnDrawGizmos()
        {
            if (aimPoint != null)
            {
                // Desenha a linha de visada exata na cor vermelha (estilo laser óptico)
                Gizmos.color = Color.red;
                Gizmos.DrawLine(aimPoint.position, aimPoint.position + aimPoint.forward * debugGizmoLineLength);

                // Desenha uma pequena esfera no ponto central da mira
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(aimPoint.position, 0.005f);
            }
            else if (muzzlePoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(muzzlePoint.position, muzzlePoint.position + muzzlePoint.forward * debugGizmoLineLength);
            }
        }
    }
}
