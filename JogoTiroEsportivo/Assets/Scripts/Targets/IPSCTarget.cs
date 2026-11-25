using System.Collections.Generic;
using UnityEngine;

namespace IPSCSimulator.Targets
{
    using IPSCSimulator.Match;

    /// <summary>
    /// Controla o alvo de papel IPSC como um todo, seus decalques e histórico de impactos.
    /// Suporta obreamento para resetar furos de bala e histórico de pontuação.
    /// </summary>
    public class IPSCTarget : MonoBehaviour
    {
        [SerializeField] private string targetName = "Target A1";
        [SerializeField] private bool isNoShoot = false;

        private readonly List<GameObject> _spawnedDecals = new List<GameObject>();
        private readonly List<IPSCZoneType> _hitsOnTarget = new List<IPSCZoneType>();

        public IReadOnlyList<IPSCZoneType> Hits => _hitsOnTarget;
        public string TargetName => targetName;
        public bool IsNoShoot => isNoShoot;

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
                decal.transform.SetParent(transform); // O furo acompanha o alvo caso seja móvel
                _spawnedDecals.Add(decal);
            }

            // 2. Notifica o Range Officer Timer da pista
            if (RangeOfficerTimer.Instance != null)
            {
                RangeOfficerTimer.Instance.RegisterTargetHit(this, zone);
            }

            Debug.Log($"[IPSC] {targetName} atingido na zona: {zone}!");
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
