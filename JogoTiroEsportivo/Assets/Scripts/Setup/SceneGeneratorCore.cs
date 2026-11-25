using System.Collections.Generic;
using UnityEngine;

namespace IPSCSimulator.Setup
{
    using IPSCSimulator.Ballistics;
    using IPSCSimulator.Match;
    using IPSCSimulator.Player;
    using IPSCSimulator.Targets;
    using IPSCSimulator.UI;
    using IPSCSimulator.Utilities;
    using IPSCSimulator.Weapons;

    /// <summary>
    /// Construtor de cena avançado: gera o estande de tiro IPSC de alta fidelidade visual,
    /// iluminação cinematográfica solar, chão de cascalho texturizado, barris táticos,
    /// biombos de madeira, bancada de municiamento com estojos e caixas de munição,
    /// alvos de papelão oficial, alvos metálicos reativos (Poppers) e pistola 3D detalhada.
    /// </summary>
    public static class SceneGeneratorCore
    {
        public static void GenerateCompleteShootingRange()
        {
            Debug.Log("[IPSC Setup] Iniciando montagem do estande de tiro de alta fidelidade...");

            // 1. Remove versão anterior se já existir na cena para evitar duplicatas
            GameObject oldRoot = GameObject.Find("--- IPSC SHOOTING RANGE ---");
            if (oldRoot != null)
            {
                Object.DestroyImmediate(oldRoot);
            }

            GameObject root = new GameObject("--- IPSC SHOOTING RANGE ---");

            // 2. Configuração de Iluminação Fotorealista
            SetupRealisticLighting();

            // 3. Criação dos Materiais PBR
            // Chão de Cascalho e Areia
            Material matGround = ProceduralMaterials.CreateMaterial(new Color(0.48f, 0.43f, 0.36f), "Mat_Gravel_PBR", 0.15f, 0.05f);
            Texture2D gravelTex = ProceduralMaterials.CreateGravelTexture();
            matGround.mainTexture = gravelTex;
            matGround.mainTextureScale = new Vector2(10f, 18f);

            // Madeira de Pinus para Biombos e Paredes
            Material matWood = ProceduralMaterials.CreateMaterial(new Color(0.65f, 0.50f, 0.33f), "Mat_Wood_Planks", 0.25f, 0.0f);
            Texture2D woodTex = ProceduralMaterials.CreateWoodPlankTexture();
            matWood.mainTexture = woodTex;
            matWood.mainTextureScale = new Vector2(2f, 8f);

            // Para-balas de Terra
            Material matBerm = ProceduralMaterials.CreateMaterial(new Color(0.32f, 0.22f, 0.15f), "Mat_EarthBerm", 0.05f, 0.0f);
            matBerm.mainTexture = gravelTex;
            matBerm.mainTextureScale = new Vector2(6f, 4f);

            // Faixas de Advertência (Fault Lines)
            Material matHazard = ProceduralMaterials.CreateMaterial(Color.white, "Mat_HazardStripes", 0.3f, 0.0f);
            Texture2D hazardTex = ProceduralMaterials.CreateWarningStripeTexture();
            matHazard.mainTexture = hazardTex;
            matHazard.mainTextureScale = new Vector2(1f, 8f);

            // Barris Industriais
            Material matBarrelBlue = ProceduralMaterials.CreateMaterial(new Color(0.12f, 0.35f, 0.75f), "Mat_BarrelBlue", 0.55f, 0.45f);
            matBarrelBlue.mainTexture = ProceduralMaterials.CreateBarrelTexture(new Color(0.12f, 0.35f, 0.75f));

            Material matBarrelYellow = ProceduralMaterials.CreateMaterial(new Color(0.85f, 0.70f, 0.12f), "Mat_BarrelYellow", 0.55f, 0.45f);
            matBarrelYellow.mainTexture = ProceduralMaterials.CreateBarrelTexture(new Color(0.85f, 0.70f, 0.12f));

            // Metal de Armas e Munições
            Material matGunMetal = ProceduralMaterials.CreateMaterial(new Color(0.10f, 0.10f, 0.11f), "Mat_GunBlack", 0.6f, 0.7f);
            Material matGunSlide = ProceduralMaterials.CreateMaterial(new Color(0.18f, 0.18f, 0.20f), "Mat_GunSlide", 0.75f, 0.85f);
            Material matBrass = ProceduralMaterials.CreateMaterial(new Color(0.92f, 0.78f, 0.32f), "Mat_BrassGold", 0.85f, 0.95f);
            Material matSightFiber = ProceduralMaterials.CreateEmissiveMaterial(new Color(1f, 0.1f, 0.05f), "Mat_FiberOpticGlow", 2.0f);

            // Chapa de Aço para Alvos Poppers
            Material matSteelPopper = ProceduralMaterials.CreateMaterial(new Color(0.90f, 0.90f, 0.92f), "Mat_SteelPlate", 0.5f, 0.65f);

            // 4. Cenografia do Estande
            GameObject envObj = new GameObject("Environment_ShootingRange");
            envObj.transform.SetParent(root.transform);

            // Chão
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground_Gravel";
            ground.transform.SetParent(envObj.transform);
            ground.transform.position = new Vector3(0, -0.25f, 12f);
            ground.transform.localScale = new Vector3(20f, 0.5f, 32f);
            ground.GetComponent<MeshRenderer>().material = matGround;

            // Para-balas de fundo (Berm imponente)
            GameObject berm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            berm.name = "Backstop_Berm";
            berm.transform.SetParent(envObj.transform);
            berm.transform.position = new Vector3(0, 2.8f, 26f);
            berm.transform.localScale = new Vector3(22f, 6f, 5f);
            berm.GetComponent<MeshRenderer>().material = matBerm;

            // Barreiras laterais de contenção
            CreateWall(envObj.transform, new Vector3(-9.5f, 1.8f, 12f), new Vector3(1f, 4f, 32f), matWood);
            CreateWall(envObj.transform, new Vector3(9.5f, 1.8f, 12f), new Vector3(1f, 4f, 32f), matWood);

            // Shooting Box com faixas de advertência
            CreateShootingBox(envObj.transform, Vector3.zero, matHazard);

            // Barris Táticos (55-gallon drums)
            CreateBarrel(envObj.transform, new Vector3(-1.8f, 0.45f, 3.5f), matBarrelBlue);
            CreateBarrel(envObj.transform, new Vector3(-1.8f, 1.35f, 3.5f), matBarrelBlue); // Barril empilhado
            CreateBarrel(envObj.transform, new Vector3(-2.4f, 0.45f, 3.8f), matBarrelYellow);

            CreateBarrel(envObj.transform, new Vector3(2.0f, 0.45f, 5.0f), matBarrelYellow);
            CreateBarrel(envObj.transform, new Vector3(2.6f, 0.45f, 5.3f), matBarrelBlue);

            // Biombos de Madeira com Passagem (Barricadas Táticas)
            CreateBarricade(envObj.transform, new Vector3(-3.2f, 1.1f, 8f), new Vector3(2.5f, 2.2f, 0.1f), matWood);
            CreateBarricade(envObj.transform, new Vector3(3.2f, 1.1f, 10f), new Vector3(2.5f, 2.2f, 0.1f), matWood);

            // Mesa de Municiamento (Safety Bench) lateral
            CreateShootingBench(envObj.transform, new Vector3(-3.2f, 0f, 0.5f), matWood, matBrass);

            // 5. Criação do Prefab de Decalque (Furo de Bala)
            GameObject decalPrefab = CreateBulletHoleDecalObject();
            decalPrefab.transform.SetParent(root.transform);
            decalPrefab.SetActive(false);

            // 6. Criação do Projétil
            GameObject bulletPrefab = CreateBulletPrefab(decalPrefab);
            bulletPrefab.transform.SetParent(root.transform);
            bulletPrefab.SetActive(false);

            // 7. Alvos Oficiais IPSC (Papelão)
            List<IPSCTarget> targetsList = new List<IPSCTarget>();
            targetsList.Add(CreateIPSCTarget(root.transform, "Target_01 (7m)", new Vector3(0f, 1.4f, 7f), false, matWood));
            targetsList.Add(CreateIPSCTarget(root.transform, "Target_02 (12m)", new Vector3(-4.0f, 1.4f, 12f), false, matWood));
            targetsList.Add(CreateIPSCTarget(root.transform, "NoShoot_Penalty", new Vector3(-3.2f, 1.35f, 11.6f), true, matWood));
            targetsList.Add(CreateIPSCTarget(root.transform, "Target_03 (16m)", new Vector3(3.8f, 1.4f, 16f), false, matWood));

            // 8. Alvos Metálicos Reativos (Steel Poppers / Plates)
            CreateSteelPopper(root.transform, "Steel_Popper_Left (9m)", new Vector3(-1.8f, 0f, 9.5f), matSteelPopper);
            CreateSteelPopper(root.transform, "Steel_Popper_Right (14m)", new Vector3(1.6f, 0f, 14.0f), matSteelPopper);

            // 9. Jogador e Pistola 3D Detalhada com Fibra Ótica
            GameObject player = CreatePlayerWithDetailedWeapon(root.transform, bulletPrefab, decalPrefab, matGunMetal, matGunSlide, matSightFiber, matBrass);

            // 10. Range Officer Timer
            GameObject roObj = new GameObject("RangeOfficer_Timer");
            roObj.transform.SetParent(root.transform);
            AudioSource roAudio = roObj.AddComponent<AudioSource>();
            roAudio.playOnAwake = false;
            RangeOfficerTimer timer = roObj.AddComponent<RangeOfficerTimer>();

            SetFieldValue(timer, "audioSource", roAudio);
            SetFieldValue(timer, "beepSound", ProceduralAudio.CreateTimerBeepClip());
            SetFieldValue(timer, "stageTargets", targetsList);

            // 11. HUD Manager
            GameObject hudObj = new GameObject("IPSC_HUD_Manager");
            hudObj.transform.SetParent(root.transform);
            hudObj.AddComponent<IPSCUIManager>();

            Debug.Log("[IPSC Setup] Estande de alta fidelidade montado com sucesso!");
        }

