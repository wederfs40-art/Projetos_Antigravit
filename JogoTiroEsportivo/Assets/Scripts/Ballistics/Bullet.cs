using UnityEngine;

namespace IPSCSimulator.Ballistics
{
    using IPSCSimulator.Targets;
    using IPSCSimulator.Utilities;

    /// <summary>
    /// Controla o comportamento físico e balístico contínuo de cada projétil instanciado.
    /// Simula gravidade real, velocidade de saída do cano, transferência de força cinética
    /// e colisão contínua via Raycast anti-tunneling.
    /// </summary>
    public class Bullet : MonoBehaviour
    {
        private Vector3 _velocity;
        private Vector3 _previousPosition;
        private float _gravityMultiplier = 1f;
        private float _maxLifetime = 5f;
        private float _currentLifetime = 0f;
        private GameObject _decalPrefab;
        private LayerMask _hitLayers;

        /// <summary>
        /// Inicializa os parâmetros balísticos da bala no momento do disparo.
        /// </summary>
        public void Initialize(float muzzleVelocity, Vector3 direction, float gravityMultiplier, GameObject decalPrefab, LayerMask hitLayers)
        {
            _velocity = direction.normalized * muzzleVelocity;
            _previousPosition = transform.position;
            _gravityMultiplier = gravityMultiplier;
            _decalPrefab = decalPrefab;
            _hitLayers = (hitLayers.value == 0) ? (LayerMask)(~0) : hitLayers;
        }

        private void Update()
        {
            _currentLifetime += Time.deltaTime;
            if (_currentLifetime >= _maxLifetime)
            {
                Destroy(gameObject);
                return;
            }

            // 1. Aplica a aceleração da gravidade na velocidade atual (queda balística real)
            _velocity += Physics.gravity * _gravityMultiplier * Time.deltaTime;

            // 2. Calcula o vetor de deslocamento deste frame
            Vector3 displacement = _velocity * Time.deltaTime;
            Vector3 newPosition = _previousPosition + displacement;
            float stepDistance = displacement.magnitude;
            Vector3 stepDirection = displacement.normalized;

            // 3. Detecção contínua por Raycast entre a posição anterior e a nova (anti-tunneling)
            if (Physics.Raycast(_previousPosition, stepDirection, out RaycastHit hit, stepDistance, _hitLayers, QueryTriggerInteraction.Collide))
            {
                transform.position = hit.point;
                ProcessDirectHit(hit, stepDirection, _decalPrefab);
                Destroy(gameObject);
                return;
            }

            // 4. Sem colisão: atualiza a posição do objeto no espaço
            transform.position = newPosition;
            _previousPosition = newPosition;
        }

        /// <summary>
        /// Processa o impacto direto de um projétil contra qualquer superfície ou alvo.
        /// Aplica força física cinética (impulso de projétil), sons de impacto de material e furos visíveis.
        /// </summary>
        public static void ProcessDirectHit(RaycastHit hit, Vector3 bulletDirection, GameObject decalPrefab)
        {
            if (hit.collider == null) return;

            // 1. Transferência de energia cinética: empurra objetos com Rigidbody (barris, caixas, etc)
            Rigidbody hitRb = hit.collider.attachedRigidbody;
            if (hitRb != null && !hitRb.isKinematic)
            {
                hitRb.AddForceAtPosition(bulletDirection.normalized * 380f, hit.point, ForceMode.Impulse);
            }

            // 2. Processa colisão com alvos de papelão IPSC
            IPSCTargetZone targetZone = hit.collider.GetComponent<IPSCTargetZone>();
            if (targetZone != null)
            {
                targetZone.RegisterHit(hit.point, hit.normal, decalPrefab);
                return;
            }

            // 3. Processa colisão com alvos metálicos reativos (Steel Poppers)
            IPSCSteelPopper steelPopper = hit.collider.GetComponentInParent<IPSCSteelPopper>();
            if (steelPopper != null)
            {
                steelPopper.OnHit(hit.point, hit.normal);
                return;
            }

            // 4. Superfícies de cenário (paredes, madeira, chão, barris, metal)
            if (decalPrefab != null)
            {
                Vector3 spawnPos = hit.point + (hit.normal * 0.001f);
                Quaternion spawnRot = Quaternion.LookRotation(-hit.normal);
                GameObject decal = Instantiate(decalPrefab, spawnPos, spawnRot);
                decal.SetActive(true); // Garante visibilidade
                decal.transform.SetParent(hit.collider.transform);
            }

            // 5. Sons de impacto contextualizados por tipo de material
            string objName = hit.collider.gameObject.name.ToLower();
            AudioClip impactClip = null;

            if (objName.Contains("barrel") || objName.Contains("steel") || objName.Contains("metal") || objName.Contains("truss"))
            {
                impactClip = ProceduralAudio.CreateMetalImpactClip();
            }
            else if (objName.Contains("wood") || objName.Contains("stud") || objName.Contains("osb") || objName.Contains("bench"))
            {
                impactClip = ProceduralAudio.CreateWoodImpactClip();
            }
            else
            {
                impactClip = ProceduralAudio.CreateConcreteImpactClip();
            }

            if (impactClip != null)
            {
                AudioSource.PlayClipAtPoint(impactClip, hit.point, 0.85f);
            }

            // 6. Clarão/faísca de impacto momentâneo no ponto de colisão
            SpawnImpactSparks(hit.point, hit.normal);
        }

        private static void SpawnImpactSparks(Vector3 point, Vector3 normal)
        {
            GameObject sparkObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sparkObj.name = "Impact_Spark";
            sparkObj.transform.position = point + normal * 0.02f;
            sparkObj.transform.localScale = Vector3.one * 0.04f;

            Material sparkMat = ProceduralMaterials.CreateEmissiveMaterial(new Color(1f, 0.85f, 0.35f), "Mat_SparkGlow", 4.0f);
            sparkObj.GetComponent<MeshRenderer>().material = sparkMat;
            Destroy(sparkObj.GetComponent<Collider>());
            Destroy(sparkObj, 0.045f);
        }
    }
}
