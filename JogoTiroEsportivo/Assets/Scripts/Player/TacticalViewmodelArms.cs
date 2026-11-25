using UnityEngine;

namespace IPSCSimulator.Player
{
    using IPSCSimulator.Weapons;

    /// <summary>
    /// Sistema de Viewmodel com Braços Articulados e Cinemática Inversa (Two-Bone IK):
    /// 1. Constrói a estrutura esquelética procedural (Ombro -> Cotovelo -> Pulso -> Mão com Dedos Articulados)
    /// 2. Aplica Two-Bone IK Solver analítico em tempo real
    /// 3. Mantém a mão direita fixa no punho da arma (Grip) e a mão esquerda no guarda-mão (Handguard)
    /// 4. Desacopla dinamicamente a mão esquerda durante a recarga para trocar de carregador
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

        [Header("Ombros (Origens do Tronco)")]
        [SerializeField] private Vector3 rightShoulderLocal = new Vector3(0.20f, -0.18f, -0.15f);
        [SerializeField] private Vector3 leftShoulderLocal = new Vector3(-0.20f, -0.18f, -0.15f);

        // Estrutura do Braço Direito
        private Transform _rightShoulder;
        private Transform _rightElbow;
        private Transform _rightWrist;
        private Transform _rightHand;

        // Estrutura do Braço Esquerdo
        private Transform _leftShoulder;
        private Transform _leftElbow;
        private Transform _leftWrist;
        private Transform _leftHand;

        private TacticalWeaponCycles _cycles;

        public void InitializeArms(Transform weaponHolder, Material matSleeve, Material matGlove, Material matArmor)
        {
            _cycles = weaponHolder.GetComponentInChildren<TacticalWeaponCycles>();

            // Criação das origens dos ombros
            GameObject rShoulderObj = new GameObject("Shoulder_R");
            rShoulderObj.transform.SetParent(transform);
            rShoulderObj.transform.localPosition = rightShoulderLocal;
            _rightShoulder = rShoulderObj.transform;

            GameObject lShoulderObj = new GameObject("Shoulder_L");
            lShoulderObj.transform.SetParent(transform);
            lShoulderObj.transform.localPosition = leftShoulderLocal;
            _leftShoulder = lShoulderObj.transform;

            // Criação dos membros e ossos articulados
            BuildArmBones(_rightShoulder, true, matSleeve, matGlove, matArmor, out _rightElbow, out _rightWrist, out _rightHand);
            BuildArmBones(_leftShoulder, false, matSleeve, matGlove, matArmor, out _leftElbow, out _leftWrist, out _leftHand);

            // Cria alvos de IK se não existirem
            CreateDefaultIKTargets(weaponHolder);
        }

        private void BuildArmBones(Transform shoulder, bool isRight, Material matSleeve, Material matGlove, Material matArmor,
            out Transform elbow, out Transform wrist, out Transform hand)
        {
            // 1. Cotovelo
            GameObject elbowObj = new GameObject(isRight ? "Elbow_R" : "Elbow_L");
            elbowObj.transform.SetParent(shoulder);
            elbow = elbowObj.transform;

            // Malha do braço superior (Upper Arm / Manga)
            GameObject upperArmMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            upperArmMesh.name = "UpperArm_Mesh";
            upperArmMesh.transform.SetParent(shoulder);
            upperArmMesh.transform.localScale = new Vector3(0.082f, upperArmLength * 0.5f, 0.082f);
            upperArmMesh.GetComponent<MeshRenderer>().material = matSleeve;
            Destroy(upperArmMesh.GetComponent<Collider>());

            // 2. Pulso
            GameObject wristObj = new GameObject(isRight ? "Wrist_R" : "Wrist_L");
            wristObj.transform.SetParent(elbow);
            wrist = wristObj.transform;

            // Malha do antebraço (Forearm / Manga)
            GameObject forearmMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            forearmMesh.name = "Forearm_Mesh";
            forearmMesh.transform.SetParent(elbow);
            forearmMesh.transform.localScale = new Vector3(0.074f, forearmLength * 0.5f, 0.074f);
            forearmMesh.GetComponent<MeshRenderer>().material = matSleeve;
            Destroy(forearmMesh.GetComponent<Collider>());

            // 3. Mão (Luva Tática)
            GameObject handObj = new GameObject(isRight ? "Hand_R" : "Hand_L");
            handObj.transform.SetParent(wrist);
            hand = handObj.transform;

            GameObject palm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            palm.name = "Glove_Palm";
            palm.transform.SetParent(hand);
            palm.transform.localPosition = new Vector3(0, 0, 0.035f);
            palm.transform.localScale = new Vector3(0.065f, 0.038f, 0.080f);
            palm.GetComponent<MeshRenderer>().material = matGlove;
            Destroy(palm.GetComponent<Collider>());

            // Placa reforçada de nó dos dedos (Knuckle Armor)
            GameObject knuckle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            knuckle.name = "Knuckle_Armor";
            knuckle.transform.SetParent(palm.transform);
            knuckle.transform.localPosition = new Vector3(0, 0.55f, 0.15f);
            knuckle.transform.localScale = new Vector3(0.92f, 0.28f, 0.65f);
            knuckle.GetComponent<MeshRenderer>().material = matArmor;
            Destroy(knuckle.GetComponent<Collider>());

            // 5 Dedos Articulados em volta da empunhadura
            for (int f = 0; f < 5; f++)
            {
                float posX = -0.024f + (f * 0.012f);
                GameObject finger = GameObject.CreatePrimitive(PrimitiveType.Cube);
                finger.name = $"Finger_{f}";
                finger.transform.SetParent(hand);
                finger.transform.localPosition = new Vector3(posX, -0.012f, 0.085f);
                finger.transform.localScale = new Vector3(0.011f, 0.012f, 0.032f);
                finger.transform.localRotation = Quaternion.Euler(28f, (f - 2) * 4f, 0);
                finger.GetComponent<MeshRenderer>().material = matGlove;
                Destroy(finger.GetComponent<Collider>());
            }
        }

