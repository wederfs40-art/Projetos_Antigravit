using System;
using System.Collections;
using UnityEngine;

namespace IPSCSimulator.Weapons
{
    using IPSCSimulator.Ballistics;
    using IPSCSimulator.Utilities;

    /// <summary>
    /// Controla o disparo mecânico, câmara, cano, munição, ferrolho,
    /// ejeção física de cápsulas de latão e animação de troca de carregador (magazine).
    /// </summary>
    public class Weapon : MonoBehaviour
    {
        [Header("Balística e Muzzle Point")]
        [Tooltip("Ponta do cano onde a bala nasce fisicamente (Height Over Bore).")]
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private Transform ejectionPort;
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private GameObject bulletHoleDecalPrefab;
        [Tooltip("Velocidade de saída do cano em metros por segundo.")]
        [SerializeField] private float muzzleVelocity = 380f; // 9mm padrão (360-390 m/s)
        [SerializeField] private float gravityMultiplier = 1f;
        [SerializeField] private LayerMask hitLayers = ~0; // Garante que atinge todas as camadas

        [Header("Alinhamento de Mira & Vetor de Disparo")]
        [Tooltip("Ponto de referência (socket) na ponta da massa de mira (Front Sight).")]
        [SerializeField] private Transform frontSightSocket;
        [Tooltip("Ponto de referência (socket) no entalhe da alça de mira (Rear Sight).")]
        [SerializeField] private Transform rearSightSocket;
        [Tooltip("Câmera do jogador para disparos e alinhamento axial com o centro óptico.")]
        [SerializeField] private Camera playerCamera;
        [Tooltip("Garante que o vetor de disparo saia exatamente do centro da câmera no alinhamento das miras, sem inclinação para baixo.")]
        [SerializeField] private bool fireFromCameraCenter = true;

        [Header("Capacidade e Ferrolho")]
        [SerializeField] private int magazineCapacity = 17;
        [SerializeField] private int currentAmmo = 17;
        [SerializeField] private Transform slideTransform;
        [SerializeField] private Transform magazineTransform;
        [SerializeField] private Vector3 slideLockedLocalPosition = new Vector3(0, 0, -0.04f);
        private Vector3 _slideOriginalLocalPosition;
        private Vector3 _magOriginalLocalPosition;
        private bool _isSlideLocked = false;
        private bool _isReloading = false;

        [Header("Efeitos Sonoros e Visuais")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip fireSound;
        [SerializeField] private AudioClip dryFireSound;
        [SerializeField] private AudioClip slideLockSound;
        [SerializeField] private AudioClip reloadSound;
        [SerializeField] private ParticleSystem muzzleFlash;

        private Light _muzzleLight;
        private Coroutine _flashCoroutine;
        private Coroutine _reloadCoroutine;

        // Eventos públicos
        public static event Action OnWeaponFired;
        public static event Action<int, int> OnAmmoChanged; // (current, max)

        public int CurrentAmmo => currentAmmo;
        public int MagazineCapacity => magazineCapacity;
        public bool IsSlideLocked => _isSlideLocked;
        public bool IsReloading => _isReloading;

        public Transform FrontSightSocket { get => frontSightSocket; set => frontSightSocket = value; }
        public Transform RearSightSocket { get => rearSightSocket; set => rearSightSocket = value; }
        public Camera PlayerCamera { get => playerCamera; set => playerCamera = value; }
        public bool FireFromCameraCenter { get => fireFromCameraCenter; set => fireFromCameraCenter = value; }

        public void ConfigureWeapon(int capacity, float velocity, float gravity = 1f)
        {
            magazineCapacity = capacity;
            currentAmmo = capacity;
            muzzleVelocity = velocity;
            gravityMultiplier = gravity;
            _isSlideLocked = false;
            _isReloading = false;
            OnAmmoChanged?.Invoke(currentAmmo, magazineCapacity);
        }

        private void Awake()
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponentInParent<Camera>() ?? Camera.main;
            }

            if (slideTransform != null)
                _slideOriginalLocalPosition = slideTransform.localPosition;

            if (magazineTransform != null)
                _magOriginalLocalPosition = magazineTransform.localPosition;

