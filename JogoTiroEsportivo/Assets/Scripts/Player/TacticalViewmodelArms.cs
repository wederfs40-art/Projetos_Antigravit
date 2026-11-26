using System;
using UnityEngine;

namespace IPSCSimulator.Player
{
    using IPSCSimulator.Utilities;
    using IPSCSimulator.Weapons;

    /// <summary>
    /// Sistema de Viewmodel com Braços Articulados Fotorrealistas (Skeletal Mesh) e Cinemática Inversa (Two-Bone IK):
    /// 1. Instancia a malha esquelética rigged com textura e materiais PBR de luvas táticas militares
    /// 2. Oculta a cabeça do modelo (Head.localScale = 0) para desobstruir 100% o campo de visão
    /// 3. Aplica Two-Bone IK Solver analítico de alta precisão nos braços (Ombro -> Cotovelo -> Mão)
    /// 4. Trava a mão direita no punho (Grip) e a mão esquerda no apoio/guarda-mão (Handguard)
    /// 5. Desacopla e move a mão esquerda realisticamente durante recargas táticas
    /// 6. Articula os dedos individualmente ao redor da empunhadura e gatilho
    /// </summary>
    public class TacticalViewmodelArms : MonoBehaviour
    {
        [Header("Alvos de IK na Arma")]
        [SerializeField] private Transform rightHandTarget;
        [SerializeField] private Transform leftHandTarget;
        [SerializeField] private Transform rightElbowHint;
        [SerializeField] private Transform leftElbowHint;

        [Header("Comprimentos Anatômicos")]
        [SerializeField] private float upperArmLength = 0.28f;
        [SerializeField] private float forearmLength = 0.26f;

        // Estrutura Óssea Rigged (Braço Direito)
        private Transform _rightUpperArm;
        private Transform _rightLowerArm;
        private Transform _rightHand;

        // Dedos da Mão Direita
        private Transform _rIndexProx;
        private Transform _rIndexInter;
        private Transform _rIndexDist;
        private Transform _rThumbProx;
        private Transform _rThumbInter;
        private Transform _rThumbDist;
        private Transform _rMiddleProx;
        private Transform _rRingProx;
        private Transform _rPinkyProx;

        // Estrutura Óssea Rigged (Braço Esquerdo)
        private Transform _leftUpperArm;
        private Transform _leftLowerArm;
        private Transform _leftHand;

        // Dedos da Mão Esquerda
        private Transform _lThumbProx;
        private Transform _lIndexProx;
        private Transform _lMiddleProx;
        private Transform _lRingProx;
        private Transform _lPinkyProx;

        private GameObject _rigInstance;
        private TacticalWeaponCycles _cycles;
        private bool _isSkeletalMeshLoaded = false;

        public void InitializeArms(Transform weaponHolder, Material matSleeve, Material matGlove, Material matArmor)
        {
            _cycles = weaponHolder.GetComponentInChildren<TacticalWeaponCycles>();

            // Cria os pontos de ancoragem da arma (Grip, Handguard e Hints de Cotovelo)
            CreateDefaultIKTargets(weaponHolder);

            // Carrega e instancia a malha esquelética 3D rigged
            LoadSkeletalArms(matSleeve, matGlove, matArmor);
        }

