using System.Collections;
using UnityEngine;

namespace IPSCSimulator.Player
{
    using IPSCSimulator.Match;
    using IPSCSimulator.Weapons;

    /// <summary>
    /// Gerenciador Tático de Braços e Mãos em Primeira Pessoa (FPS Tactical Viewmodel Arms):
    /// - Constrói e renderiza as mãos e braços táticos do operador em primeira pessoa empunhando a arma.
    /// - Detalhes visuais realistas: Mangas táticas em tecido ripstop escuro, luvas táticas de combate com
    ///   reforço nos nós dos dedos (knuckle protectors) e dedos articulados segurando a empunhadura e o gatilho.
    /// - Posturas dinâmicas (Two-Handed Isosceles Thumbs-Forward para pistolas/revólveres e C-Clamp / Handguard Grip para fuzis/SMG).
    /// - Sincronização 100% fluida com recuo procedural, mira ADS, respiração (sway) e passada de movimento.
    /// </summary>
    public class TacticalViewmodelArms : MonoBehaviour
    {
        [Header("Configurações dos Braços")]
        [SerializeField] private bool showArms = true;
        [SerializeField] private Color sleeveColor = new Color(0.12f, 0.13f, 0.15f); // Grafite tático de combate
        [SerializeField] private Color gloveColor = new Color(0.18f, 0.18f, 0.20f);  // Luvas táticas de couro/kevlar
        [SerializeField] private Color knuckleColor = new Color(0.08f, 0.08f, 0.09f); // Protetor de nós dos dedos reforçado

        private Transform _weaponHolder;
        private GameObject _armsRoot;
        private Transform _rightArmTrans;
        private Transform _leftArmTrans;
        private Transform _rightHandTrans;
        private Transform _leftHandTrans;

        private SelectedWeaponType _currentWeaponType = SelectedWeaponType.Pistol_9mm;
        private Material _matSleeve;
        private Material _matGlove;
        private Material _matKnuckle;

        public bool ShowArms
        {
            get => showArms;
            set
            {
                showArms = value;
                if (_armsRoot != null) _armsRoot.SetActive(value);
            }
        }

        public void InitializeArms(Transform weaponHolder, Material matSleeve = null, Material matGlove = null, Material matArmor = null)
        {
            _weaponHolder = weaponHolder;

            // Remove artefatos de braços antigos para reconstrução limpa
            Transform oldRoot = _weaponHolder.Find("FPS_Tactical_Arms");
            if (oldRoot != null)
            {
                oldRoot.gameObject.SetActive(false);
                oldRoot.SetParent(null);
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(oldRoot.gameObject);
                else Destroy(oldRoot.gameObject);
#else
                Destroy(oldRoot.gameObject);
#endif
            }

            if (!showArms) return;

            // Cria materiais táticos profissionais com shaders PBR
            Shader pbrShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            _matSleeve = matSleeve != null ? matSleeve : new Material(pbrShader)
            {
                name = "Mat_TacticalSleeve_Dark",
                color = sleeveColor
            };
            _matSleeve.SetFloat("_Smoothness", 0.15f);
            _matSleeve.SetFloat("_Metallic", 0.02f);

            _matGlove = matGlove != null ? matGlove : new Material(pbrShader)
            {
                name = "Mat_TacticalGlove_Kevlar",
                color = gloveColor
            };
            _matGlove.SetFloat("_Smoothness", 0.35f);
            _matGlove.SetFloat("_Metallic", 0.10f);

            _matKnuckle = matArmor != null ? matArmor : new Material(pbrShader)
            {
                name = "Mat_GloveKnuckle_Reinforced",
                color = knuckleColor
            };
            _matKnuckle.SetFloat("_Smoothness", 0.65f);
            _matKnuckle.SetFloat("_Metallic", 0.45f);

            BuildTacticalArmsMesh();
        }

