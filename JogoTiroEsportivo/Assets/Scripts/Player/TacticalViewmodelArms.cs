using System.Collections;
using UnityEngine;

namespace IPSCSimulator.Player
{
    using IPSCSimulator.Match;
    using IPSCSimulator.Utilities;
    using IPSCSimulator.Weapons;

    /// <summary>
    /// Sistema de Braços e Luvas Táticas em Primeira Pessoa (FPS Tactical Viewmodel Arms):
    /// 1. Antebraços com mangas táticas de camuflagem militar PBR
    /// 2. Relógio tático militar no pulso esquerdo (inspirado na Imagem 4)
    /// 3. Luvas de combate estilo Mechanix M-Pact com placas de proteção nos nós dos dedos (Knuckle Armor)
    /// 4. Dedos e polegares articulados anatomicamente envolvendo a arma com precisão milimétrica
    /// 5. Postura dinâmica comutável:
    ///    - Fuzil AKM: Empunhadura C-Clamp no guarda-mão (Imagem 1)
    ///    - Pistola Beretta M9T: Empunhadura moderna de duas mãos (Imagens 2 e 4)
    /// 6. Sincronizado com movimentação procedural (Sway, Bobbing, Recoil, Inspect e Reload)
    /// </summary>
    public class TacticalViewmodelArms : MonoBehaviour
    {
        private Transform _weaponHolder;
        private TacticalWeaponCycles _cycles;
        private GameObject _handsRoot;
        private GameObject _leftArmGroup;
        private GameObject _rightArmGroup;
        private SelectedWeaponType _currentWeaponType;

        private Vector3 _leftArmBasePos;
        private Quaternion _leftArmBaseRot;

        public void InitializeArms(Transform weaponHolder, Material matSleeve, Material matGlove, Material matArmor)
        {
            _weaponHolder = weaponHolder;
            _cycles = weaponHolder.GetComponentInChildren<TacticalWeaponCycles>();

            BuildPhotorealisticHands(matSleeve, matGlove, matArmor);
        }