        private void LoadSkeletalArms(Material matSleeve, Material matGlove, Material matArmor)
        {
            if (_rigInstance != null)
            {
                Destroy(_rigInstance);
            }

            GameObject armaturePrefab = null;
#if UNITY_EDITOR
            armaturePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Starter Assets/Runtime/ThirdPersonController/Prefabs/PlayerArmature.prefab");
#endif

            if (armaturePrefab != null)
            {
                _rigInstance = Instantiate(armaturePrefab, transform);
                _rigInstance.name = "FPS_Skeletal_Arms_Rig";
                // Ancorado no tronco ligeiramente abaixo e recuado da câmera
                _rigInstance.transform.localPosition = new Vector3(0f, -1.38f, -0.06f);
                _rigInstance.transform.localRotation = Quaternion.identity;
                _rigInstance.transform.localScale = Vector3.one;

                // Remove componentes de física/controle que colidiriam com o jogador
                foreach (Collider c in _rigInstance.GetComponentsInChildren<Collider>())
                {
                    Destroy(c);
                }
                foreach (MonoBehaviour mb in _rigInstance.GetComponents<MonoBehaviour>())
                {
                    if (mb != this) Destroy(mb);
                }

                // Desativa Animator para que o solver IK controle os ossos diretamente
                Animator anim = _rigInstance.GetComponent<Animator>();
                if (anim != null) anim.enabled = false;

                // Oculta cabeça e pescoço para nunca obstruir a câmera
                Transform head = FindBoneRecursive(_rigInstance.transform, "Head");
                if (head != null) head.localScale = Vector3.zero;
                Transform neck = FindBoneRecursive(_rigInstance.transform, "Neck");
                if (neck != null) neck.localScale = Vector3.zero;

                // Mapeia ossos do braço direito
                _rightUpperArm = FindBoneRecursive(_rigInstance.transform, "Right_UpperArm");
                _rightLowerArm = FindBoneRecursive(_rigInstance.transform, "Right_LowerArm");
                _rightHand = FindBoneRecursive(_rigInstance.transform, "Right_Hand");

                _rIndexProx = FindBoneRecursive(_rigInstance.transform, "Right_IndexProximal");
                _rIndexInter = FindBoneRecursive(_rigInstance.transform, "Right_IndexIntermediate");
                _rIndexDist = FindBoneRecursive(_rigInstance.transform, "Right_IndexDistal");

                _rThumbProx = FindBoneRecursive(_rigInstance.transform, "Right_ThumbProximal");
                _rThumbInter = FindBoneRecursive(_rigInstance.transform, "Right_ThumbIntermediate");
                _rThumbDist = FindBoneRecursive(_rigInstance.transform, "Right_ThumbDistal");

                _rMiddleProx = FindBoneRecursive(_rigInstance.transform, "Right_MiddleProximal");
                _rRingProx = FindBoneRecursive(_rigInstance.transform, "Right_RingProximal");
                _rPinkyProx = FindBoneRecursive(_rigInstance.transform, "Right_PinkyProximal");

                // Mapeia ossos do braço esquerdo
                _leftUpperArm = FindBoneRecursive(_rigInstance.transform, "Left_UpperArm");
                _leftLowerArm = FindBoneRecursive(_rigInstance.transform, "Left_LowerArm");
                _leftHand = FindBoneRecursive(_rigInstance.transform, "Left_Hand");

                _lThumbProx = FindBoneRecursive(_rigInstance.transform, "Left_ThumbProximal");
                _lIndexProx = FindBoneRecursive(_rigInstance.transform, "Left_IndexProximal");
                _lMiddleProx = FindBoneRecursive(_rigInstance.transform, "Left_MiddleProximal");
                _lRingProx = FindBoneRecursive(_rigInstance.transform, "Left_RingProximal");
                _lPinkyProx = FindBoneRecursive(_rigInstance.transform, "Left_PinkyProximal");

                // Aplica materiais PBR de luvas de combate e mangas táticas
                SkinnedMeshRenderer smr = _rigInstance.GetComponentInChildren<SkinnedMeshRenderer>();
                if (smr != null)
                {
                    smr.quality = SkinQuality.Bone4;
                    smr.updateWhenOffscreen = true;

                    Material gloveMat = matGlove != null ? matGlove : PBRMaterialFactory.CreateCombatGlovePBRMaterial();
                    Material sleeveMat = matSleeve != null ? matSleeve : PBRMaterialFactory.CreateTacticalCamoPBRMaterial();

                    Material[] mats = smr.materials;
                    if (mats != null && mats.Length > 0)
                    {
                        mats[0] = gloveMat;
                        if (mats.Length > 1) mats[1] = sleeveMat;
                        smr.materials = mats;
                    }
                }

                _isSkeletalMeshLoaded = true;
            }
        }

        private void CreateDefaultIKTargets(Transform weaponHolder)
        {
            if (rightHandTarget == null)
            {
                GameObject rTarget = new GameObject("IK_Target_RightHand");
                rTarget.transform.SetParent(weaponHolder);
                rTarget.transform.localPosition = new Vector3(0.005f, -0.052f, 0.055f);
                rTarget.transform.localRotation = Quaternion.Euler(14f, 2f, -4f);
                rightHandTarget = rTarget.transform;
            }

            if (leftHandTarget == null)
            {
                GameObject lTarget = new GameObject("IK_Target_LeftHand");
                lTarget.transform.SetParent(weaponHolder);
                // Postura de pegada de suporte de pistola (envolve a mão direita pela frente do punho)
                lTarget.transform.localPosition = new Vector3(-0.015f, -0.062f, 0.065f);
                lTarget.transform.localRotation = Quaternion.Euler(20f, 18f, -12f);
                leftHandTarget = lTarget.transform;
            }

            if (rightElbowHint == null)
            {
                GameObject rHint = new GameObject("IK_Hint_RightElbow");
                rHint.transform.SetParent(transform);
                rHint.transform.localPosition = new Vector3(0.38f, -0.32f, 0.08f);
                rightElbowHint = rHint.transform;
            }

            if (leftElbowHint == null)
            {
                GameObject lHint = new GameObject("IK_Hint_LeftElbow");
                lHint.transform.SetParent(transform);
                lHint.transform.localPosition = new Vector3(-0.38f, -0.32f, 0.08f);
                leftElbowHint = lHint.transform;
            }
        }

