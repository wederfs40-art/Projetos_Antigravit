using UnityEngine;

namespace IPSCSimulator.Ballistics
{
    using IPSCSimulator.Targets;

    /// <summary>
    /// Controla o comportamento físico e balístico contínuo de cada projétil instanciado.
    /// Simula gravidade real, velocidade de saída do cano e colisão contínua via Raycast.
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
        /// <param name="muzzleVelocity">Velocidade inicial em m/s (ex: 380m/s para 9mm Luger).</param>
        /// <param name="direction">Direção frontal do cano da arma (Muzzle Point).</param>
        /// <param name="gravityMultiplier">Multiplicador da gravidade da cena.</param>
        /// <param name="decalPrefab">Prefab do furo da bala a ser instanciado.</param>
        /// <param name="hitLayers">Camadas físicas elegíveis para colisão.</param>
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

            // 1. Aplica a aceleração da gravidade na velocidade atual (queda da bala / balística externa)
            _velocity += Physics.gravity * _gravityMultiplier * Time.deltaTime;

            // 2. Calcula o vetor de deslocamento deste frame
            Vector3 displacement = _velocity * Time.deltaTime;
            Vector3 newPosition = _previousPosition + displacement;
            float stepDistance = displacement.magnitude;
            Vector3 stepDirection = displacement.normalized;

            // 3. Detecção contínua por Raycast entre a posição anterior e a nova (evita tunneling)
            if (Physics.Raycast(_previousPosition, stepDirection, out RaycastHit hit, stepDistance, _hitLayers, QueryTriggerInteraction.Collide))
            {
                transform.position = hit.point;

                // Processa colisão com alvos IPSC de papel
                IPSCTargetZone targetZone = hit.collider.GetComponent<IPSCTargetZone>();
                if (targetZone != null)
                {
                    targetZone.RegisterHit(hit.point, hit.normal, _decalPrefab);
                }
                else
                {
                    // Processa colisão com alvos metálicos reativos (Steel Poppers)
                    IPSCSteelPopper steelPopper = hit.collider.GetComponentInParent<IPSCSteelPopper>();
                    if (steelPopper != null)
                    {
                        steelPopper.OnHit(hit.point, hit.normal);
                    }
                    else
                    {
                        // Superfície genérica (chão, parede, suporte de madeira)
                        if (_decalPrefab != null)
                        {
                            GameObject decal = Instantiate(_decalPrefab, hit.point + (hit.normal * 0.001f), Quaternion.LookRotation(-hit.normal));
                            decal.transform.SetParent(hit.collider.transform);
                        }
                    }
                }

                Destroy(gameObject);
                return;
            }

            // 4. Sem colisão: atualiza a posição do objeto
            transform.position = newPosition;
            _previousPosition = newPosition;
        }
    }
}