            // Cria luz pontual de clarão de disparo (Muzzle Flash Dynamic Light)
            if (muzzlePoint != null)
            {
                GameObject lightObj = new GameObject("MuzzleFlash_Light");
                lightObj.transform.SetParent(muzzlePoint);
                lightObj.transform.localPosition = Vector3.zero;
                _muzzleLight = lightObj.AddComponent<Light>();
                _muzzleLight.type = LightType.Point;
                _muzzleLight.color = new Color(1f, 0.78f, 0.35f);
                _muzzleLight.range = 5.5f;
                _muzzleLight.intensity = 4.0f;
                _muzzleLight.enabled = false;
            }
        }

        private void Start()
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponentInParent<Camera>() ?? Camera.main;
            }
            OnAmmoChanged?.Invoke(currentAmmo, magazineCapacity);
        }

        /// <summary>
        /// Aciona o mecanismo de disparo da arma.
        /// Garante que o vetor de disparo saia exatamente do centro da câmera no alinhamento das miras, sem inclinação para baixo.
        /// </summary>
        public bool TryFire()
        {
            if (_isReloading) return false;

            // Se a arma estiver vazia ou com o ferrolho travado aberto
            if (currentAmmo <= 0 || _isSlideLocked)
            {
                PlayDryFire();
                StartReload(); // Tenta recarregar automaticamente se clicou no seco
                return false;
            }

            // 1. Consome munição do carregador
            currentAmmo--;
            OnAmmoChanged?.Invoke(currentAmmo, magazineCapacity);

            // Garante que o muzzlePoint exista
            if (muzzlePoint == null)
            {
                muzzlePoint = transform.Find("MuzzlePoint");
                if (muzzlePoint == null) muzzlePoint = transform;
            }

            if (playerCamera == null)
            {
                playerCamera = GetComponentInParent<Camera>() ?? Camera.main;
            }

            // Determina a origem e a direção de disparo:
            // Alinhado ao centro da câmera e ao eixo óptico da visada (sem inclinação para baixo)
            Vector3 shootOrigin = (playerCamera != null) ? playerCamera.transform.position : muzzlePoint.position;
            Vector3 shootDir = (playerCamera != null) ? playerCamera.transform.forward : muzzlePoint.forward;

            // 2. Instancia o projétil no centro óptico da câmera ou cano e garante que esteja ATIVO na cena
            if (bulletPrefab != null)
            {
                // Se disparando pelo centro da câmera, spawna logo à frente da lente para trajetória limpa
                Vector3 spawnPos = (fireFromCameraCenter && playerCamera != null)
                    ? (playerCamera.transform.position + playerCamera.transform.forward * 0.20f)
                    : muzzlePoint.position;

                GameObject bulletObj = Instantiate(bulletPrefab, spawnPos, Quaternion.LookRotation(shootDir));
                bulletObj.SetActive(true); // Impede que o projétil nasça desativado
                Bullet bulletScript = bulletObj.GetComponent<Bullet>();
                if (bulletScript != null)
                {
                    bulletScript.Initialize(muzzleVelocity, shootDir, gravityMultiplier, bulletHoleDecalPrefab, hitLayers);
                }
            }
            else
            {
                // Fallback instantâneo direto de Raycast caso o prefab do projétil não esteja presente
                RaycastHit[] hits = Physics.RaycastAll(shootOrigin, shootDir, 250f, hitLayers, QueryTriggerInteraction.Collide);
                if (hits != null && hits.Length > 0)
                {
                    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                    foreach (var h in hits)
                    {
                        if (h.collider == null || Bullet.IsPlayerOrWeapon(h.collider)) continue;
                        Bullet.ProcessDirectHit(h, shootDir, bulletHoleDecalPrefab);
                        break;
                    }
                }
            }

            // 3. Efeitos visuais e sonoros de disparo
            if (muzzleFlash != null) muzzleFlash.Play();
            TriggerMuzzleFlashLight();
            if (audioSource != null && fireSound != null) audioSource.PlayOneShot(fireSound);

            // 4. Ejeção física da cápsula de latão
            EjectBrassCasing();

            // 5. Notifica o sistema de Timer e o manuseio de recuo
            OnWeaponFired?.Invoke();

            // 6. Verifica se foi o último tiro para travar o ferrolho aberto (Slide Lock)
            if (currentAmmo == 0)
            {
                LockSlideOpen();
            }

            return true;
        }

        private void TriggerMuzzleFlashLight()
        {
            if (_muzzleLight != null)
            {
                if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
                _flashCoroutine = StartCoroutine(FlashLightCoroutine());
            }
        }

        private IEnumerator FlashLightCoroutine()
        {
            _muzzleLight.enabled = true;
            yield return new WaitForSeconds(0.04f);
            _muzzleLight.enabled = false;
        }

        /// <summary>
        /// Instancia um estojo de latão ejetado com impulso físico e rotação angular realista.
        /// </summary>
        private void EjectBrassCasing()
        {
            Vector3 spawnPos = (ejectionPort != null) ? ejectionPort.position : (transform.position + transform.right * 0.06f + transform.up * 0.03f + transform.forward * 0.05f);
            GameObject casing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            casing.name = "Spent_Brass_Casing";
            casing.transform.position = spawnPos;
            casing.transform.localScale = new Vector3(0.010f, 0.016f, 0.010f);
            casing.transform.rotation = UnityEngine.Random.rotation;

            Material brassMat = ProceduralMaterials.CreateMaterial(new Color(0.92f, 0.78f, 0.28f), "Mat_Brass_Casing", 0.9f, 0.95f);
            casing.GetComponent<MeshRenderer>().material = brassMat;

            // Física realista de arremesso lateral
            Rigidbody rb = casing.AddComponent<Rigidbody>();
            rb.mass = 0.012f;
            Vector3 ejectDir = (transform.right * UnityEngine.Random.Range(2.2f, 3.4f)) + 
                              (transform.up * UnityEngine.Random.Range(1.2f, 2.0f)) - 
                              (transform.forward * UnityEngine.Random.Range(0.2f, 0.6f));
            rb.linearVelocity = ejectDir;
            rb.angularVelocity = new Vector3(UnityEngine.Random.Range(-30f, 30f), UnityEngine.Random.Range(-30f, 30f), UnityEngine.Random.Range(-30f, 30f));

            Destroy(casing, 3.5f);
        }

        private void LockSlideOpen()
        {
            _isSlideLocked = true;
            if (slideTransform != null)
            {
                slideTransform.localPosition = _slideOriginalLocalPosition + slideLockedLocalPosition;
            }
            if (audioSource != null && slideLockSound != null)
            {
                audioSource.PlayOneShot(slideLockSound);
            }
        }

        /// <summary>
        /// Inicia a animação tática física de recarregamento da arma.
        /// </summary>
        public void StartReload()
        {
            if (_isReloading || currentAmmo == magazineCapacity) return;
            if (_reloadCoroutine != null) StopCoroutine(_reloadCoroutine);
            _reloadCoroutine = StartCoroutine(ReloadAnimationCoroutine());
        }

        public void Reload()
        {
            StartReload();
        }

        private IEnumerator ReloadAnimationCoroutine()
        {
            _isReloading = true;

            // 1. Som e movimento de soltar o carregador (Magazine Out)
            if (audioSource != null)
            {
                audioSource.PlayOneShot(ProceduralAudio.CreateMagOutClip());
            }

            if (magazineTransform != null)
            {
                float t = 0f;
                while (t < 0.25f)
                {
                    t += Time.deltaTime;
                    magazineTransform.localPosition = _magOriginalLocalPosition + new Vector3(0, -Mathf.Lerp(0f, 0.18f, t / 0.25f), 0);
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(0.25f);
            }

            yield return new WaitForSeconds(0.25f);

            // 2. Novo carregador inserido e travado no poço (Magazine In)
            if (audioSource != null)
            {
                audioSource.PlayOneShot(ProceduralAudio.CreateMagInClip());
            }

            if (magazineTransform != null)
            {
                float t = 0f;
                while (t < 0.25f)
                {
                    t += Time.deltaTime;
                    magazineTransform.localPosition = _magOriginalLocalPosition + new Vector3(0, -Mathf.Lerp(0.18f, 0f, t / 0.25f), 0);
                    yield return null;
                }
                magazineTransform.localPosition = _magOriginalLocalPosition;
            }
            else
            {
                yield return new WaitForSeconds(0.25f);
            }

            yield return new WaitForSeconds(0.2f);

            // 3. Ferrolho puxado e liberado (Slide Rack)
            if (audioSource != null)
            {
                audioSource.PlayOneShot(ProceduralAudio.CreateSlideRackClip());
            }

            if (slideTransform != null)
            {
                slideTransform.localPosition = _slideOriginalLocalPosition;
            }

            _isSlideLocked = false;
            currentAmmo = magazineCapacity;
            OnAmmoChanged?.Invoke(currentAmmo, magazineCapacity);

            yield return new WaitForSeconds(0.15f);
            _isReloading = false;
        }

        private void PlayDryFire()
        {
            if (audioSource != null && dryFireSound != null)
            {
                audioSource.PlayOneShot(dryFireSound);
            }
        }
    }
}