        private void LateUpdate()
        {
            if (!_isSkeletalMeshLoaded || _rightUpperArm == null || _leftUpperArm == null) return;

            // 1. Two-Bone IK Solver do Braço Direito (Mão no Punho/Grip)
            if (rightHandTarget != null && _rightHand != null)
            {
                Vector3 hint = rightElbowHint != null ? rightElbowHint.position : _rightUpperArm.position + Vector3.right * 0.35f;
                SolveTwoBoneIK(_rightUpperArm, _rightLowerArm, _rightHand, rightHandTarget.position, rightHandTarget.rotation, hint);

                // Articulação dos Dedos da Mão Direita (Grip Tático e Dedo no Gatilho)
                ApplyRightHandFingers();
            }

            // 2. Two-Bone IK Solver do Braço Esquerdo (Mão de Apoio ou Recarga)
            if (leftHandTarget != null && _leftHand != null)
            {
                Vector3 targetPos = leftHandTarget.position;
                Quaternion targetRot = leftHandTarget.rotation;

                // Deslocamento dinâmico em caso de recarga
                if (_cycles != null && _cycles.IsReloading)
                {
                    targetPos += new Vector3(0.05f, -0.16f, -0.09f);
                    targetRot = Quaternion.Euler(50f, -15f, 25f);
                }

                Vector3 hint = leftElbowHint != null ? leftElbowHint.position : _leftUpperArm.position + Vector3.left * 0.35f;
                SolveTwoBoneIK(_leftUpperArm, _leftLowerArm, _leftHand, targetPos, targetRot, hint);

                // Articulação dos Dedos da Mão Esquerda (Envolve a mão direita ou o guarda-mão)
                ApplyLeftHandFingers();
            }
        }

        private void ApplyRightHandFingers()
        {
            // Dedo indicador descansando no guarda-mato ou acionando o gatilho
            if (_rIndexProx != null) _rIndexProx.localRotation = Quaternion.Euler(15f, 4f, 8f);
            if (_rIndexInter != null) _rIndexInter.localRotation = Quaternion.Euler(28f, 0f, 0f);
            if (_rIndexDist != null) _rIndexDist.localRotation = Quaternion.Euler(20f, 0f, 0f);

            // Polegar envolvendo o lado esquerdo da armação
            if (_rThumbProx != null) _rThumbProx.localRotation = Quaternion.Euler(-18f, -28f, -12f);
            if (_rThumbInter != null) _rThumbInter.localRotation = Quaternion.Euler(22f, 0f, 0f);
            if (_rThumbDist != null) _rThumbDist.localRotation = Quaternion.Euler(30f, 0f, 0f);

            // Médio, anelar e mínimo firmemente fechados no punho
            if (_rMiddleProx != null) _rMiddleProx.localRotation = Quaternion.Euler(55f, 0f, 0f);
            if (_rRingProx != null) _rRingProx.localRotation = Quaternion.Euler(62f, 0f, 0f);
            if (_rPinkyProx != null) _rPinkyProx.localRotation = Quaternion.Euler(68f, 0f, 0f);
        }

        private void ApplyLeftHandFingers()
        {
            // Polegar apoiado para cima (Thumps Forward Grip)
            if (_lThumbProx != null) _lThumbProx.localRotation = Quaternion.Euler(-12f, 25f, 10f);

            // Dedos esquerdos abraçando a frente dos dedos direitos
            if (_lIndexProx != null) _lIndexProx.localRotation = Quaternion.Euler(48f, 0f, 0f);
            if (_lMiddleProx != null) _lMiddleProx.localRotation = Quaternion.Euler(54f, 0f, 0f);
            if (_lRingProx != null) _lRingProx.localRotation = Quaternion.Euler(60f, 0f, 0f);
            if (_lPinkyProx != null) _lPinkyProx.localRotation = Quaternion.Euler(65f, 0f, 0f);
        }

        /// <summary>
        /// Solver Analítico Two-Bone Inverse Kinematics (Lei dos Cossenos com Hint Vector).
        /// </summary>
        private void SolveTwoBoneIK(Transform upper, Transform lower, Transform hand,
            Vector3 targetPos, Quaternion targetRot, Vector3 hintPos)
        {
            Vector3 a = upper.position;
            Vector3 c = targetPos;
            Vector3 ac = c - a;
            float dist = Mathf.Clamp(ac.magnitude, 0.05f, upperArmLength + forearmLength - 0.002f);

            // Lei dos cossenos
            float cosAngleA = (upperArmLength * upperArmLength + dist * dist - forearmLength * forearmLength) / (2f * upperArmLength * dist);
            float angleA = Mathf.Acos(Mathf.Clamp(cosAngleA, -1f, 1f));

            // Vetor normal do plano do braço (Shoulder, Target, Hint)
            Vector3 hintDir = hintPos - a;
            Vector3 normal = Vector3.Cross(ac, hintDir).normalized;
            if (normal == Vector3.zero) normal = Vector3.up;

            Vector3 armDir = Quaternion.AngleAxis(-angleA * Mathf.Rad2Deg, normal) * ac.normalized;
            Vector3 b = a + armDir * upperArmLength;

            // Posicionamento e rotação dos membros
            lower.position = b;
            hand.position = c;

            upper.rotation = Quaternion.LookRotation(b - a, normal);
            lower.rotation = Quaternion.LookRotation(c - b, normal);
            hand.rotation = targetRot;
        }

        private Transform FindBoneRecursive(Transform parent, string boneName)
        {
            if (parent.name.Equals(boneName, StringComparison.OrdinalIgnoreCase)) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindBoneRecursive(parent.GetChild(i), boneName);
                if (found != null) return found;
            }
            return null;
        }
    }
}