        private static void SetupRealisticLighting()
        {
            // Luz Solar Direcional com Sombras Suaves
            Light sun = Object.FindAnyObjectByType<Light>();
            if (sun == null)
            {
                GameObject sunObj = new GameObject("Directional_Sun");
                sun = sunObj.AddComponent<Light>();
                sun.type = LightType.Directional;
            }

            sun.transform.rotation = Quaternion.Euler(42f, -38f, 0f);
            sun.color = new Color(1.0f, 0.96f, 0.88f); // Luz dourada e quente de sol
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
            sun.shadowResolution = UnityEngine.Rendering.LightShadowResolution.VeryHigh;
            sun.shadowBias = 0.04f;
            sun.shadowNormalBias = 0.35f;

            // Iluminação Ambiente Trilight (Céu azul, horizonte suave, terra quente)
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.68f, 0.80f, 0.96f);
            RenderSettings.ambientEquatorColor = new Color(0.72f, 0.68f, 0.62f);
            RenderSettings.ambientGroundColor = new Color(0.40f, 0.34f, 0.26f);

            // Névoa Atmosférica de Profundidade
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.007f;
            RenderSettings.fogColor = new Color(0.76f, 0.82f, 0.88f);
        }

        private static void CreateBarrel(Transform parent, Vector3 pos, Material mat)
        {
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "Tactical_Barrel_55Gal";
            barrel.transform.SetParent(parent);
            barrel.transform.position = pos;
            barrel.transform.localScale = new Vector3(0.62f, 0.48f, 0.62f); // Altura de ~0.96m
            barrel.GetComponent<MeshRenderer>().material = mat;
        }