        private void BuildTacticalArmsMesh()
        {
            _armsRoot = new GameObject("FPS_Tactical_Arms");
            _armsRoot.transform.SetParent(_weaponHolder, false);
            _armsRoot.transform.localPosition = Vector3.zero;
            _armsRoot.transform.localRotation = Quaternion.identity;
            _armsRoot.transform.localScale = Vector3.one;

            // 1. BRAÇO DIREITO (Empunha o cabo principal e dedo no gatilho)
            BuildRightArm();

            // 2. BRAÇO ESQUERDO (Apoio tático frontal ou envolvendo a mão direita)
            BuildLeftArm();

            // Configura postura inicial baseada na arma equipada
            SetWeaponIKStance(_currentWeaponType);
        }

        private void BuildRightArm()
        {
            GameObject rightRoot = new GameObject("Arm_Right");
            rightRoot.transform.SetParent(_armsRoot.transform, false);
            _rightArmTrans = rightRoot.transform;

            // Manga do Antebraço Direito (Vem do canto inferior direito da visão)
            GameObject sleeve = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            sleeve.name = "Right_Sleeve";
            sleeve.transform.SetParent(rightRoot.transform, false);
            sleeve.transform.localPosition = new Vector3(0.12f, -0.22f, -0.20f);
            sleeve.transform.localRotation = Quaternion.Euler(52f, -22f, 15f);
            sleeve.transform.localScale = new Vector3(0.065f, 0.17f, 0.065f);
            SafeRemoveCollider(sleeve);
            sleeve.GetComponent<MeshRenderer>().material = _matSleeve;

            // Pulso Direito
            GameObject wrist = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wrist.name = "Right_Wrist";
            wrist.transform.SetParent(rightRoot.transform, false);
            wrist.transform.localPosition = new Vector3(0.065f, -0.11f, -0.09f);
            wrist.transform.localRotation = Quaternion.Euler(42f, -18f, 10f);
            wrist.transform.localScale = new Vector3(0.048f, 0.06f, 0.048f);
            SafeRemoveCollider(wrist);
            wrist.GetComponent<MeshRenderer>().material = _matGlove;

            // Palma / Dorso da Mão Direita (Envolve o Grip da arma)
            GameObject palm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            palm.name = "Right_HandPalm";
            palm.transform.SetParent(rightRoot.transform, false);
            palm.transform.localPosition = new Vector3(0.022f, -0.060f, -0.025f);
            palm.transform.localRotation = Quaternion.Euler(18f, -12f, 4f);
            palm.transform.localScale = new Vector3(0.038f, 0.075f, 0.048f);
            SafeRemoveCollider(palm);
            palm.GetComponent<MeshRenderer>().material = _matGlove;
            _rightHandTrans = palm.transform;

            // Protetor de Nós dos Dedos (Knuckle Plate)
            GameObject knuckle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            knuckle.name = "Right_KnuckleGuard";
            knuckle.transform.SetParent(palm.transform, false);
            knuckle.transform.localPosition = new Vector3(0.38f, 0.15f, 0f);
            knuckle.transform.localRotation = Quaternion.identity;
            knuckle.transform.localScale = new Vector3(0.25f, 0.65f, 0.95f);
            SafeRemoveCollider(knuckle);
            knuckle.GetComponent<MeshRenderer>().material = _matKnuckle;

            // Dedo Indicador (Curvado em direção ao gatilho)
            GameObject triggerFinger = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            triggerFinger.name = "Right_TriggerFinger";
            triggerFinger.transform.SetParent(palm.transform, false);
            triggerFinger.transform.localPosition = new Vector3(-0.25f, 0.38f, 0.45f);
            triggerFinger.transform.localRotation = Quaternion.Euler(75f, 10f, 0f);
            triggerFinger.transform.localScale = new Vector3(0.26f, 0.35f, 0.26f);
            SafeRemoveCollider(triggerFinger);
            triggerFinger.GetComponent<MeshRenderer>().material = _matGlove;

            // Dedos Médio, Anelar e Mínimo (Fechados envolvendo o cabo)
            for (int i = 0; i < 3; i++)
            {
                GameObject finger = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                finger.name = $"Right_GripFinger_{i + 1}";
                finger.transform.SetParent(palm.transform, false);
                finger.transform.localPosition = new Vector3(-0.35f, 0.05f - (i * 0.28f), 0.30f);
                finger.transform.localRotation = Quaternion.Euler(10f, 95f, 0f);
                finger.transform.localScale = new Vector3(0.24f, 0.30f, 0.24f);
                SafeRemoveCollider(finger);
                finger.GetComponent<MeshRenderer>().material = _matGlove;
            }

            // Polegar Direito (Firme na lateral esquerda da arma)
            GameObject thumb = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            thumb.name = "Right_Thumb";
            thumb.transform.SetParent(palm.transform, false);
            thumb.transform.localPosition = new Vector3(-0.15f, 0.30f, -0.28f);
            thumb.transform.localRotation = Quaternion.Euler(30f, 25f, 35f);
            thumb.transform.localScale = new Vector3(0.28f, 0.30f, 0.28f);
            SafeRemoveCollider(thumb);
            thumb.GetComponent<MeshRenderer>().material = _matGlove;
        }