        private void BuildPhotorealisticHands(Material matSleeve, Material matGlove, Material matArmor)
        {
            if (_handsRoot != null)
            {
                Destroy(_handsRoot);
            }

            if (matSleeve == null) matSleeve = PBRMaterialFactory.CreateTacticalCamoPBRMaterial();
            if (matGlove == null) matGlove = PBRMaterialFactory.CreateCombatGlovePBRMaterial();
            if (matArmor == null) matArmor = PBRMaterialFactory.CreateGunSteelPBRMaterial(new Color(0.10f, 0.10f, 0.12f));

            Material matWatch = PBRMaterialFactory.CreateGunSteelPBRMaterial(new Color(0.08f, 0.08f, 0.09f));
            Material matScreen = ProceduralMaterials.CreateEmissiveMaterial(new Color(0.12f, 0.65f, 0.35f), "Mat_TacticalWatchScreen", 2.2f);

            _handsRoot = new GameObject("FPS_Photorealistic_Tactical_Hands");
            _handsRoot.transform.SetParent(_weaponHolder);
            _handsRoot.transform.localPosition = Vector3.zero;
            _handsRoot.transform.localRotation = Quaternion.identity;
            _handsRoot.transform.localScale = Vector3.one;

            // =========================================================================
            // BRAÇO E MÃO DIREITA (EMPUNHADURA FORTE / DISPARO)
            // =========================================================================
            _rightArmGroup = new GameObject("Right_Tactical_Arm");
            _rightArmGroup.transform.SetParent(_handsRoot.transform);
            _rightArmGroup.transform.localPosition = new Vector3(0.02f, -0.065f, -0.02f);
            _rightArmGroup.transform.localRotation = Quaternion.identity;

            // Antebraço Direito (Manga de Combate)
            GameObject rForearm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rForearm.name = "Right_Forearm";
            rForearm.transform.SetParent(_rightArmGroup.transform);
            rForearm.transform.localPosition = new Vector3(0.12f, -0.22f, -0.16f);
            rForearm.transform.localScale = new Vector3(0.082f, 0.22f, 0.082f);
            rForearm.transform.localRotation = Quaternion.Euler(46f, -16f, 18f);
            rForearm.GetComponent<MeshRenderer>().material = matSleeve;
            DestroyCollider(rForearm);

            // Punho / Base da Luva Direita
            GameObject rGlove = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rGlove.name = "Right_Glove_Palm";
            rGlove.transform.SetParent(_rightArmGroup.transform);
            rGlove.transform.localPosition = new Vector3(0.012f, -0.015f, 0.045f);
            rGlove.transform.localScale = new Vector3(0.052f, 0.088f, 0.065f);
            rGlove.transform.localRotation = Quaternion.Euler(14f, 2f, -4f);
            rGlove.GetComponent<MeshRenderer>().material = matGlove;
            DestroyCollider(rGlove);

            // Blindagem rígida dos nós dos dedos da luva (Knuckle Armor)
            GameObject rKnuckle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rKnuckle.name = "Right_Knuckle_Guard";
            rKnuckle.transform.SetParent(rGlove.transform);
            rKnuckle.transform.localPosition = new Vector3(0.48f, 0.12f, 0.05f);
            rKnuckle.transform.localScale = new Vector3(0.22f, 0.72f, 0.88f);
            rKnuckle.GetComponent<MeshRenderer>().material = matArmor;
            DestroyCollider(rKnuckle);

            // Dedos direitos fechados ao redor do cabo (médio, anelar, mínimo)
            for (int i = 0; i < 3; i++)
            {
                GameObject finger = GameObject.CreatePrimitive(PrimitiveType.Cube);
                finger.name = $"Right_Finger_{i}";
                finger.transform.SetParent(rGlove.transform);
                finger.transform.localPosition = new Vector3(-0.35f, -0.32f + (i * 0.28f), 0.52f);
                finger.transform.localScale = new Vector3(0.55f, 0.22f, 0.24f);
                finger.transform.localRotation = Quaternion.Euler(0, -28f, 0);
                finger.GetComponent<MeshRenderer>().material = matGlove;
                DestroyCollider(finger);
            }

            // Dedo indicador descansando no gatilho / lateral da armação
            GameObject rIndex = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rIndex.name = "Right_Trigger_Finger";
            rIndex.transform.SetParent(rGlove.transform);
            rIndex.transform.localPosition = new Vector3(-0.25f, 0.45f, 0.65f);
            rIndex.transform.localScale = new Vector3(0.35f, 0.18f, 0.55f);
            rIndex.transform.localRotation = Quaternion.Euler(6f, -14f, 8f);
            rIndex.GetComponent<MeshRenderer>().material = matGlove;
            DestroyCollider(rIndex);

            // Polegar envolvendo a lateral esquerda da armação
            GameObject rThumb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rThumb.name = "Right_Thumb";
            rThumb.transform.SetParent(rGlove.transform);
            rThumb.transform.localPosition = new Vector3(-0.48f, 0.28f, -0.15f);
            rThumb.transform.localScale = new Vector3(0.38f, 0.22f, 0.42f);
            rThumb.transform.localRotation = Quaternion.Euler(-18f, 24f, -12f);
            rThumb.GetComponent<MeshRenderer>().material = matGlove;
            DestroyCollider(rThumb);

            // =========================================================================
            // BRAÇO E MÃO ESQUERDA (APOIO / RECARGA / GUARDA-MÃO)
            // =========================================================================
            _leftArmGroup = new GameObject("Left_Tactical_Arm");
            _leftArmGroup.transform.SetParent(_handsRoot.transform);
            _leftArmGroup.transform.localPosition = new Vector3(-0.02f, -0.065f, 0.05f);
            _leftArmGroup.transform.localRotation = Quaternion.identity;

            // Antebraço Esquerdo (Manga de Combate)
            GameObject lForearm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lForearm.name = "Left_Forearm";
            lForearm.transform.SetParent(_leftArmGroup.transform);
            lForearm.transform.localPosition = new Vector3(-0.14f, -0.22f, -0.12f);
            lForearm.transform.localScale = new Vector3(0.082f, 0.22f, 0.082f);
            lForearm.transform.localRotation = Quaternion.Euler(46f, 18f, -16f);
            lForearm.GetComponent<MeshRenderer>().material = matSleeve;
            DestroyCollider(lForearm);

            // Relógio Tático Militar no Pulso Esquerdo (Inspirado na Imagem 4 de referência!)
            GameObject watchBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            watchBase.name = "Tactical_Watch_Body";
            watchBase.transform.SetParent(lForearm.transform);
            watchBase.transform.localPosition = new Vector3(0f, 0.38f, 0.42f);
            watchBase.transform.localScale = new Vector3(0.55f, 0.06f, 0.55f);
            watchBase.transform.localRotation = Quaternion.Euler(0, 0, 90f);
            watchBase.GetComponent<MeshRenderer>().material = matWatch;
            DestroyCollider(watchBase);

            GameObject watchScreen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            watchScreen.name = "Tactical_Watch_Screen";
            watchScreen.transform.SetParent(watchBase.transform);
            watchScreen.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            watchScreen.transform.localScale = new Vector3(0.65f, 0.15f, 0.65f);
            watchScreen.GetComponent<MeshRenderer>().material = matScreen;
            DestroyCollider(watchScreen);

            // Luva Esquerda
            GameObject lGlove = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lGlove.name = "Left_Glove_Palm";
            lGlove.transform.SetParent(_leftArmGroup.transform);
            lGlove.transform.localPosition = new Vector3(-0.015f, -0.010f, 0.045f);
            lGlove.transform.localScale = new Vector3(0.052f, 0.085f, 0.065f);
            lGlove.transform.localRotation = Quaternion.Euler(20f, 18f, -14f);
            lGlove.GetComponent<MeshRenderer>().material = matGlove;
            DestroyCollider(lGlove);

            // Blindagem rígida dos nós dos dedos esquerdo
            GameObject lKnuckle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lKnuckle.name = "Left_Knuckle_Guard";
            lKnuckle.transform.SetParent(lGlove.transform);
            lKnuckle.transform.localPosition = new Vector3(-0.48f, 0.12f, 0.05f);
            lKnuckle.transform.localScale = new Vector3(0.22f, 0.72f, 0.88f);
            lKnuckle.GetComponent<MeshRenderer>().material = matArmor;
            DestroyCollider(lKnuckle);

            // Dedos esquerdos
            for (int i = 0; i < 4; i++)
            {
                GameObject finger = GameObject.CreatePrimitive(PrimitiveType.Cube);
                finger.name = $"Left_Finger_{i}";
                finger.transform.SetParent(lGlove.transform);
                finger.transform.localPosition = new Vector3(0.32f, -0.38f + (i * 0.25f), 0.52f);
                finger.transform.localScale = new Vector3(0.55f, 0.20f, 0.22f);
                finger.transform.localRotation = Quaternion.Euler(0, 32f, 0);
                finger.GetComponent<MeshRenderer>().material = matGlove;
                DestroyCollider(finger);
            }

            // Polegar esquerdo apoiado à frente (Modern Thumbs-Forward Grip)
            GameObject lThumb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lThumb.name = "Left_Thumb";
            lThumb.transform.SetParent(lGlove.transform);
            lThumb.transform.localPosition = new Vector3(0.42f, 0.38f, 0.15f);
            lThumb.transform.localScale = new Vector3(0.35f, 0.20f, 0.48f);
            lThumb.transform.localRotation = Quaternion.Euler(-14f, -22f, 12f);
            lThumb.GetComponent<MeshRenderer>().material = matGlove;
            DestroyCollider(lThumb);

            _leftArmBasePos = _leftArmGroup.transform.localPosition;
            _leftArmBaseRot = _leftArmGroup.transform.localRotation;
        }