        private static void CreateBarricade(Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject barricade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barricade.name = "Vision_Barricade";
            barricade.transform.SetParent(parent);
            barricade.transform.position = pos;
            barricade.transform.localScale = scale;
            barricade.GetComponent<MeshRenderer>().material = mat;
        }

        private static void CreateShootingBench(Transform parent, Vector3 pos, Material woodMat, Material brassMat)
        {
            GameObject benchRoot = new GameObject("Safety_Area_Bench");
            benchRoot.transform.SetParent(parent);
            benchRoot.transform.position = pos;

            // Tampo da mesa
            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.name = "Bench_Top";
            top.transform.SetParent(benchRoot.transform);
            top.transform.localPosition = new Vector3(0, 0.85f, 0);
            top.transform.localScale = new Vector3(1.4f, 0.08f, 0.75f);
            top.GetComponent<MeshRenderer>().material = woodMat;

            // Pés da mesa
            CreateBenchLeg(benchRoot.transform, new Vector3(-0.6f, 0.42f, -0.3f), woodMat);
            CreateBenchLeg(benchRoot.transform, new Vector3(0.6f, 0.42f, -0.3f), woodMat);
            CreateBenchLeg(benchRoot.transform, new Vector3(-0.6f, 0.42f, 0.3f), woodMat);
            CreateBenchLeg(benchRoot.transform, new Vector3(0.6f, 0.42f, 0.3f), woodMat);

            // Caixas de Munição 9mm sobre a mesa
            Material matAmmoBox = ProceduralMaterials.CreateMaterial(new Color(0.20f, 0.30f, 0.18f), "Mat_AmmoBox_Green", 0.4f, 0.1f);
            GameObject box1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box1.name = "AmmoBox_9mm";
            box1.transform.SetParent(benchRoot.transform);
            box1.transform.localPosition = new Vector3(-0.35f, 0.94f, 0.05f);
            box1.transform.localScale = new Vector3(0.18f, 0.10f, 0.12f);
            box1.transform.localRotation = Quaternion.Euler(0, 15f, 0);
            box1.GetComponent<MeshRenderer>().material = matAmmoBox;
            Object.DestroyImmediate(box1.GetComponent<Collider>());

            // Estojos de bala dourados espalhados
            for (int i = 0; i < 5; i++)
            {
                GameObject casing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                casing.name = $"Brass_Casing_{i}";
                casing.transform.SetParent(benchRoot.transform);
                casing.transform.localPosition = new Vector3(Random.Range(-0.1f, 0.4f), 0.90f, Random.Range(-0.2f, 0.2f));
                casing.transform.localScale = new Vector3(0.012f, 0.02f, 0.012f);
                casing.transform.localRotation = Quaternion.Euler(90f, Random.Range(0, 360f), 0);
                casing.GetComponent<MeshRenderer>().material = brassMat;
                Object.DestroyImmediate(casing.GetComponent<Collider>());
            }
        }

