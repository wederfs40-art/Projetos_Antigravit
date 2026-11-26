using System.Collections;
using UnityEngine;

namespace IPSCSimulator.Weapons
{
    using IPSCSimulator.Ballistics;
    using IPSCSimulator.Targets;
    using IPSCSimulator.Utilities;

    /// <summary>
    /// Gerenciador balístico completo para armas em primeira pessoa.
    /// Controla dispersão dinâmica estilo Counter-Strike baseada no movimento do jogador,
    /// disparo híbrido (Hitscan instantâneo vs Projétil Físico com queda balística)
    /// e penetração realista através de superfícies sólidas (PenetrableSurface).
    /// </summary>
    public class WeaponBallistics : MonoBehaviour
    {
        [Header("Dados do Armamento")]
        [Tooltip("ScriptableObject com as configurações balísticas, de recuo e visada.")]
        [SerializeField] private WeaponData weaponData;

        [Header("Origem e Referências")]
        [Tooltip("Boca do cano (Muzzle Point) de onde o projétil/traçante sai fisicamente.")]
        [SerializeField] private Transform muzzlePoint;

        [Tooltip("Câmera principal em primeira pessoa do jogador.")]
        [SerializeField] private Camera playerCamera;

        [Tooltip("Componente de movimento para leitura de velocidade dinâmica (CharacterController ou Rigidbody).")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Rigidbody playerRigidbody;

        [Header("Decalques e Camadas")]
        [Tooltip("Prefab do orifício de impacto de bala (Decal).")]
        [SerializeField] private GameObject bulletDecalPrefab;

        [Tooltip("Camadas de colisão atingíveis pelos disparos.")]
        [SerializeField] private LayerMask hitLayers = ~0;

        [Header("Configurações Avançadas")]
        [Tooltip("Distância máxima de alcance efetivo do disparo.")]
        [SerializeField] private float maxDistance = 350f;

        [Tooltip("Número máximo de superfícies que um único disparo pode atravessar em sequência.")]
        [SerializeField] private int maxPenetrationLayers = 4;

        // Estado interno de mira ADS
        private bool _isAiming = false;

        public WeaponData CurrentWeaponData
        {
            get => weaponData;
            set => weaponData = value;
        }

        public bool IsAiming
        {
            get => _isAiming;
            set => _isAiming = value;
        }

        private void Awake()
        {
            // Auto-detecta componentes essenciais caso não tenham sido atribuídos no Inspector
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            if (characterController == null)
            {
                characterController = GetComponentInParent<CharacterController>();
            }

            if (playerRigidbody == null)
            {
                playerRigidbody = GetComponentInParent<Rigidbody>();
            }

            if (muzzlePoint == null)
            {
                muzzlePoint = transform;
            }
        }

        private void Update()
        {
            // Atualiza o estado de mira pelo botão direito do mouse caso nenhum script externo esteja controlando
            if (Input.GetMouseButtonDown(1)) _isAiming = true;
            if (Input.GetMouseButtonUp(1)) _isAiming = false;
        }

        /// <summary>
        /// Executa o disparo balístico completo da arma atual.
        /// </summary>
        public void Shoot()
        {
            if (weaponData == null)
            {
                Debug.LogWarning("[WeaponBallistics] Nenhum WeaponData atribuído para efetuar o disparo!");
                return;
            }

            // 1. Determina a origem e a direção base da visada da câmera
            Vector3 rayOrigin = (playerCamera != null) ? playerCamera.transform.position : muzzlePoint.position;
            Vector3 baseDirection = (playerCamera != null) ? playerCamera.transform.forward : muzzlePoint.forward;

            // 2. Calcula a dispersão (Spread) dinâmica estilo CS baseada na velocidade do operador
            Vector3 spreadDirection = CalculateSpreadDirection(baseDirection);

            // 3. Executa o disparo no modo configurado no ScriptableObject
            if (weaponData.BallisticMode == BallisticMode.Hitscan)
            {
                ExecuteHitscanShot(rayOrigin, spreadDirection);
            }
            else
            {
                StartCoroutine(SimulatePhysicalProjectile(rayOrigin, spreadDirection));
            }
        }

        // ========================================================================================
        // 1. CÁLCULO DE DISPERSÃO (SPREAD ESTILO COUNTER-STRIKE)
        // ========================================================================================
        /// <summary>
        /// Calcula a dispersão angular do projétil baseada na velocidade de movimento do jogador.
        /// Se o jogador estiver parado e em mira ADS, a dispersão é quase nula (100% precisa).
        /// Ao correr ou saltar, a dispersão cresce drasticamente.
        /// </summary>
        public Vector3 CalculateSpreadDirection(Vector3 baseDirection)
        {
            // Lê a velocidade escalar atual do jogador
            float currentSpeed = 0f;
            if (characterController != null)
            {
                currentSpeed = characterController.velocity.magnitude;
            }
            else if (playerRigidbody != null)
            {
                currentSpeed = playerRigidbody.linearVelocity.magnitude;
            }

            // Multiplicador de precisão:
            // - Parado em ADS: precisão cirúrgica (spread reduzido a 5%)
            // - Parado no quadril (Hipfire): spread base
            // - Em movimento: spread base amplificado pela velocidade e pelo multiplicador de movimento
            float spreadMultiplier;

            if (_isAiming)
            {
                // Em mira ADS: se estiver totalmente parado, precisão de quase 100%
                float stillFactor = Mathf.Clamp01(currentSpeed / 2f);
                spreadMultiplier = Mathf.Lerp(0.04f, 0.45f, stillFactor);
            }
            else
            {
                // Disparo instintivo no quadril (Hipfire estilo CS)
                spreadMultiplier = 1f + (currentSpeed * weaponData.MovementSpreadMultiplier);
            }

            float finalSpreadAngle = weaponData.BaseSpread * spreadMultiplier;

            // Gera desvio angular radial aleatório
            Vector2 randomCircle = Random.insideUnitCircle * finalSpreadAngle;
            Quaternion spreadRotation = Quaternion.Euler(randomCircle.y, randomCircle.x, 0f);

            return (playerCamera != null)
                ? playerCamera.transform.rotation * spreadRotation * Vector3.forward
                : spreadRotation * baseDirection;
        }


        // ========================================================================================
        // 2. MODO HITSCAN COM PENETRAÇÃO SUCESSIVA
        // ========================================================================================
        private void ExecuteHitscanShot(Vector3 origin, Vector3 direction)
        {
            Vector3 currentOrigin = origin;
            Vector3 currentDirection = direction.normalized;
            float currentPower = weaponData.PenetrationPower;
            float currentDamage = weaponData.DamageStat;
            float remainingDistance = maxDistance;

            for (int layer = 0; layer < maxPenetrationLayers; layer++)
            {
                if (Physics.Raycast(currentOrigin, currentDirection, out RaycastHit hit, remainingDistance, hitLayers, QueryTriggerInteraction.Collide))
                {
                    // Ignora colisão com o próprio jogador ou com a arma
                    if (IsPlayerOrWeapon(hit.collider))
                    {
                        currentOrigin = hit.point + (currentDirection * 0.05f);
                        remainingDistance -= hit.distance;
                        continue;
                    }

                    // Processa o impacto direto na superfície
                    ProcessSurfaceHit(hit, currentDirection, currentDamage);

                    // Verifica se a superfície atingida possui propriedades de penetração
                    PenetrableSurface penetrable = hit.collider.GetComponent<PenetrableSurface>() ?? hit.collider.GetComponentInParent<PenetrableSurface>();

                    if (penetrable != null && penetrable.EvaluatePenetration(
                        hit.point,
                        currentDirection,
                        currentPower,
                        currentDamage,
                        out Vector3 exitPoint,
                        out float remainingPower,
                        out float remainingDamage))
                    {
                        // Projétil varou a superfície com sucesso:
                        // Atualiza a origem para o ponto de saída físico com pequeno offset
                        currentOrigin = exitPoint + (currentDirection * 0.02f);
                        remainingDistance -= (hit.distance + Vector3.Distance(hit.point, exitPoint));
                        currentPower = remainingPower;
                        currentDamage = remainingDamage;

                        // Adiciona ligeira deflexão no trajeto do projétil ao varar
                        Vector2 deflection = Random.insideUnitCircle * 0.4f;
                        currentDirection = Quaternion.Euler(deflection.y, deflection.x, 0f) * currentDirection;

                        // Cria faísca e fumaça no orifício de saída
                        SpawnImpactSparks(exitPoint, currentDirection);

                        if (remainingDistance <= 0f || currentPower <= 0f) break;
                    }
                    else
                    {
                        // Superfície sólida demais ou sem poder de penetração suficiente: bala para aqui
                        break;
                    }
                }
                else
                {
                    // Tiro seguiu para o vazio sem atingir nada
                    break;
                }
            }
        }


        // ========================================================================================
        // 3. MODO PROJÉTIL FÍSICO COM SUB-STEPS E BULLET DROP
        // ========================================================================================
        private IEnumerator SimulatePhysicalProjectile(Vector3 startOrigin, Vector3 startDirection)
        {
            Vector3 currentPosition = startOrigin;
            Vector3 velocity = startDirection.normalized * weaponData.BulletSpeed;
            float currentPower = weaponData.PenetrationPower;
            float currentDamage = weaponData.DamageStat;
            float gravityAccel = weaponData.BulletGravity;

            float maxLifetime = 5f;
            float lifetime = 0f;
            int penetrationCount = 0;

            while (lifetime < maxLifetime)
            {
                float dt = Time.deltaTime;
                if (dt <= 0f) yield return null;

                // Aplica aceleração gravitacional contínua (queda de bala / Bullet Drop)
                velocity += Vector3.down * gravityAccel * dt;

                Vector3 displacement = velocity * dt;
                float stepDistance = displacement.magnitude;
                Vector3 stepDirection = displacement.normalized;

                // Detecção contínua via Raycast para prevenir tunneling em alta velocidade
                if (Physics.Raycast(currentPosition, stepDirection, out RaycastHit hit, stepDistance, hitLayers, QueryTriggerInteraction.Collide))
                {
                    if (IsPlayerOrWeapon(hit.collider))
                    {
                        currentPosition = hit.point + (stepDirection * 0.05f);
                        lifetime += dt;
                        yield return null;
                        continue;
                    }

                    // Processa o impacto
                    ProcessSurfaceHit(hit, stepDirection, currentDamage);

                    // Avalia se vara a superfície
                    PenetrableSurface penetrable = hit.collider.GetComponent<PenetrableSurface>() ?? hit.collider.GetComponentInParent<PenetrableSurface>();

                    if (penetrable != null && penetrationCount < maxPenetrationLayers && penetrable.EvaluatePenetration(
                        hit.point,
                        stepDirection,
                        currentPower,
                        currentDamage,
                        out Vector3 exitPoint,
                        out float remPower,
                        out float remDamage))
                    {
                        // Atravessou o obstáculo
                        penetrationCount++;
                        currentPosition = exitPoint + (stepDirection * 0.02f);
                        currentPower = remPower;
                        currentDamage = remDamage;

                        // Perde velocidade cinética ao transpassar o material
                        velocity *= Mathf.Clamp01(1f - penetrable.DamageReductionPercentage);

                        SpawnImpactSparks(exitPoint, stepDirection);
                    }
                    else
                    {
                        // Projétil alojou-se no obstáculo e encerrou a trajetória
                        yield break;
                    }
                }
                else
                {
                    // Sem colisão: atualiza a posição no ar
                    currentPosition += displacement;
                }

                lifetime += dt;
                yield return null;
            }
        }


        // ========================================================================================
        // 4. PROCESSAMENTO DE IMPACTOS (ALVOS, RIGIDBODY, SOM E DECALQUES)
        // ========================================================================================
        private void ProcessSurfaceHit(RaycastHit hit, Vector3 bulletDirection, float damage)
        {
            if (hit.collider == null) return;

            // 1. Aplica força física cinética no Rigidbody atingido
            Rigidbody rb = hit.collider.attachedRigidbody;
            if (rb != null && !rb.isKinematic)
            {
                rb.AddForceAtPosition(bulletDirection * (damage * 8f), hit.point, ForceMode.Impulse);
            }

            // 2. Colisão com Zonas de Alvo IPSC (Papelão com pontuação Alpha, Charlie, Delta)
            IPSCTargetZone targetZone = hit.collider.GetComponent<IPSCTargetZone>();
            if (targetZone != null)
            {
                targetZone.RegisterHit(hit.point, hit.normal, bulletDecalPrefab);
                return;
            }

            // 3. Colisão com Steel Poppers Reativos (Alvos Metálicos de Aço)
            IPSCSteelPopper steelPopper = hit.collider.GetComponentInParent<IPSCSteelPopper>();
            if (steelPopper != null)
            {
                steelPopper.OnHit(hit.point, hit.normal);
                return;
            }

            // 4. Instancia decalque de orifício de entrada
            if (bulletDecalPrefab != null)
            {
                Vector3 spawnPos = hit.point + (hit.normal * 0.001f);
                Quaternion spawnRot = Quaternion.LookRotation(-hit.normal);
                GameObject decal = Instantiate(bulletDecalPrefab, spawnPos, spawnRot, hit.collider.transform);
                decal.SetActive(true);
            }

            // 5. Sons de impacto procedurais por tipo de material
            PlayProceduralImpactAudio(hit.collider.gameObject.name, hit.point);

            // 6. Efeito visual de faíscas/poeira no ponto de colisão
            SpawnImpactSparks(hit.point, hit.normal);
        }

        private void PlayProceduralImpactAudio(string objectName, Vector3 hitPoint)
        {
            string nameLower = objectName.ToLower();
            AudioClip clip = null;

            if (nameLower.Contains("wood") || nameLower.Contains("osb") || nameLower.Contains("stud") || nameLower.Contains("bench"))
            {
                clip = ProceduralAudio.CreateWoodImpactClip();
            }
            else if (nameLower.Contains("metal") || nameLower.Contains("steel") || nameLower.Contains("barrel") || nameLower.Contains("pipe"))
            {
                clip = ProceduralAudio.CreateMetalImpactClip();
            }
            else
            {
                clip = ProceduralAudio.CreateConcreteImpactClip();
            }

            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, hitPoint, 0.9f);
            }
        }

        private void SpawnImpactSparks(Vector3 hitPoint, Vector3 normal)
        {
            // Cria um clarão pontual sutil no impacto que se auto-destrói
            GameObject spark = new GameObject("ImpactSpark");
            spark.transform.position = hitPoint + (normal * 0.02f);

            Light sparkLight = spark.AddComponent<Light>();
            sparkLight.type = LightType.Point;
            sparkLight.range = 1.2f;
            sparkLight.intensity = 2.5f;
            sparkLight.color = new Color(1f, 0.85f, 0.4f);

            Destroy(spark, 0.06f);
        }

        private bool IsPlayerOrWeapon(Collider col)
        {
            if (col == null) return false;
            if (characterController != null && col == characterController) return true;
            if (playerRigidbody != null && col.attachedRigidbody == playerRigidbody) return true;
            if (col.transform.IsChildOf(transform.root)) return true;
            return false;
        }
    }
}
