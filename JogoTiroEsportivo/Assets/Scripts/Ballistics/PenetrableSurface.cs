using UnityEngine;

namespace IPSCSimulator.Ballistics
{
    /// <summary>
    /// Tipos comuns de materiais de superfícies para cálculo de penetração balística.
    /// </summary>
    public enum SurfaceMaterialType
    {
        PapelaoAlvoIPSC,       // Quase zero resistência (alvos de papelão)
        MadeiraCompensado,     // Compensado/OSB/Madeira leve (baixa resistência, fácil de varar)
        DrywallGesso,          // Paredes divisórias de gesso acartonado
        MadeiraMaciça,         // Portas maciças, vigas estruturais
        ChapaMetalicaLeve,     // Latarias, chapas finas, contêineres industriais
        ConcretoAlvenaria,     // Tijolos, concreto, alvenaria rígida (difícil de varar)
        AcoBlindadoImpenetravel // Aço temperado de poppers, trilhos pesados (impenetrável)
    }

    /// <summary>
    /// Componente anexado a GameObjects do cenário para definir se e como
    /// projéteis de armas conseguem atravessar (varar) o obstáculo.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PenetrableSurface : MonoBehaviour
    {
        [Header("Classificação do Material")]
        [Tooltip("Material que compõe este objeto no cenário.")]
        [SerializeField] private SurfaceMaterialType materialType = SurfaceMaterialType.MadeiraCompensado;

        [Header("Resistência Balística")]
        [Tooltip("Poder mínimo de penetração exigido da arma para conseguir varar esta superfície (0 a 100).")]
        [Range(0f, 100f)]
        [SerializeField] private float penetrationResistance = 25f;

        [Tooltip("Porcentagem de redução de dano aplicada à bala após atravessar (0.1 = 10% a menos, 0.7 = 70% a menos).")]
        [Range(0f, 1f)]
        [SerializeField] private float damageReductionPercentage = 0.35f;

        [Header("Espessura e Geometria")]
        [Tooltip("Tenta detectar a espessura física real disparando um raycast reverso pela face traseira do collider.")]
        [SerializeField] private bool autoDetectThickness = true;

        [Tooltip("Espessura padrão estimada em metros caso a detecção por raycast reverso falhe ou o collider seja plano.")]
        [SerializeField] private float defaultThickness = 0.08f;

        [Header("Decalque de Saída")]
        [Tooltip("Prefab opcional do buraco de saída da bala na face traseira do obstáculo.")]
        [SerializeField] private GameObject exitDecalPrefab;

        private Collider _collider;

        public SurfaceMaterialType MaterialType => materialType;
        public float PenetrationResistance => penetrationResistance;
        public float DamageReductionPercentage => damageReductionPercentage;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            ApplyPresetDefaults();
        }

        private void Reset()
        {
            _collider = GetComponent<Collider>();
            ApplyPresetDefaults();
        }

        /// <summary>
        /// Aplica configurações automáticas recomendadas baseadas no tipo de material selecionado.
        /// </summary>
        public void ApplyPresetDefaults()
        {
            switch (materialType)
            {
                case SurfaceMaterialType.PapelaoAlvoIPSC:
                    penetrationResistance = 2f;
                    damageReductionPercentage = 0.02f;
                    defaultThickness = 0.01f;
                    break;

                case SurfaceMaterialType.MadeiraCompensado:
                    penetrationResistance = 18f;
                    damageReductionPercentage = 0.20f;
                    defaultThickness = 0.05f;
                    break;

                case SurfaceMaterialType.DrywallGesso:
                    penetrationResistance = 14f;
                    damageReductionPercentage = 0.15f;
                    defaultThickness = 0.08f;
                    break;

                case SurfaceMaterialType.MadeiraMaciça:
                    penetrationResistance = 42f;
                    damageReductionPercentage = 0.40f;
                    defaultThickness = 0.12f;
                    break;

                case SurfaceMaterialType.ChapaMetalicaLeve:
                    penetrationResistance = 55f;
                    damageReductionPercentage = 0.50f;
                    defaultThickness = 0.03f;
                    break;

                case SurfaceMaterialType.ConcretoAlvenaria:
                    penetrationResistance = 85f;
                    damageReductionPercentage = 0.75f;
                    defaultThickness = 0.25f;
                    break;

                case SurfaceMaterialType.AcoBlindadoImpenetravel:
                    penetrationResistance = 9999f;
                    damageReductionPercentage = 1f;
                    defaultThickness = 0.50f;
                    break;
            }
        }

        /// <summary>
        /// Avalia se um projétil consegue atravessar esta superfície física.
        /// Caso penetre, calcula o ponto de saída, a espessura atravessada,
        /// o poder residual de penetração e o dano amortecido.
        /// </summary>
        public bool EvaluatePenetration(
            Vector3 hitPoint,
            Vector3 hitDirection,
            float incomingPower,
            float incomingDamage,
            out Vector3 exitPoint,
            out float remainingPower,
            out float remainingDamage)
        {
            exitPoint = hitPoint;
            remainingPower = 0f;
            remainingDamage = 0f;

            // Se for aço impenetrável ou o projétil não tiver força mínima, para imediatamente
            if (materialType == SurfaceMaterialType.AcoBlindadoImpenetravel || incomingPower <= 0f)
            {
                return false;
            }

            // 1. Determina a espessura e o ponto de saída físico do projétil
            float thickness = defaultThickness;
            bool foundExit = false;

            if (autoDetectThickness && _collider != null)
            {
                // Dispara um raycast invertido a partir do outro lado do objeto em direção ao ponto de entrada
                float maxCheckDist = Mathf.Max(0.5f, defaultThickness * 3f);
                Vector3 probeOrigin = hitPoint + (hitDirection.normalized * maxCheckDist);
                Ray reverseRay = new Ray(probeOrigin, -hitDirection.normalized);

                if (_collider.Raycast(reverseRay, out RaycastHit exitHit, maxCheckDist + 0.05f))
                {
                    exitPoint = exitHit.point;
                    thickness = Vector3.Distance(hitPoint, exitPoint);
                    foundExit = true;

                    // Opcional: Instancia decalque de saída com diâmetro um pouco maior (orifício de saída real)
                    if (exitDecalPrefab != null)
                    {
                        Vector3 decalPos = exitPoint + (exitHit.normal * 0.001f);
                        Quaternion decalRot = Quaternion.LookRotation(exitHit.normal);
                        GameObject decal = Instantiate(exitDecalPrefab, decalPos, decalRot, transform);
                        decal.SetActive(true);
                    }
                }
            }

            if (!foundExit)
            {
                exitPoint = hitPoint + (hitDirection.normalized * defaultThickness);
                thickness = defaultThickness;
            }

            // 2. Calcula o consumo de penetração proporcional à espessura física atravessada
            float effectiveResistance = penetrationResistance * Mathf.Max(0.5f, thickness * 10f);

            // Se a força de penetração for insuficiente para transpassar
            if (incomingPower < effectiveResistance)
            {
                return false;
            }

            // 3. Projétil atravessou com sucesso: amortece o poder e o dano proporcionalmente
            remainingPower = Mathf.Max(0f, incomingPower - effectiveResistance);
            float damageFactor = Mathf.Clamp01(1f - (damageReductionPercentage * Mathf.Max(0.8f, thickness * 8f)));
            remainingDamage = incomingDamage * damageFactor;

            return true;
        }
    }
}