        private void CreateDefaultIKTargets(Transform weaponHolder)
        {
            if (rightHandTarget == null)
            {
                GameObject rTarget = new GameObject("IK_Target_RightHand (Grip)");
                rTarget.transform.SetParent(weaponHolder);
                rTarget.transform.localPosition = new Vector3(0f, -0.055f, 0.14f);
                rTarget.transform.localRotation = Quaternion.Euler(12f, 0, 0);
                rightHandTarget = rTarget.transform;
            }

            if (leftHandTarget == null)
            {
                GameObject lTarget = new GameObject("IK_Target_LeftHand (Handguard)");
                lTarget.transform.SetParent(weaponHolder);
                lTarget.transform.localPosition = new Vector3(-0.045f, -0.060f, 0.26f);
                lTarget.transform.localRotation = Quaternion.Euler(22f, 25f, -15f);
                leftHandTarget = lTarget.transform;
            }

            if (rightElbowHint == null)
            {
                GameObject rHint = new GameObject("IK_Hint_RightElbow");
                rHint.transform.SetParent(transform);
                rHint.transform.localPosition = new Vector3(0.40f, -0.35f, 0.05f);
                rightElbowHint = rHint.transform;
            }

            if (leftElbowHint == null)
            {
                GameObject lHint = new GameObject("IK_Hint_LeftElbow");
                lHint.transform.SetParent(transform);
                lHint.transform.localPosition = new Vector3(-0.40f, -0.35f, 0.05f);
                leftElbowHint = lHint.transform;
            }
        }

        private void LateUpdate()
        {
            if (_rightShoulder == null || _leftShoulder == null) return;

            // Resolve IK do Braço Direito (Mão no Punho/Grip)
            if (rightHandTarget != null)
            {
                SolveTwoBoneIK(_rightShoulder, _rightElbow, _rightWrist, _rightHand,
                    rightHandTarget.position, rightHandTarget.rotation,
                    rightElbowHint != null ? rightElbowHint.position : _rightShoulder.position + Vector3.right * 0.3f);
            }

            // Resolve IK do Braço Esquerdo (Mão no Guarda-Mão / Suporte)
            if (leftHandTarget != null)
            {
                Vector3 targetPos = leftHandTarget.position;
                Quaternion targetRot = leftHandTarget.rotation;

                // Se estiver recarregando, a mão esquerda se descola para baixo em direção ao magazine
                if (_cycles != null && _cycles.IsReloading)
                {
                    targetPos += new Vector3(0.04f, -0.14f, -0.08f);
                    targetRot = Quaternion.Euler(45f, -10f, 20f);
                }

                SolveTwoBoneIK(_leftShoulder, _leftElbow, _leftWrist, _leftHand,
                    targetPos, targetRot,
                    leftElbowHint != null ? leftElbowHint.position : _leftShoulder.position + Vector3.left * 0.3f);
            }
        }

        /// <summary>
        /// Solver Analítico Two-Bone Inverse Kinematics (Lei dos Cossenos com Hint Vector).
        /// </summary>
        private void SolveTwoBoneIK(Transform shoulder, Transform elbow, Transform wrist, Transform hand,
            Vector3 targetPos, Quaternion targetRot, Vector3 hintPos)
        {
            Vector3 a = shoulder.position;
            Vector3 c = targetPos;
            Vector3 ac = c - a;
            float dist = Mathf.Clamp(ac.magnitude, 0.02f, upperArmLength + forearmLength - 0.002f);

            // Lei dos cossenos para encontrar ângulos internos
            float cosAngleA = (upperArmLength * upperArmLength + dist * dist - forearmLength * forearmLength) / (2f * upperArmLength * dist);
            float angleA = Mathf.Acos(Mathf.Clamp(cosAngleA, -1f, 1f));

            // Plano formado por Shoulder, Target e Hint
            Vector3 hintDir = hintPos - a;
            Vector3 normal = Vector3.Cross(ac, hintDir).normalized;
            if (normal == Vector3.zero) normal = Vector3.up;

            Vector3 armDir = Quaternion.AngleAxis(-angleA * Mathf.Rad2Deg, normal) * ac.normalized;
            Vector3 b = a + armDir * upperArmLength;

            // Posiciona cotovelo
            elbow.position = b;
            wrist.position = c;

            // Alinha rotação do braço superior
            shoulder.rotation = Quaternion.LookRotation(b - a, normal);

            // Alinha rotação do antebraço
            elbow.rotation = Quaternion.LookRotation(c - b, normal);

            // Alinha rotação final da mão ao alvo
            hand.rotation = targetRot;
        }
    }
}