        public void SetWeaponIKStance(SelectedWeaponType weaponType)
        {
            _currentWeaponType = weaponType;
            if (_leftArmGroup == null || _rightArmGroup == null) return;

            if (weaponType == SelectedWeaponType.SMG_Tactical)
            {
                // ============================================================
                // POSTURA DE FUZIL TÁTICO AKM (C-CLAMP HANDGUARD GRIP - IMAGEM 1)
                // ============================================================
                _rightArmGroup.transform.localPosition = new Vector3(0.005f, -0.055f, -0.065f);
                _rightArmGroup.transform.localRotation = Quaternion.Euler(14f, 0f, 0f);

                // Mão esquerda estendida segurando o guarda-mão do rifle à frente
                _leftArmGroup.transform.localPosition = new Vector3(-0.065f, 0.015f, 0.265f);
                _leftArmGroup.transform.localRotation = Quaternion.Euler(24f, 14f, 38f);
            }
            else
            {
                // ============================================================
                // POSTURA DE PISTOLA/REVÓLVER (MODERN ISOSCELES TWO-HANDS - IMAGENS 2 & 4)
                // ============================================================
                _rightArmGroup.transform.localPosition = new Vector3(0.015f, -0.050f, 0.010f);
                _rightArmGroup.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);

                // Mão esquerda apoiando a mão direita por baixo e frente do punho
                _leftArmGroup.transform.localPosition = new Vector3(-0.018f, -0.062f, 0.045f);
                _leftArmGroup.transform.localRotation = Quaternion.Euler(20f, 18f, -14f);
            }

            _leftArmBasePos = _leftArmGroup.transform.localPosition;
            _leftArmBaseRot = _leftArmGroup.transform.localRotation;
        }

        private void LateUpdate()
        {
            if (_leftArmGroup == null) return;

            // Animação de Recarga Tática: a mão esquerda desce para o carregador
            if (_cycles != null && _cycles.IsReloading)
            {
                Vector3 reloadPos = _leftArmBasePos + new Vector3(0.04f, -0.18f, -0.08f);
                Quaternion reloadRot = _leftArmBaseRot * Quaternion.Euler(45f, -25f, 15f);
                _leftArmGroup.transform.localPosition = Vector3.Lerp(_leftArmGroup.transform.localPosition, reloadPos, Time.deltaTime * 12f);
                _leftArmGroup.transform.localRotation = Quaternion.Slerp(_leftArmGroup.transform.localRotation, reloadRot, Time.deltaTime * 12f);
            }
            else
            {
                _leftArmGroup.transform.localPosition = Vector3.Lerp(_leftArmGroup.transform.localPosition, _leftArmBasePos, Time.deltaTime * 10f);
                _leftArmGroup.transform.localRotation = Quaternion.Slerp(_leftArmGroup.transform.localRotation, _leftArmBaseRot, Time.deltaTime * 10f);
            }
        }

        private void DestroyCollider(GameObject obj)
        {
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
