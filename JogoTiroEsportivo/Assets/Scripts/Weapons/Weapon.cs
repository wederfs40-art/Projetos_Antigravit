using System;
using UnityEngine;

namespace IPSCSimulator.Weapons
{
    using IPSCSimulator.Ballistics;

    /// <summary>
    /// Controla o disparo mecânico, câmara, cano, munição e ferrolho da arma.
    /// Instancia a bala na ponta do cano (Height Over Bore real).
    /// </summary>
    public class Weapon : MonoBehaviour
    {
        [Header("Balística e Muzzle Point")]
        [Tooltip("Ponta do cano onde a bala nasce fisicamente (Height Over Bore).")]
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private GameObject bulletHoleDecalPrefab;
        [Tooltip("Velocidade de saída do cano em metros por segundo.")]
        [SerializeField] private float muzzleVelocity = 380f; // 9mm padrão (360-390 m/s)
        [SerializeField] private float gravityMultiplier = 1f;
        [SerializeField] private LayerMask hitLayers;

        [Header("Capacidade e Ferrolho")]
        [SerializeField] private int magazineCapacity = 17;
        [SerializeField] private int currentAmmo = 17;
        [SerializeField] private Transform slideTransform;
        [SerializeField] private Vector3 slideLockedLocalPosition = new Vector3(0, 0, -0.04f); // Recuo do ferrolho
        private Vector3 _slideOriginalLocalPosition;
        private bool _isSlideLocked = false;

        [Header("Efeitos Sonoros e Visuais")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip fireSound;
        [SerializeField] private AudioClip dryFireSound;
        [SerializeField] private AudioClip slideLockSound;
        [SerializeField] private AudioClip reloadSound;
        [SerializeField] private ParticleSystem muzzleFlash;

        // Eventos públicos
        public static event Action OnWeaponFired;
        public static event Action<int, int> OnAmmoChanged; // (current, max)

        public int CurrentAmmo => currentAmmo;
        public int MagazineCapacity => magazineCapacity;
        public bool IsSlideLocked => _isSlideLocked;

        private void Awake()
        {
            if (slideTransform != null)
                _slideOriginalLocalPosition = slideTransform.localPosition;
        }

        private void Start()
        {
            OnAmmoChanged?.Invoke(currentAmmo, magazineCapacity);
        }

        /// <summary>
        /// Aciona o mecanismo de disparo da arma.
        /// </summary>
        public bool TryFire()
        {
            // Se a arma estiver vazia ou com o ferrolho travado aberto
            if (currentAmmo <= 0 || _isSlideLocked)
            {
                PlayDryFire();
                return false;
            }

            // 1. Consome munição do carregador
            currentAmmo--;
            OnAmmoChanged?.Invoke(currentAmmo, magazineCapacity);

            // 2. Instancia o projétil no cano (Muzzle Point)
            if (bulletPrefab != null && muzzlePoint != null)
            {
                GameObject bulletObj = Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);
                Bullet bulletScript = bulletObj.GetComponent<Bullet>();
                if (bulletScript != null)
                {
                    bulletScript.Initialize(muzzleVelocity, muzzlePoint.forward, gravityMultiplier, bulletHoleDecalPrefab, hitLayers);
                }
            }

            // 3. Efeitos visuais e sonoros
            if (muzzleFlash != null) muzzleFlash.Play();
            if (audioSource != null && fireSound != null) audioSource.PlayOneShot(fireSound);

            // 4. Notifica o sistema de Timer e o manuseio de recuo
            OnWeaponFired?.Invoke();

            // 5. Verifica se foi o último tiro para travar o ferrolho aberto (Slide Lock)
            if (currentAmmo == 0)
            {
                LockSlideOpen();
            }

            return true;
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
        /// Recarrega a arma e destrava o ferrolho.
        /// </summary>
        public void Reload()
        {
            currentAmmo = magazineCapacity;
            _isSlideLocked = false;
            if (slideTransform != null)
            {
                slideTransform.localPosition = _slideOriginalLocalPosition;
            }
            if (audioSource != null && reloadSound != null)
            {
                audioSource.PlayOneShot(reloadSound);
            }
            OnAmmoChanged?.Invoke(currentAmmo, magazineCapacity);
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
