using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IPSCSimulator.Targets
{
    using IPSCSimulator.Match;
    using IPSCSimulator.Utilities;

    /// <summary>
    /// Controla o alvo de papel IPSC como um todo, seus decalques, vibração de impacto e histórico de impactos.
    /// Suporta obreamento para resetar furos de bala e histórico de pontuação.
    /// </summary>
    public class IPSCTarget : MonoBehaviour
    {
        [SerializeField] private string targetName = "Target A1";
        [SerializeField] private bool isNoShoot = false;

        private readonly List<GameObject> _spawnedDecals = new List<GameObject>();
        private readonly List<IPSCZoneType> _hitsOnTarget = new List<IPSCZoneType>();
        private AudioSource _audioSource;
        private Quaternion _originalRotation;
        private Coroutine _wiggleCoroutine;

        public static event Action<string, IPSCZoneType, bool> OnTargetZoneHitGlobal;

        public IReadOnlyList<IPSCZoneType> Hits => _hitsOnTarget;
        public string TargetName => targetName;
        public bool IsNoShoot => isNoShoot;

        private void Awake()
        {
            _originalRotation = transform.localRotation;
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1.0f;
            _audioSource.minDistance = 2f;
            _audioSource.maxDistance = 45f;
        }

        /// <summary>
        /// Processa o impacto recebido por uma de suas zonas.
        /// </summary>
        public void OnZoneHit(IPSCZoneType zone, Vector3 hitPoint, Vector3 hitNormal, GameObject decalPrefab)
        {
            _hitsOnTarget.Add(zone);

            // 1. Instancia o furo da bala exatamente no ponto e normal de impacto
            if (decalPrefab != null)
            {
                Vector3 spawnPos = hitPoint + (hitNormal * 0.001f);
                Quaternion spawnRot = Quaternion.LookRotation(-hitNormal);

                GameObject decal = Instantiate(decalPrefab, spawnPos, spawnRot);
                decal.SetActive(true); // Garante que o furo fique visível
                decal.transform.SetParent(transform); // O furo acompanha o alvo caso seja móvel
                _spawnedDecals.Add(decal);
            }

            // 2. Som de impacto perfurando o papelão
            if (_audioSource != null)
            {
                _audioSource.PlayOneShot(ProceduralAudio.CreateCardboardImpactClip());
            }

            // 3. Efeito físico de vibração / impacto da bala no suporte de papelão
            if (_wiggleCoroutine != null) StopCoroutine(_wiggleCoroutine);
            _wiggleCoroutine = StartCoroutine(ImpactWiggleCoroutine(hitNormal));

            // 4. Notifica o Range Officer Timer da pista
            if (RangeOfficerTimer.Instance != null)
            {
                RangeOfficerTimer.Instance.RegisterTargetHit(this, zone);
            }

            // 5. Notifica globalmente para o HUD exibir feedback visual imediato
            OnTargetZoneHitGlobal?.Invoke(targetName, zone, isNoShoot);

            Debug.Log($"[IPSC] {targetName} atingido na zona: {zone}!");
        }

        private IEnumerator ImpactWiggleCoroutine(Vector3 hitNormal)
        {
            float duration = 0.22f;
            float elapsed = 0f;
            Vector3 recoilAxis = Vector3.Cross(hitNormal, Vector3.up).normalized;
            if (recoilAxis == Vector3.zero) recoilAxis = Vector3.right;

            Quaternion kickRot = _originalRotation * Quaternion.AngleAxis(3.5f, recoilAxis);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float damp = Mathf.Sin(progress * Mathf.PI * 3f) * (1f - progress);
                transform.localRotation = Quaternion.Slerp(_originalRotation, kickRot, damp);
                yield return null;
            }

            transform.localRotation = _originalRotation;
        }

        /// <summary>
        /// Obreia o alvo: destrói os decalques de furo de bala e reseta a contagem de impactos.
        /// </summary>
        public void ObrearAlvo()
        {
            foreach (GameObject decal in _spawnedDecals)
            {
                if (decal != null)
                {
                    Destroy(decal);
                }
            }

            _spawnedDecals.Clear();
            _hitsOnTarget.Clear();
            Debug.Log($"[IPSC] Alvo {targetName} obreado e pronto para nova série.");
        }
    }
}