        private static void CreateBenchLeg(Transform parent, Vector3 localPos, Material mat)
        {
            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leg.transform.SetParent(parent);
            leg.transform.localPosition = localPos;
            leg.transform.localScale = new Vector3(0.08f, 0.84f, 0.08f);
            leg.GetComponent<MeshRenderer>().material = mat;
        }

        private static void CreateSteelPopper(Transform parent, string name, Vector3 pos, Material mat)
        {
            GameObject popperRoot = new GameObject(name);
            popperRoot.transform.SetParent(parent);
            popperRoot.transform.position = pos;

            // Base de apoio no chão
            GameObject basePlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            basePlate.name = "Popper_Base";
            basePlate.transform.SetParent(popperRoot.transform);
            basePlate.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            basePlate.transform.localScale = new Vector3(0.45f, 0.08f, 0.55f);
            basePlate.GetComponent<MeshRenderer>().material = mat;

            // Eixo rotativo (Hinge)
            GameObject hinge = new GameObject("Popper_Hinge");
            hinge.transform.SetParent(popperRoot.transform);
            hinge.transform.localPosition = new Vector3(0f, 0.08f, 0f);

            // Haste de aço vertical
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pole.name = "Popper_Body";
            pole.transform.SetParent(hinge.transform);
            pole.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            pole.transform.localScale = new Vector3(0.16f, 0.75f, 0.035f);
            pole.GetComponent<MeshRenderer>().material = mat;

            // Cabeça circular de impacto (Steel Plate)
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            head.name = "Popper_Head";
            head.transform.SetParent(hinge.transform);
            head.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            head.transform.localScale = new Vector3(0.35f, 0.02f, 0.35f);
            head.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            head.GetComponent<MeshRenderer>().material = mat;

            // Script do Popper Reativo
            IPSCSteelPopper popperScript = popperRoot.AddComponent<IPSCSteelPopper>();
            SetFieldValue(popperScript, "targetName", name);
            SetFieldValue(popperScript, "hingeTransform", hinge.transform);
        }