        private void BuildLeftArm()
        {
            GameObject leftRoot = new GameObject("Arm_Left");
            leftRoot.transform.SetParent(_armsRoot.transform, false);
            _leftArmTrans = leftRoot.transform;

            // Manga do Antebraço Esquerdo
            GameObject sleeve = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            sleeve.name = "Left_Sleeve";
            sleeve.transform.SetParent(leftRoot.transform, false);
            sleeve.transform.localPosition = new Vector3(-0.16f, -0.24f, -0.18f);
            sleeve.transform.localRotation = Quaternion.Euler(48f, 26f, -20f);
            sleeve.transform.localScale = new Vector3(0.065f, 0.17f, 0.065f);
            SafeRemoveCollider(sleeve);
            sleeve.GetComponent<MeshRenderer>().material = _matSleeve;

            // Pulso Esquerdo (com relógio tático / fita preta)
            GameObject wrist = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wrist.name = "Left_Wrist";
            wrist.transform.SetParent(leftRoot.transform, false);
            wrist.transform.localPosition = new Vector3(-0.085f, -0.12f, -0.07f);
            wrist.transform.localRotation = Quaternion.Euler(38f, 22f, -15f);
            wrist.transform.localScale = new Vector3(0.048f, 0.06f, 0.048f);
            SafeRemoveCollider(wrist);
            wrist.GetComponent<MeshRenderer>().material = _matGlove;

            // Relógio Tático Operacional
            GameObject watch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            watch.name = "Tactical_Watch";
            watch.transform.SetParent(wrist.transform, false);
            watch.transform.localPosition = new Vector3(0.52f, 0f, 0f);
            watch.transform.localScale = new Vector3(0.35f, 0.45f, 0.70f);
            SafeRemoveCollider(watch);
            watch.GetComponent<MeshRenderer>().material = _matKnuckle;

            // Palma / Dorso da Mão Esquerda
            GameObject palm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            palm.name = "Left_HandPalm";
            palm.transform.SetParent(leftRoot.transform, false);
            palm.transform.localPosition = new Vector3(-0.025f, -0.062f, -0.022f);
            palm.transform.localRotation = Quaternion.Euler(15f, 15f, -10f);
            palm.transform.localScale = new Vector3(0.038f, 0.075f, 0.048f);
            SafeRemoveCollider(palm);
            palm.GetComponent<MeshRenderer>().material = _matGlove;
            _leftHandTrans = palm.transform;

            // Protetor de Nós dos Dedos Esquerdo
            GameObject knuckle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            knuckle.name = "Left_KnuckleGuard";
            knuckle.transform.SetParent(palm.transform, false);
            knuckle.transform.localPosition = new Vector3(-0.38f, 0.15f, 0f);
            knuckle.transform.localRotation = Quaternion.identity;
            knuckle.transform.localScale = new Vector3(0.25f, 0.65f, 0.95f);
            SafeRemoveCollider(knuckle);
            knuckle.GetComponent<MeshRenderer>().material = _matKnuckle;

            // Dedos da Mão Esquerda (Envolvem a mão direita ou o handguard)
            for (int i = 0; i < 4; i++)
            {
                GameObject finger = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                finger.name = $"Left_Finger_{i + 1}";
                finger.transform.SetParent(palm.transform, false);
                finger.transform.localPosition = new Vector3(0.32f, 0.28f - (i * 0.22f), 0.25f);
                finger.transform.localRotation = Quaternion.Euler(12f, -85f, 0f);
                finger.transform.localScale = new Vector3(0.23f, 0.28f, 0.23f);
                SafeRemoveCollider(finger);
                finger.GetComponent<MeshRenderer>().material = _matGlove;
            }

            // Polegar Esquerdo (Apontado para a frente em Thumbs Forward)
            GameObject thumb = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            thumb.name = "Left_Thumb";
            thumb.transform.SetParent(palm.transform, false);
            thumb.transform.localPosition = new Vector3(0.18f, 0.38f, -0.22f);
            thumb.transform.localRotation = Quaternion.Euler(20f, -15f, -25f);
            thumb.transform.localScale = new Vector3(0.26f, 0.28f, 0.26f);
            SafeRemoveCollider(thumb);
            thumb.GetComponent<MeshRenderer>().material = _matGlove;
        }

