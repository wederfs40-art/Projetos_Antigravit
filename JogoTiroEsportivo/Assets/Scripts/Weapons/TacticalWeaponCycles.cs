using System.Collections;
using UnityEngine;

namespace IPSCSimulator.Weapons
{
    using IPSCSimulator.Utilities;

    /// <summary>
    /// Gerencia os ciclos mecânicos da arma de fogo:
    /// 1. Bolt Cycle (recuo físico do ferrolho e ejeção a cada tiro)
    /// 2. Tactical Reload (troca de carregador com bala na câmara) vs Empty Reload (com slide release)
    /// 3. Weapon Inspect (inspeção detalhada da arma e câmara ao pressionar 'I')
    /// 4. Sincronização de estados de corrida tática e recarga com o Viewmodel
    /// </summary>
    public class TacticalWeaponCycles : MonoBehaviour
    {
        [Header("Componentes Vinculados")]
        [SerializeField] private Weapon weapon;
        [SerializeField] private Transform slideTransform;
        [SerializeField] private Transform magazineTransform;
        [SerializeField] private ProceduralViewmodelMotion motion;

        [Header("Ciclo do Ferrolho (Bolt Cycle)")]
        [SerializeField] private float boltTravelDistance = 0.045f;
        [SerializeField] private float boltReturnSpeed = 38.0f;

        [Header("Inspeção da Arma (Weapon Inspect)")]
        [SerializeField] private KeyCode inspectKey = KeyCode.I;

        private Vector3 _slideOriginalLocalPos;
        private Vector3 _magOriginalLocalPos;
        private bool _isBoltCycling;
        private bool _isInspecting;
        private bool _isReloading;

        public bool IsInspecting => _isInspecting;
        public bool IsReloading => _isReloading;

        private void Awake()
        {
            if (weapon == null) weapon = GetComponent<Weapon>();
            if (motion == null) motion = GetComponent<ProceduralViewmodelMotion>();

            if (slideTransform != null)
                _slideOriginalLocalPos = slideTransform.localPosition;

            if (magazineTransform != null)
                _magOriginalLocalPos = magazineTransform.localPosition;
        }

        private void Start()
        {
            Weapon.OnWeaponFired += OnFiredTriggerBolt;
        }

        private void OnDestroy()
        {
            Weapon.OnWeaponFired -= OnFiredTriggerBolt;
        }

        private void Update()
        {
            // Atalho para inspecionar a arma (Tecla I)
            if (Input.GetKeyDown(inspectKey) && !_isInspecting && !_isReloading && (motion == null || !motion.IsAiming))
            {
                StartCoroutine(WeaponInspectCoroutine());
            }

            // Atalho de recarga rápida (Tecla R)
            if (Input.GetKeyDown(KeyCode.R) && !_isReloading && !_isInspecting)
            {
                if (weapon != null && weapon.CurrentAmmo < weapon.MagazineCapacity)
                {
                    StartCoroutine(TacticalReloadCoroutine());
                }
            }
        }

        /// <summary>
        /// Acionado a cada disparo para ciclar instantaneamente o ferrolho da arma.
        /// </summary>
        private void OnFiredTriggerBolt()
        {
            if (slideTransform == null) return;
            if (_isBoltCycling) StopCoroutine("BoltCycleCoroutine");
            StartCoroutine(BoltCycleCoroutine());
        }

        private IEnumerator BoltCycleCoroutine()
        {
            _isBoltCycling = true;
            Vector3 backPos = _slideOriginalLocalPos - Vector3.forward * boltTravelDistance;

            // Recuo instantâneo do ferrolho
            slideTransform.localPosition = backPos;
            yield return new WaitForSeconds(0.025f);

            // Se for o último tiro, o ferrolho trava aberto no retém (Slide Lock)
            if (weapon != null && weapon.CurrentAmmo == 0)
            {
                slideTransform.localPosition = backPos;
                _isBoltCycling = false;
                yield break;
            }

            // Retorno elástico suave à frente pela mola recuperadora
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * boltReturnSpeed;
                slideTransform.localPosition = Vector3.Lerp(backPos, _slideOriginalLocalPos, t);
                yield return null;
            }

            slideTransform.localPosition = _slideOriginalLocalPos;
            _isBoltCycling = false;
        }

        /// <summary>
        /// Animação de inspeção tática em primeira pessoa: gira a arma para ver o ferrolho,
        /// a câmara de ejeção e a parte inferior do carregador.
        /// </summary>
        private IEnumerator WeaponInspectCoroutine()
        {
            _isInspecting = true;
            if (motion != null) motion.SetInspecting(true);

            Vector3 startPos = transform.localPosition;
            Quaternion startRot = transform.localRotation;

            // Fase 1: inclina a arma para a esquerda mostrando o ferrolho e câmara
            Vector3 inspectPos1 = startPos + new Vector3(-0.06f, 0.02f, -0.04f);
            Quaternion inspectRot1 = Quaternion.Euler(-8f, -28f, 38f);

            float elapsed = 0f;
            float duration = 0.55f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float p = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                transform.localPosition = Vector3.Lerp(startPos, inspectPos1, p);
                transform.localRotation = Quaternion.Slerp(startRot, inspectRot1, p);
                yield return null;
            }