        private static void CreateWall(Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Safety_Wall";
            wall.transform.SetParent(parent);
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            wall.GetComponent<MeshRenderer>().material = mat;
        }

        private static void CreateShootingBox(Transform parent, Vector3 center, Material mat)
        {
            GameObject box = new GameObject("IPSC_ShootingBox");
            box.transform.SetParent(parent);
            box.transform.position = center;

            float size = 1.1f;
            float thickness = 0.08f;

            CreateRip(box.transform, new Vector3(0, 0.02f, size * 0.5f), new Vector3(size, 0.04f, thickness), mat);
            CreateRip(box.transform, new Vector3(0, 0.02f, -size * 0.5f), new Vector3(size, 0.04f, thickness), mat);
            CreateRip(box.transform, new Vector3(size * 0.5f, 0.02f, 0), new Vector3(thickness, 0.04f, size), mat);
            CreateRip(box.transform, new Vector3(-size * 0.5f, 0.02f, 0), new Vector3(thickness, 0.04f, size), mat);
        }

        private static void CreateRip(Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject rip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rip.transform.SetParent(parent);
            rip.transform.localPosition = pos;
            rip.transform.localScale = scale;
            rip.GetComponent<MeshRenderer>().material = mat;
            Object.DestroyImmediate(rip.GetComponent<Collider>());
        }

        private static GameObject CreateBulletHoleDecalObject()
        {
            GameObject decal = GameObject.CreatePrimitive(PrimitiveType.Quad);
            decal.name = "Decal_BulletHole_Prefab";
            decal.transform.localScale = new Vector3(0.045f, 0.045f, 1f);

            Material decalMat = ProceduralMaterials.CreateMaterial(Color.white, "Mat_DecalBulletHole", 0f);
            decalMat.mainTexture = ProceduralMaterials.CreateBulletHoleTexture();
            decalMat.SetFloat("_Mode", 2);
            decalMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            decalMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            decalMat.SetInt("_ZWrite", 0);
            decalMat.DisableKeyword("_ALPHATEST_ON");
            decalMat.EnableKeyword("_ALPHABLEND_ON");
            decalMat.renderQueue = 3000;

            decal.GetComponent<MeshRenderer>().material = decalMat;
            Object.DestroyImmediate(decal.GetComponent<Collider>());
            return decal;
        }

        private static GameObject CreateBulletPrefab(GameObject decalPrefab)
        {
            GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bullet.name = "Bullet_Prefab";
            bullet.transform.localScale = new Vector3(0.015f, 0.015f, 0.035f);

            Material bulletMat = ProceduralMaterials.CreateMaterial(new Color(0.95f, 0.85f, 0.35f), "Mat_Bullet_Brass", 0.9f, 0.95f);
            bullet.GetComponent<MeshRenderer>().material = bulletMat;
            Object.DestroyImmediate(bullet.GetComponent<Collider>());

            bullet.AddComponent<Bullet>();
            return bullet;
        }