        /// <summary>
        /// Adapta a posição e rotação relativa dos braços de acordo com o calibre e porte da arma (Pistola vs Fuzil).
        /// </summary>
        public void SetWeaponIKStance(SelectedWeaponType weaponType)
        {
            _currentWeaponType = weaponType;
            if (_armsRoot == null || _leftArmTrans == null || _rightArmTrans == null) return;

            bool isLongGun = (weaponType == SelectedWeaponType.Rifle_AKM_Tactical ||
                              weaponType == SelectedWeaponType.SMG_Tactical ||
                              weaponType == SelectedWeaponType.SMG_Strongarm_Tactical ||
                              weaponType == SelectedWeaponType.SMG_MP_Vector ||
                              weaponType == SelectedWeaponType.SMG_P90_Compact);

            if (isLongGun)
            {
                // POSTURA PARA FUZIL / SMG:
                // Mão direita no pistol grip traseiro; mão esquerda avançada para a frente segurando o handguard / telha inferior
                _rightArmTrans.localPosition = new Vector3(0.005f, 0.005f, -0.06f);
                _rightArmTrans.localRotation = Quaternion.Euler(0f, 0f, 0f);

                _leftArmTrans.localPosition = new Vector3(-0.065f, 0.045f, 0.24f); // Avança para o handguard
                _leftArmTrans.localRotation = Quaternion.Euler(-18f, 15f, -32f);
            }
            else
            {
                // POSTURA PARA PISTOLAS E REVÓLVERES (Modern Two-Handed Isosceles Thumbs Forward):
                // Mão direita empunha o cabo principal; mão esquerda fecha a empunhadura envolvendo a mão direita
                _rightArmTrans.localPosition = Vector3.zero;
                _rightArmTrans.localRotation = Quaternion.identity;

                _leftArmTrans.localPosition = Vector3.zero;
                _leftArmTrans.localRotation = Quaternion.identity;
            }
        }

        private static void SafeRemoveCollider(GameObject obj)
        {
            if (obj == null) return;
            Collider col = obj.GetComponent<Collider>();
            if (col != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(col);
                else Destroy(col);
#else
                Destroy(col);
#endif
            }
        }
    }
}
