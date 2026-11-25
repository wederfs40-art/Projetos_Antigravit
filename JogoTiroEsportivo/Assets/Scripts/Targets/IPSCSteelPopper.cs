using System.Collections;
using UnityEngine;

namespace IPSCSimulator.Targets
{
    using IPSCSimulator.Match;
    using IPSCSimulator.Utilities;

    /// <summary>
    /// Alvo metálico reativo (Pepper Popper / Steel Plate) regulamentar IPSC.
    /// Quando atingido por um projétil, emite o estalido metálico característico ("PING!"),
    /// tomba para trás por gravidade e contabiliza 5 pontos (Zona Alpha).
    /// Pode ser reerguido automaticamente junto com o obreamento dos alvos de papel (tecla U).
    /// </summary>
    public class IPSCSteelPopper : MonoBehaviour
    {
        [SerializeField] private string targetName = "Steel Popper";
        [SerializeField] private Transform hingeTransform;
        [SerializeField] private AudioSource audioSource;

        private bool _isDown = false;
        private Quaternion _uprightRotation;
        private Quaternion _fallenRotation;
        private Coroutine _fallCoroutine;

        public bool IsDown => _isDown;
        public string TargetName => targetName;

        private void Awake()
        {
            if (hingeTransform == null) hingeTransform = transform;
            _uprightRotation = hingeTransform.localRotation;
            _fallenRotation = _uprightRotation * Quaternion.Euler(85f, 0f, 0f);

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1.0f; // Áudio 3D posicional no estande
                audioSource.minDistance = 2f;
                audioSource.maxDistance = 50f;
            }
        }

        /// <summary>
        /// Processa o impacto de um projétil de arma de fogo.
        /// </summary>
        public void OnHit(Vector3 hitPoint, Vector3 hitNormal)
        {
            if (_isDown) return;

            _isDown = true;

            // 1. Som metálico estridente de chapa de aço
            if (audioSource != null)
            {
                audioSource.PlayOneShot(ProceduralAudio.CreateSteelPingClip());
            }

            // 2. Pontuação no timer (Alvo metálico abatido equivale a 5 pontos)
            if (RangeOfficerTimer.Instance != null)
            {
                RangeOfficerTimer.Instance.AddScore(5);
            }

            // 3. Animação física de queda para trás
            if (_fallCoroutine != null) StopCoroutine(_fallCoroutine);
            _fallCoroutine = StartCoroutine(AnimateFall());

            Debug.Log($"[IPSC Steel] {targetName} abatido com sucesso! +5 pontos");
        }

        private IEnumerator AnimateFall()
        {
            float elapsed = 0f;
            float duration = 0.35f;
            Quaternion startRot = hingeTransform.localRotation;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Curva de aceleração gravitacional quadrática
                float ease = t * t;
                hingeTransform.localRotation = Quaternion.Slerp(startRot, _fallenRotation, ease);
                yield return null;
            }

            hingeTransform.localRotation = _fallenRotation;
        }

        /// <summary>
        /// Reergue o alvo metálico para a posição vertical de tiro.
        /// </summary>
        public void ResetPopper()
        {
            if (_fallCoroutine != null) StopCoroutine(_fallCoroutine);
            _isDown = false;
            hingeTransform.localRotation = _uprightRotation;
        }
    }
}