        public static IPSCTarget CreateIPSCTarget(Transform parent, string name, Vector3 position, bool isNoShoot, Material woodMat)
        {
            GameObject targetObj = new GameObject(name);
            targetObj.transform.SetParent(parent);
            targetObj.transform.position = position;

            IPSCTarget targetScript = targetObj.AddComponent<IPSCTarget>();
            SetFieldValue(targetScript, "targetName", name);
            SetFieldValue(targetScript, "isNoShoot", isNoShoot);

            // Suportes de ripa de madeira
            GameObject stickL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stickL.name = "Stick_Left";
            stickL.transform.SetParent(targetObj.transform);
            stickL.transform.localPosition = new Vector3(-0.18f, -0.5f, -0.02f);
            stickL.transform.localScale = new Vector3(0.04f, 1.4f, 0.04f);
            stickL.GetComponent<MeshRenderer>().material = woodMat;

            GameObject stickR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stickR.name = "Stick_Right";
            stickR.transform.SetParent(targetObj.transform);
            stickR.transform.localPosition = new Vector3(0.18f, -0.5f, -0.02f);
            stickR.transform.localScale = new Vector3(0.04f, 1.4f, 0.04f);
            stickR.GetComponent<MeshRenderer>().material = woodMat;

            // Placa de papelão
            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Paper_Board";
            board.transform.SetParent(targetObj.transform);
            board.transform.localPosition = Vector3.zero;
            board.transform.localScale = new Vector3(0.45f, 0.75f, 0.005f);

            Material targetMat = ProceduralMaterials.CreateMaterial(Color.white, isNoShoot ? "Mat_TargetNoShoot" : "Mat_TargetIPSC", 0.1f);
            targetMat.mainTexture = ProceduralMaterials.CreateIPSCTargetTexture(isNoShoot);
            board.GetComponent<MeshRenderer>().material = targetMat;
            Object.DestroyImmediate(board.GetComponent<Collider>());

            // Zona Alpha (Centro: 15cm x 32cm)
            CreateTargetZone(targetObj.transform, "Zone_Alpha", Vector3.zero, new Vector3(0.15f, 0.32f, 0.02f), isNoShoot ? IPSCZoneType.NoShoot : IPSCZoneType.Alpha, targetScript);

            // Zona Charlie (Intermediária: 30cm x 55cm)
            CreateTargetZone(targetObj.transform, "Zone_Charlie", Vector3.zero, new Vector3(0.30f, 0.55f, 0.02f), isNoShoot ? IPSCZoneType.NoShoot : IPSCZoneType.Charlie, targetScript);

            // Zona Delta (Externa: 45cm x 75cm)
            CreateTargetZone(targetObj.transform, "Zone_Delta", Vector3.zero, new Vector3(0.45f, 0.75f, 0.02f), isNoShoot ? IPSCZoneType.NoShoot : IPSCZoneType.Delta, targetScript);

            return targetScript;
        }

        private static void CreateTargetZone(Transform parent, string zoneName, Vector3 localPos, Vector3 size, IPSCZoneType type, IPSCTarget parentTarget)
        {
            GameObject zoneObj = new GameObject(zoneName);
            zoneObj.transform.SetParent(parent);
            zoneObj.transform.localPosition = localPos;

            BoxCollider col = zoneObj.AddComponent<BoxCollider>();
            col.size = size;
            col.isTrigger = false;

            IPSCTargetZone zone = zoneObj.AddComponent<IPSCTargetZone>();
            SetFieldValue(zone, "zoneType", type);
            SetFieldValue(zone, "parentTarget", parentTarget);
        }

        private static GameObject CreatePlayerWithDetailedWeapon(Transform parent, GameObject bulletPrefab, GameObject decalPrefab, Material matMetal, Material matSlide, Material matFiber, Material matBrass)
        {
            GameObject player = new GameObject("Player");
            player.transform.SetParent(parent);
            player.transform.position = new Vector3(0f, 1.0f, 0f);

            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0, 0.9f, 0);

            PlayerController controller = player.AddComponent<PlayerController>();

            // Câmera com FOV realista
            GameObject camObj = new GameObject("PlayerCamera");
            camObj.transform.SetParent(player.transform);
            camObj.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            Camera cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            camObj.AddComponent<AudioListener>();
            cam.fieldOfView = 68f;
            cam.nearClipPlane = 0.02f;

            // Desativa câmeras antigas
            Camera[] allCams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (Camera c in allCams)
            {
                if (c != cam && c != null) c.gameObject.SetActive(false);
            }

