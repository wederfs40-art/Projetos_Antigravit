using UnityEngine;

namespace IPSCSimulator.Targets
{
    public enum IPSCZoneType
    {
        Alpha,
        Charlie,
        Delta,
        NoShoot
    }

    /// <summary>
    /// Representa uma subzona de impacto do alvo (Alpha, Charlie, Delta ou No-Shoot).
    /// </summary>
    public class IPSCTargetZone : MonoBehaviour
    {
        [SerializeField] private IPSCZoneType zoneType;
        [SerializeField] private IPSCTarget parentTarget;

        public IPSCZoneType ZoneType => zoneType;

        private void Awake()
        {
            if (parentTarget == null)
            {
                parentTarget = GetComponentInParent<IPSCTarget>();
            }
        }

        public void RegisterHit(Vector3 hitPoint, Vector3 hitNormal, GameObject decalPrefab)
        {
            if (parentTarget != null)
            {
                parentTarget.OnZoneHit(zoneType, hitPoint, hitNormal, decalPrefab);
            }
        }
    }
}