            yield return new WaitForSeconds(0.65f); // Pausa para admirar os detalhes da câmara

            // Fase 2: gira suavemente para a direita mostrando o outro lado e o magazine
            Vector3 inspectPos2 = startPos + new Vector3(0.04f, -0.02f, 0.01f);
            Quaternion inspectRot2 = Quaternion.Euler(12f, 22f, -24f);

            elapsed = 0f;
            duration = 0.60f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float p = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                transform.localPosition = Vector3.Lerp(inspectPos1, inspectPos2, p);
                transform.localRotation = Quaternion.Slerp(inspectRot1, inspectRot2, p);
                yield return null;
            }

            yield return new WaitForSeconds(0.45f);

            // Fase 3: retorna à empunhadura original de tiro
            elapsed = 0f;
            duration = 0.50f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float p = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                transform.localPosition = Vector3.Lerp(inspectPos2, startPos, p);
                transform.localRotation = Quaternion.Slerp(inspectRot2, startRot, p);
                yield return null;
            }

            transform.localPosition = startPos;
            transform.localRotation = startRot;

            _isInspecting = false;
            if (motion != null) motion.SetInspecting(false);
        }

        /// <summary>
        /// Realiza a recarga sincronizada: detecta se a câmara está vazia (com slide lock)
        /// ou se é uma recarga tática rápida com bala na câmara.
        /// </summary>
        private IEnumerator TacticalReloadCoroutine()
        {
            _isReloading = true;
            bool wasEmpty = (weapon != null && weapon.CurrentAmmo == 0);

            // 1. Desce a arma levemente e inclina para troca de pente
            Vector3 originPos = transform.localPosition;
            Quaternion originRot = transform.localRotation;
            Vector3 reloadStancePos = originPos + new Vector3(-0.02f, -0.06f, -0.03f);
            Quaternion reloadStanceRot = Quaternion.Euler(15f, -12f, 18f);

            float t = 0f;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                transform.localPosition = Vector3.Lerp(originPos, reloadStancePos, t / 0.25f);
                transform.localRotation = Quaternion.Slerp(originRot, reloadStanceRot, t / 0.25f);
                yield return null;
            }

            // 2. Ejeção do magazine para baixo
            if (magazineTransform != null)
            {
                Vector3 droppedMagPos = _magOriginalLocalPos - Vector3.up * 0.18f;
                AudioSource.PlayClipAtPoint(ProceduralAudio.CreateMagOutClip(), transform.position, 0.8f);

                t = 0f;
                while (t < 0.28f)
                {
                    t += Time.deltaTime;
                    magazineTransform.localPosition = Vector3.Lerp(_magOriginalLocalPos, droppedMagPos, t / 0.28f);
                    yield return null;
                }

                yield return new WaitForSeconds(0.20f); // Tempo de buscar novo carregador no colete

                // 3. Inserção do novo magazine com clique firme
                t = 0f;
                while (t < 0.22f)
                {
                    t += Time.deltaTime;
                    magazineTransform.localPosition = Vector3.Lerp(droppedMagPos, _magOriginalLocalPos, t / 0.22f);
                    yield return null;
                }
                magazineTransform.localPosition = _magOriginalLocalPos;
                AudioSource.PlayClipAtPoint(ProceduralAudio.CreateMagInClip(), transform.position, 0.9f);
            }

            yield return new WaitForSeconds(0.12f);

            // 4. Se a arma estava vazia, cicla o ferrolho (Slide Rack / Bolt Release)
            if (wasEmpty && slideTransform != null)
            {
                AudioSource.PlayClipAtPoint(ProceduralAudio.CreateSlideRackClip(), transform.position, 0.95f);
                Vector3 backPos = _slideOriginalLocalPos - Vector3.forward * boltTravelDistance;

                t = 0f;
                while (t < 0.15f)
                {
                    t += Time.deltaTime;
                    slideTransform.localPosition = Vector3.Lerp(backPos, _slideOriginalLocalPos, t / 0.15f);
                    yield return null;
                }
                slideTransform.localPosition = _slideOriginalLocalPos;
            }

            // 5. Retorna à empunhadura tática
            t = 0f;
            while (t < 0.30f)
            {
                t += Time.deltaTime;
                transform.localPosition = Vector3.Lerp(reloadStancePos, originPos, t / 0.30f);
                transform.localRotation = Quaternion.Slerp(reloadStanceRot, originRot, t / 0.30f);
                yield return null;
            }

            transform.localPosition = originPos;
            transform.localRotation = originRot;

            if (weapon != null)
            {
                weapon.ConfigureWeapon(weapon.MagazineCapacity, 380f);
            }

            _isReloading = false;
        }

        public void BindTransforms(Weapon w, Transform slide, Transform mag, ProceduralViewmodelMotion m)
        {
            weapon = w;
            slideTransform = slide;
            magazineTransform = mag;
            motion = m;

            if (slideTransform != null) _slideOriginalLocalPos = slideTransform.localPosition;
            if (magazineTransform != null) _magOriginalLocalPos = magazineTransform.localPosition;
        }
    }
}