            SetFieldValue(controller, "playerCameraTransform", camObj.transform);

            // Suporte da Arma
            GameObject holder = new GameObject("WeaponHolder");
            holder.transform.SetParent(camObj.transform);
            holder.transform.localPosition = new Vector3(0.18f, -0.22f, 0.42f);

            WeaponHandling handling = holder.AddComponent<WeaponHandling>();

            // Pistola 3D Detalhada
            GameObject pistol = new GameObject("Pistol_9mm_Tactical");
            pistol.transform.SetParent(holder.transform);
            pistol.transform.localPosition = Vector3.zero;
            pistol.transform.localRotation = Quaternion.identity;

            // Empunhadura / Grip ergonômico
            GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grip.name = "Pistol_Grip";
            grip.transform.SetParent(pistol.transform);
            grip.transform.localPosition = new Vector3(0f, -0.06f, -0.02f);
            grip.transform.localScale = new Vector3(0.028f, 0.12f, 0.046f);
            grip.transform.localRotation = Quaternion.Euler(18f, 0, 0);
            grip.GetComponent<MeshRenderer>().material = matMetal;
            Object.DestroyImmediate(grip.GetComponent<Collider>());

            // Guarda-Mato (Trigger Guard)
            GameObject guard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            guard.name = "TriggerGuard";
            guard.transform.SetParent(pistol.transform);
            guard.transform.localPosition = new Vector3(0f, -0.03f, 0.035f);
            guard.transform.localScale = new Vector3(0.018f, 0.035f, 0.045f);
            guard.GetComponent<MeshRenderer>().material = matMetal;
            Object.DestroyImmediate(guard.GetComponent<Collider>());

            // Gatilho
            GameObject trigger = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trigger.name = "Trigger";
            trigger.transform.SetParent(pistol.transform);
            trigger.transform.localPosition = new Vector3(0f, -0.025f, 0.028f);
            trigger.transform.localScale = new Vector3(0.008f, 0.02f, 0.012f);
            trigger.GetComponent<MeshRenderer>().material = matSlide;
            Object.DestroyImmediate(trigger.GetComponent<Collider>());

            // Armação Superior (Frame Top)
            GameObject frameTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frameTop.name = "Frame_Top";
            frameTop.transform.SetParent(pistol.transform);
            frameTop.transform.localPosition = new Vector3(0f, 0.005f, 0.04f);
            frameTop.transform.localScale = new Vector3(0.030f, 0.022f, 0.17f);
            frameTop.GetComponent<MeshRenderer>().material = matMetal;
            Object.DestroyImmediate(frameTop.GetComponent<Collider>());

            // Ferrolho móvel (Slide com chanfros e ranhuras)
            GameObject slide = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slide.name = "Pistol_Slide";
            slide.transform.SetParent(pistol.transform);
            slide.transform.localPosition = new Vector3(0f, 0.026f, 0.04f);
            slide.transform.localScale = new Vector3(0.031f, 0.028f, 0.18f);
            slide.GetComponent<MeshRenderer>().material = matSlide;
            Object.DestroyImmediate(slide.GetComponent<Collider>());

            // Janela de Ejeção com estojo dourado visível
            GameObject chamberCartridge = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            chamberCartridge.name = "Chamber_Brass";
            chamberCartridge.transform.SetParent(slide.transform);
            chamberCartridge.transform.localPosition = new Vector3(0.25f, 0.1f, -0.05f);
            chamberCartridge.transform.localScale = new Vector3(0.2f, 0.12f, 0.2f);
            chamberCartridge.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            chamberCartridge.GetComponent<MeshRenderer>().material = matBrass;
            Object.DestroyImmediate(chamberCartridge.GetComponent<Collider>());

            // Cano Cilindrico Frontal (Barrel Crown)
            GameObject barrelCrown = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrelCrown.name = "Barrel_Crown";
            barrelCrown.transform.SetParent(pistol.transform);
            barrelCrown.transform.localPosition = new Vector3(0f, 0.022f, 0.135f);
            barrelCrown.transform.localScale = new Vector3(0.016f, 0.02f, 0.016f);
            barrelCrown.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            barrelCrown.GetComponent<MeshRenderer>().material = matMetal;
            Object.DestroyImmediate(barrelCrown.GetComponent<Collider>());

            // Aparelho de Pontaria: Massa de Mira (Front Sight com Fibra Ótica Vermelha)
            GameObject frontSightBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frontSightBase.name = "FrontSight_Blade";
            frontSightBase.transform.SetParent(slide.transform);
            frontSightBase.transform.localPosition = new Vector3(0f, 0.58f, 0.44f);
            frontSightBase.transform.localScale = new Vector3(0.12f, 0.28f, 0.06f);
            frontSightBase.GetComponent<MeshRenderer>().material = matMetal;
            Object.DestroyImmediate(frontSightBase.GetComponent<Collider>());

            GameObject fiberDot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fiberDot.name = "FiberOptic_RedDot";
            fiberDot.transform.SetParent(frontSightBase.transform);
            fiberDot.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            fiberDot.transform.localScale = new Vector3(0.4f, 0.7f, 0.4f);
            fiberDot.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            fiberDot.GetComponent<MeshRenderer>().material = matFiber;
            Object.DestroyImmediate(fiberDot.GetComponent<Collider>());

            // Alça de Mira Traseira em U (Rear Sight)
            GameObject rearSightL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rearSightL.name = "RearSight_Left";
            rearSightL.transform.SetParent(slide.transform);
            rearSightL.transform.localPosition = new Vector3(-0.24f, 0.58f, -0.44f);
            rearSightL.transform.localScale = new Vector3(0.18f, 0.30f, 0.06f);
            rearSightL.GetComponent<MeshRenderer>().material = matMetal;
            Object.DestroyImmediate(rearSightL.GetComponent<Collider>());

            GameObject rearSightR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rearSightR.name = "RearSight_Right";
            rearSightR.transform.SetParent(slide.transform);
            rearSightR.transform.localPosition = new Vector3(0.24f, 0.58f, -0.44f);
            rearSightR.transform.localScale = new Vector3(0.18f, 0.30f, 0.06f);
            rearSightR.GetComponent<MeshRenderer>().material = matMetal;
            Object.DestroyImmediate(rearSightR.GetComponent<Collider>());

            // Cano (Muzzle Point balístico com Height-Over-Bore)
            GameObject muzzle = new GameObject("MuzzlePoint");
            muzzle.transform.SetParent(pistol.transform);
            muzzle.transform.localPosition = new Vector3(0f, 0.022f, 0.145f);

            // Componente Weapon
            Weapon weapon = pistol.AddComponent<Weapon>();
            AudioSource weaponAudio = pistol.AddComponent<AudioSource>();
            weaponAudio.playOnAwake = false;

            SetFieldValue(weapon, "muzzlePoint", muzzle.transform);
            SetFieldValue(weapon, "bulletPrefab", bulletPrefab);
            SetFieldValue(weapon, "bulletHoleDecalPrefab", decalPrefab);
            SetFieldValue(weapon, "slideTransform", slide.transform);
            SetFieldValue(weapon, "audioSource", weaponAudio);
            SetFieldValue(weapon, "fireSound", ProceduralAudio.CreateGunshotClip());
            SetFieldValue(weapon, "dryFireSound", ProceduralAudio.CreateDryFireClip());
            SetFieldValue(weapon, "slideLockSound", ProceduralAudio.CreateSlideLockClip());

            // Configura WeaponHandling (ADS perfeitamente alinhado com a massa de mira de fibra ótica)
            SetFieldValue(handling, "weapon", weapon);
            SetFieldValue(handling, "weaponTransform", holder.transform);
            SetFieldValue(handling, "hipPosition", new Vector3(0.18f, -0.22f, 0.42f));
            SetFieldValue(handling, "adsPosition", new Vector3(0f, -0.0465f, 0.32f));

            return player;
        }

        private static void SetFieldValue(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }
    }
}
