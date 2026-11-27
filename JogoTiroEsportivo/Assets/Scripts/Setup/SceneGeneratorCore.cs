using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
    /// Gerador de mundos e cenários 3D: constrói proceduralmente múltiplos ambientes:
    /// 1. Pistas de IPSC Esportivo (Curta, Média e Longa)
    /// 2. Estande Indoor de Precisão com Cabines e Alvos a 10m/25m
    /// 3. Armazém Tático Urbano / Killhouse estilo Counter-Strike com Contêineres e CQB
    /// Também equipa o jogador com o arsenal escolhido (Pistola 9mm, Dan Wesson .357 e SMG).
    /// </summary>
    public static class SceneGeneratorCore
    {
        public static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying)
                Object.Destroy(obj);
            else
                Object.DestroyImmediate(obj);
        }

        public static void GenerateCompleteShootingRange()
        {
            BuildSelectedEnvironment(GameEnvironmentType.Outdoor_Precision_Stand, SelectedWeaponType.Pistol_9mm);
        }

        public static void BuildSelectedEnvironment(GameEnvironmentType envType, SelectedWeaponType weaponType)
        {
            Debug.Log($"[SceneGenerator] Construindo ambiente '{envType}' com armamento '{weaponType}'...");

            // 1. Limpeza de ambiente anterior
            GameObject oldRoot = GameObject.Find("--- IPSC SHOOTING RANGE ---");
            if (oldRoot != null) SafeDestroy(oldRoot);

            GameObject root = new GameObject("--- IPSC SHOOTING RANGE ---");

            // 2. Garante o Gerenciador Central e Menus
            EnsureManagers(root);

            // 3. Criação de Prefabs Base (Furo de Bala e Projétil)
            GameObject decalPrefab = CreateBulletHoleDecalObject();
            decalPrefab.transform.SetParent(root.transform);
            decalPrefab.SetActive(false);

            GameObject bulletPrefab = CreateBulletPrefab(decalPrefab);
            bulletPrefab.transform.SetParent(root.transform);
            bulletPrefab.SetActive(false);

            // 4. Constrói o Cenário Específico
            List<IPSCTarget> targetsList = new List<IPSCTarget>();
            switch (envType)
            {
                case GameEnvironmentType.IPSC_Outdoor_Short:
                    BuildIPSCShort(root.transform, targetsList);
                    break;
                case GameEnvironmentType.IPSC_Outdoor_Medium:
                    BuildIPSCMedium(root.transform, targetsList);
                    break;
                case GameEnvironmentType.IPSC_Outdoor_Long:
                    BuildIPSCLong(root.transform, targetsList);
                    break;
                case GameEnvironmentType.Indoor_Precision_Stand:
                    BuildIndoorStand(root.transform, targetsList);
                    break;
                case GameEnvironmentType.Tactical_CS_Killhouse:
                    BuildCSKillhouse(root.transform, targetsList);
                    break;
                case GameEnvironmentType.Bodycam_ShootHouse:
                    BuildBodycamShootHouse(root.transform, targetsList);
                    break;
                case GameEnvironmentType.Outdoor_Precision_Stand:
                    BuildOutdoorPrecisionStand(root.transform, targetsList);
                    break;
            }

            // 5. Instancia o Jogador e a Arma Selecionada
            CreatePlayerWithWeapon(root.transform, bulletPrefab, decalPrefab, weaponType);

            // 6. Range Officer Timer
            GameObject roObj = new GameObject("RangeOfficer_Timer");
            roObj.transform.SetParent(root.transform);
            AudioSource roAudio = roObj.AddComponent<AudioSource>();
            roAudio.playOnAwake = false;
            RangeOfficerTimer timer = roObj.AddComponent<RangeOfficerTimer>();

            SetFieldValue(timer, "audioSource", roAudio);
            SetFieldValue(timer, "beepSound", ProceduralAudio.CreateTimerBeepClip());
            SetFieldValue(timer, "stageTargets", targetsList);

            // 7. In-Game HUD Manager
            GameObject hudObj = new GameObject("IPSC_HUD_Manager");
            hudObj.transform.SetParent(root.transform);
            hudObj.AddComponent<IPSCUIManager>();

            // Instancia o HUD minimalista e translúcido estilo CS2 com Canvas e TextMeshPro
            GameplayHUD.CreateProceduralHUD(root.transform);
        }

        private static void EnsureManagers(GameObject root)
        {
            if (TacticalGameManager.Instance == null)
            {
                GameObject gmObj = new GameObject("TacticalGameManager");
                gmObj.AddComponent<TacticalGameManager>();
            }

            if (Object.FindAnyObjectByType<MainMenuManager>() == null)
            {
                GameObject menuObj = new GameObject("MainMenu_UI");
                menuObj.transform.SetParent(root.transform);
                menuObj.AddComponent<MainMenuManager>();
            }

            // Garante que o menu de Arsenal estilo CS2 esteja sempre disponível para alternar com a tecla [B]
            if (Object.FindAnyObjectByType<IPSCSimulator.UI.LoadoutMenuManager>() == null)
            {
                var loadout = IPSCSimulator.UI.LoadoutMenuManager.CreateProceduralLoadoutCanvas();
                loadout.SetMenuOpen(false); // Inicia fechado para o jogador jogar diretamente
            }
        }

        // =========================================================================
        // CENÁRIO 1: PISTA CURTA IPSC (SPEED OPTION)
        // =========================================================================
        private static void BuildIPSCShort(Transform root, List<IPSCTarget> targets)
        {
            SetupSunLighting(true);

            Material matGround = PBRMaterialFactory.CreateRealisticShootRangeGroundPBRMaterial();
            Material matWood = PBRMaterialFactory.CreatePlywoodShootHousePBRMaterial();
            Material matBerm = CreateBermMaterial();
            Material matHazard = CreateHazardMaterial();
            Material matBarrelBlue = CreateBarrelMaterial(new Color(0.12f, 0.35f, 0.75f));
            Material matBarrelYellow = CreateBarrelMaterial(new Color(0.85f, 0.70f, 0.12f));
            Material matSteel = PBRMaterialFactory.CreateGunSteelPBRMaterial(new Color(0.85f, 0.85f, 0.88f));

            GameObject env = new GameObject("Environment_IPSC_Short");
            env.transform.SetParent(root);

            // Chão e Para-balas com Materiais PBR de Alta Fidelidade
            CreateFloor(env.transform, new Vector3(0, -0.25f, 10f), new Vector3(18f, 0.5f, 26f), matGround);
            CreateBerm(env.transform, new Vector3(0, 2.5f, 22f), new Vector3(20f, 5.5f, 4f), matBerm);

            // Paredes táticas da baía com montantes verticais (Framing Studs) inspiradas em Shoot House de Bodycam
            CreateWallWithFraming(env.transform, new Vector3(-8.5f, 1.8f, 10f), new Vector3(0.15f, 3.8f, 24f), matWood);
            CreateWallWithFraming(env.transform, new Vector3(8.5f, 1.8f, 10f), new Vector3(0.15f, 3.8f, 24f), matWood);

            // Vigas de telhado (Roof Rafters) que projetam sombras lineares realistas na pista
            CreateRoofRafters(env.transform, 10f, 24f, 3.8f, matWood);

            // Shooting Box regulamentar
            CreateShootingBox(env.transform, Vector3.zero, matHazard);

            // Coberturas táticas com caixas 3D realistas e barris dinâmicos
            CreateTacticalCover(env.transform, new Vector3(-1.8f, 0f, 3.5f), matWood, matBarrelBlue);
            CreateTacticalCover(env.transform, new Vector3(2.2f, 0f, 4.5f), matWood, matBarrelYellow);
            CreateShootingBench(env.transform, new Vector3(-3f, 0, 0.5f), matWood);

            // Alvos: 3 Papelão IPSC regulamentares + 2 Steel Poppers reativos
            targets.Add(CreateIPSCTarget(root, "Target_01 (7m)", new Vector3(0f, 1.4f, 7f), false, matWood));
            targets.Add(CreateIPSCTarget(root, "Target_02 (11m)", new Vector3(-3.5f, 1.4f, 11f), false, matWood));
            targets.Add(CreateIPSCTarget(root, "NoShoot_01", new Vector3(-2.8f, 1.35f, 10.6f), true, matWood));
            targets.Add(CreateIPSCTarget(root, "Target_03 (15m)", new Vector3(3.2f, 1.4f, 15f), false, matWood));

            CreateSteelPopper(root, "Popper_Left (9m)", new Vector3(-1.6f, 0, 9f), matSteel);
            CreateSteelPopper(root, "Popper_Right (13m)", new Vector3(1.8f, 0, 13f), matSteel);
        }

        // =========================================================================
        // CENÁRIO 2: PISTA MÉDIA IPSC (LABIRINTO TÁTICO COM BIOMBOS)
        // =========================================================================
        private static void BuildIPSCMedium(Transform root, List<IPSCTarget> targets)
        {
            SetupSunLighting(true);

            Material matGround = PBRMaterialFactory.CreateRealisticShootRangeGroundPBRMaterial();
            Material matWood = PBRMaterialFactory.CreatePlywoodShootHousePBRMaterial();
            Material matBerm = CreateBermMaterial();
            Material matHazard = CreateHazardMaterial();
            Material matBarrelBlue = CreateBarrelMaterial(new Color(0.12f, 0.35f, 0.75f));
            Material matBarrelYellow = CreateBarrelMaterial(new Color(0.85f, 0.70f, 0.12f));
            Material matSteel = PBRMaterialFactory.CreateGunSteelPBRMaterial(new Color(0.85f, 0.85f, 0.88f));

            GameObject env = new GameObject("Environment_IPSC_Medium");
            env.transform.SetParent(root);

            CreateFloor(env.transform, new Vector3(0, -0.25f, 14f), new Vector3(22f, 0.5f, 34f), matGround);
            CreateBerm(env.transform, new Vector3(0, 3f, 30f), new Vector3(24f, 6f, 5f), matBerm);
            CreateWallWithFraming(env.transform, new Vector3(-10.5f, 2f, 14f), new Vector3(0.15f, 4f, 34f), matWood);
            CreateWallWithFraming(env.transform, new Vector3(10.5f, 2f, 14f), new Vector3(0.15f, 4f, 34f), matWood);
            CreateRoofRafters(env.transform, 14f, 32f, 4.0f, matWood);

            // Boxes de Tiro 1 e 2 (o atirador se desloca)
            CreateShootingBox(env.transform, Vector3.zero, matHazard);
            CreateShootingBox(env.transform, new Vector3(2.5f, 0, 6.5f), matHazard);

            // Labirinto de Biombos de Madeira com Passagens
            CreateBarricade(env.transform, new Vector3(-2.5f, 1.1f, 4f), new Vector3(3.5f, 2.2f, 0.1f), matWood);
            CreateBarricade(env.transform, new Vector3(3.0f, 1.1f, 8f), new Vector3(4.0f, 2.2f, 0.1f), matWood);
            CreateBarricade(env.transform, new Vector3(-1.0f, 1.1f, 12f), new Vector3(3.5f, 2.2f, 0.1f), matWood);

            // Pilhas de Barris como Cobertura
            CreateBarrel(env.transform, new Vector3(-0.5f, 0.45f, 4.2f), matBarrelYellow);
            CreateBarrel(env.transform, new Vector3(1.2f, 0.45f, 8.2f), matBarrelBlue);
            CreateBarrel(env.transform, new Vector3(1.2f, 1.35f, 8.2f), matBarrelBlue);

            // 5 Alvos de Papelão em ângulos de visão fechados + 2 No-Shoots + 3 Poppers
            targets.Add(CreateIPSCTarget(root, "Target_A (6m)", new Vector3(-3.2f, 1.4f, 6.5f), false, matWood));
            targets.Add(CreateIPSCTarget(root, "NoShoot_A", new Vector3(-2.6f, 1.35f, 6.2f), true, matWood));
            targets.Add(CreateIPSCTarget(root, "Target_B (10m)", new Vector3(1.5f, 1.4f, 10f), false, matWood));
            targets.Add(CreateIPSCTarget(root, "Target_C (14m)", new Vector3(-4.0f, 1.4f, 14f), false, matWood));
            targets.Add(CreateIPSCTarget(root, "NoShoot_B", new Vector3(-3.3f, 1.35f, 13.6f), true, matWood));
            targets.Add(CreateIPSCTarget(root, "Target_D (18m)", new Vector3(3.5f, 1.4f, 18f), false, matWood));
            targets.Add(CreateIPSCTarget(root, "Target_E (22m)", new Vector3(0f, 1.4f, 22f), false, matWood));

            CreateSteelPopper(root, "Popper_Center (11m)", new Vector3(-0.8f, 0, 11f), matSteel);
            CreateSteelPopper(root, "Popper_Right (15m)", new Vector3(4.5f, 0, 15f), matSteel);
            CreateSteelPopper(root, "Popper_Deep (20m)", new Vector3(-2.5f, 0, 20f), matSteel);
        }

        // =========================================================================
        // CENÁRIO 3: PISTA LONGA IPSC (GRANDE TORNEIO 30 METROS)
        // =========================================================================
        private static void BuildIPSCLong(Transform root, List<IPSCTarget> targets)
        {
            SetupSunLighting(true);

            Material matGround = CreateGravelMaterial();
            Material matWood = CreateWoodMaterial();
            Material matBerm = CreateBermMaterial();
            Material matHazard = CreateHazardMaterial();
            Material matBarrelBlue = CreateBarrelMaterial(new Color(0.12f, 0.35f, 0.75f));
            Material matBarrelYellow = CreateBarrelMaterial(new Color(0.85f, 0.70f, 0.12f));
            Material matSteel = CreateSteelMaterial();

            GameObject env = new GameObject("Environment_IPSC_Long");
            env.transform.SetParent(root);

            CreateFloor(env.transform, new Vector3(0, -0.25f, 18f), new Vector3(26f, 0.5f, 42f), matGround);
            CreateBerm(env.transform, new Vector3(0, 3.5f, 38f), new Vector3(28f, 7f, 6f), matBerm);
            CreateWall(env.transform, new Vector3(-12.5f, 2f, 18f), new Vector3(1f, 4f, 42f), matWood);
            CreateWall(env.transform, new Vector3(12.5f, 2f, 18f), new Vector3(1f, 4f, 42f), matWood);

            // 3 Caixas de Tiro demarcadas ao longo da pista
            CreateShootingBox(env.transform, Vector3.zero, matHazard);
            CreateShootingBox(env.transform, new Vector3(-3f, 0, 8f), matHazard);
            CreateShootingBox(env.transform, new Vector3(3f, 0, 16f), matHazard);

            // Diversos Biombos e Barreiras
            CreateBarricade(env.transform, new Vector3(-1.5f, 1.1f, 5f), new Vector3(3.5f, 2.2f, 0.1f), matWood);
            CreateBarricade(env.transform, new Vector3(2.5f, 1.1f, 11f), new Vector3(4.0f, 2.2f, 0.1f), matWood);
            CreateBarricade(env.transform, new Vector3(-2.0f, 1.1f, 19f), new Vector3(4.5f, 2.2f, 0.1f), matWood);

            // Múltiplos barris
            CreateBarrel(env.transform, new Vector3(0.5f, 0.45f, 5.2f), matBarrelYellow);
            CreateBarrel(env.transform, new Vector3(-1.5f, 0.45f, 11.2f), matBarrelBlue);
            CreateBarrel(env.transform, new Vector3(-1.5f, 1.35f, 11.2f), matBarrelBlue);

            // 8 Alvos de Papelão + 4 Poppers
            for (int i = 1; i <= 8; i++)
            {
                float zDist = 6f + (i * 3.2f);
                float xOffset = ((i % 2 == 0) ? 1 : -1) * (1.8f + (i * 0.4f));
                bool isNoShoot = (i == 3 || i == 6);
                targets.Add(CreateIPSCTarget(root, $"Target_{i:D2} ({zDist:F0}m)", new Vector3(xOffset, 1.4f, zDist), isNoShoot, matWood));
            }

            CreateSteelPopper(root, "Popper_A (12m)", new Vector3(-3.5f, 0, 12f), matSteel);
            CreateSteelPopper(root, "Popper_B (18m)", new Vector3(4.2f, 0, 18f), matSteel);
            CreateSteelPopper(root, "Popper_C (24m)", new Vector3(-2.8f, 0, 24f), matSteel);
            CreateSteelPopper(root, "Popper_D (28m)", new Vector3(2.5f, 0, 28f), matSteel);
        }

        // =========================================================================
        // CENÁRIO 4: ESTANDE INDOOR DE PRECISÃO (CABINES E ALVOS EM TRILHOS)
        // =========================================================================
        private static void BuildIndoorStand(Transform root, List<IPSCTarget> targets)
        {
            SetupIndoorLighting();

            Material matConcrete = CreateConcreteMaterial();
            Material matAcoustic = ProceduralMaterials.CreateMaterial(new Color(0.18f, 0.18f, 0.20f), "Mat_AcousticDark", 0.1f, 0.05f);
            Material matWood = CreateWoodMaterial();
            Material matHazard = CreateHazardMaterial();
            Material matSteel = CreateSteelMaterial();

            GameObject env = new GameObject("Environment_Indoor_Precision");
            env.transform.SetParent(root);

            // Chão de Concreto Polido
            CreateFloor(env.transform, new Vector3(0, -0.25f, 14f), new Vector3(14f, 0.5f, 34f), matConcrete);

            // Teto com Vigas de Aço Industriais
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling_Industrial";
            ceiling.transform.SetParent(env.transform);
            ceiling.transform.position = new Vector3(0, 4.2f, 14f);
            ceiling.transform.localScale = new Vector3(14f, 0.3f, 34f);
            ceiling.GetComponent<MeshRenderer>().material = matAcoustic;

            // Paredes Acústicas Escuras
            CreateWall(env.transform, new Vector3(-6.8f, 2f, 14f), new Vector3(0.5f, 4.2f, 34f), matAcoustic);
            CreateWall(env.transform, new Vector3(6.8f, 2f, 14f), new Vector3(0.5f, 4.2f, 34f), matAcoustic);
            CreateWall(env.transform, new Vector3(0, 2f, 30.5f), new Vector3(14f, 4.2f, 0.5f), matAcoustic);

            // 4 Baias de Tiro Individuais
            float[] laneX = { -3.6f, -1.2f, 1.2f, 3.6f };
            for (int i = 0; i < laneX.Length; i++)
            {
                CreateShootingLane(env.transform, laneX[i], i + 1, matWood, matAcoustic);
            }

            // Linhas de Distância Amarelas no Chão (5m, 10m, 15m, 20m, 25m)
            float[] distances = { 5f, 10f, 15f, 20f, 25f };
            foreach (float d in distances)
            {
                CreateDistanceLine(env.transform, d, matHazard);
            }

            // Alvos suspensos nas baias
            targets.Add(CreateIPSCTarget(root, "Alvo_Baia_1 (10m)", new Vector3(-3.6f, 1.5f, 10f), false, matWood));
            targets.Add(CreateIPSCTarget(root, "Alvo_Baia_2 (15m)", new Vector3(-1.2f, 1.5f, 15f), false, matWood));
            targets.Add(CreateIPSCTarget(root, "Alvo_Baia_3 (20m)", new Vector3(1.2f, 1.5f, 20f), false, matWood));
            targets.Add(CreateIPSCTarget(root, "Alvo_Baia_4 (25m)", new Vector3(3.6f, 1.5f, 25f), false, matWood));

            CreateSteelPopper(root, "Chapa_Metal_10m", new Vector3(0f, 0, 10.5f), matSteel);
            CreateSteelPopper(root, "Chapa_Metal_20m", new Vector3(0f, 0, 20.5f), matSteel);
        }

        private static void CreateShootingLane(Transform parent, float x, int laneNum, Material woodMat, Material partitionMat)
        {
            GameObject lane = new GameObject($"ShootingLane_{laneNum}");
            lane.transform.SetParent(parent);
            lane.transform.position = new Vector3(x, 0, 0);

            // Divisória lateral esquerda
            GameObject wallL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallL.transform.SetParent(lane.transform);
            wallL.transform.localPosition = new Vector3(-1.0f, 1.2f, 0.8f);
            wallL.transform.localScale = new Vector3(0.08f, 2.4f, 1.8f);
            wallL.GetComponent<MeshRenderer>().material = partitionMat;

            // Balcão de apoio de armas
            GameObject counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.transform.SetParent(lane.transform);
            counter.transform.localPosition = new Vector3(0, 0.85f, 1.2f);
            counter.transform.localScale = new Vector3(1.9f, 0.08f, 0.5f);
            counter.GetComponent<MeshRenderer>().material = woodMat;
        }

        private static void CreateDistanceLine(Transform parent, float z, Material mat)
        {
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = $"Distance_Line_{z}m";
            line.transform.SetParent(parent);
            line.transform.position = new Vector3(0, 0.01f, z);
            line.transform.localScale = new Vector3(12f, 0.02f, 0.15f);
            line.GetComponent<MeshRenderer>().material = mat;
            SafeDestroy(line.GetComponent<Collider>());
        }

        // =========================================================================
        // CENÁRIO 5: ARMAZÉM TÁTICO CQB (ESTILO COUNTER-STRIKE)
        // =========================================================================
        private static void BuildCSKillhouse(Transform root, List<IPSCTarget> targets)
        {
            SetupTacticalCSLighting();

            Material matConcrete = CreateConcreteMaterial();
            Material matCrate = CreateCrateMaterial();
            Material matWood = CreateWoodMaterial();
            Material matContainerRed = ProceduralMaterials.CreateMaterial(new Color(0.72f, 0.20f, 0.15f), "Mat_ContainerRed", 0.4f, 0.4f);
            Material matContainerBlue = ProceduralMaterials.CreateMaterial(new Color(0.15f, 0.32f, 0.65f), "Mat_ContainerBlue", 0.4f, 0.4f);
            Material matSteel = CreateSteelMaterial();

            GameObject env = new GameObject("Environment_CS_Killhouse");
            env.transform.SetParent(root);

            // Piso de Galpão Industrial
            CreateFloor(env.transform, new Vector3(0, -0.25f, 16f), new Vector3(24f, 0.5f, 36f), matConcrete);

            // Paredes de Armazém Urbano
            Material matWall = ProceduralMaterials.CreateMaterial(new Color(0.35f, 0.35f, 0.38f), "Mat_WarehouseWall", 0.15f, 0.05f);
            CreateWall(env.transform, new Vector3(-11.5f, 2.5f, 16f), new Vector3(1f, 5f, 36f), matWall);
            CreateWall(env.transform, new Vector3(11.5f, 2.5f, 16f), new Vector3(1f, 5f, 36f), matWall);
            CreateWall(env.transform, new Vector3(0, 2.5f, 33.5f), new Vector3(24f, 5f, 1f), matWall);

            // Contêineres de Carga Marítimos de 6 metros (Shipping Containers estilo CS)
            CreateShippingContainer(env.transform, new Vector3(-4f, 1.3f, 9f), new Vector3(2.5f, 2.6f, 6.0f), matContainerRed, 12f);
            CreateShippingContainer(env.transform, new Vector3(4.5f, 1.3f, 18f), new Vector3(2.5f, 2.6f, 6.0f), matContainerBlue, -25f);

            // Pilhas de Caixas de Madeira Militares (Crates)
            CreateCrateStack(env.transform, new Vector3(2.5f, 0, 7f), matCrate);
            CreateCrateStack(env.transform, new Vector3(-3.5f, 0, 18f), matCrate);
            CreateCrateStack(env.transform, new Vector3(0f, 0, 24f), matCrate);

            // Alvos de Ameaça Tática em Esquinas (Corner Clearing)
            targets.Add(CreateIPSCTarget(root, "Ameaça_Esquina_1 (7m)", new Vector3(-1.8f, 1.4f, 7.5f), false, matWood));
            targets.Add(CreateIPSCTarget(root, "Refem_Civil_1", new Vector3(-1.1f, 1.35f, 7.2f), true, matWood));

            targets.Add(CreateIPSCTarget(root, "Ameaça_Atras_Container (12m)", new Vector3(-5.2f, 1.4f, 12.5f), false, matWood));
            targets.Add(CreateIPSCTarget(root, "Ameaça_Corredor_Direito (16m)", new Vector3(3.2f, 1.4f, 16.0f), false, matWood));
            targets.Add(CreateIPSCTarget(root, "Refem_Civil_2", new Vector3(3.9f, 1.35f, 15.6f), true, matWood));

            targets.Add(CreateIPSCTarget(root, "Ameaça_Fundo_Killhouse (24m)", new Vector3(-2.0f, 1.4f, 24.5f), false, matWood));
            targets.Add(CreateIPSCTarget(root, "Ameaça_Janela_Final (28m)", new Vector3(4.0f, 1.4f, 28.0f), false, matWood));

            CreateSteelPopper(root, "Popper_Ameaça_Metal", new Vector3(1.2f, 0, 13f), matSteel);
            CreateSteelPopper(root, "Popper_Ameaça_Fundo", new Vector3(-4.5f, 0, 22f), matSteel);
        }

        private static void CreateShippingContainer(Transform parent, Vector3 pos, Vector3 scale, Material mat, float yRot)
        {
            GameObject container = GameObject.CreatePrimitive(PrimitiveType.Cube);
            container.name = "Shipping_Container_6m";
            container.transform.SetParent(parent);
            container.transform.position = pos;
            container.transform.localScale = scale;
            container.transform.rotation = Quaternion.Euler(0, yRot, 0);
            container.GetComponent<MeshRenderer>().material = mat;
        }

        private static void CreateCrateStack(Transform parent, Vector3 basePos, Material mat)
        {
            GameObject stack = new GameObject("Crates_Stack");
            stack.transform.SetParent(parent);
            stack.transform.position = basePos;

            // Caixa 1 (baixo)
            CreateSingleCrate(stack.transform, new Vector3(0, 0.55f, 0), new Vector3(1.1f, 1.1f, 1.1f), mat);
            // Caixa 2 (ao lado)
            CreateSingleCrate(stack.transform, new Vector3(1.15f, 0.55f, 0.1f), new Vector3(1.1f, 1.1f, 1.1f), mat);
            // Caixa 3 (em cima)
            CreateSingleCrate(stack.transform, new Vector3(0.5f, 1.65f, 0.05f), new Vector3(1.0f, 1.0f, 1.0f), mat);
        }

        private static void CreateSingleCrate(Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "Military_Crate";
            crate.transform.SetParent(parent);
            crate.transform.localPosition = pos;
            crate.transform.localScale = scale;
            crate.GetComponent<MeshRenderer>().material = mat;

            Rigidbody rb = crate.AddComponent<Rigidbody>();
            rb.mass = 35f;
            rb.linearDamping = 0.8f;
            rb.angularDamping = 0.8f;
        }

        // =========================================================================
        // CENÁRIO 6: KILLHOUSE TÁTICO POLICIAL EM COMPENSADO (ESTILO BODYCAM / UNRECORD)
        // =========================================================================
        private static void BuildBodycamShootHouse(Transform root, List<IPSCTarget> targets)
        {
            SetupBodycamWarehouseLighting();

            Material matPlywood = PBRMaterialFactory.CreatePlywoodShootHousePBRMaterial();
            Material matFloor = PBRMaterialFactory.CreateWarehouseConcretePBRMaterial();
            Material matStud = ProceduralMaterials.CreateMaterial(new Color(0.68f, 0.48f, 0.28f), "Mat_PineStud_2x4", 0.15f, 0.0f);
            Material matTruss = ProceduralMaterials.CreateTrussSteelMaterial();
            Material matRoof = ProceduralMaterials.CreateMaterial(new Color(0.20f, 0.22f, 0.24f), "Mat_WarehouseRoof", 0.2f, 0.5f);
            Material matSteel = CreateSteelMaterial();

            GameObject env = new GameObject("Environment_Bodycam_ShootHouse");
            env.transform.SetParent(root);

            // 1. Piso de concreto industrial polido
            CreateFloor(env.transform, new Vector3(0, -0.1f, 15f), new Vector3(22f, 0.2f, 32f), matFloor);

            // 2. Galpão industrial externo (Teto alto com clarabóias)
            CreateWall(env.transform, new Vector3(-10.5f, 3.2f, 15f), new Vector3(0.5f, 6.5f, 32f), matRoof);
            CreateWall(env.transform, new Vector3(10.5f, 3.2f, 15f), new Vector3(0.5f, 6.5f, 32f), matRoof);
            CreateWall(env.transform, new Vector3(0, 3.2f, 30.5f), new Vector3(22f, 6.5f, 0.5f), matRoof);
            CreateWall(env.transform, new Vector3(0, 3.2f, -0.5f), new Vector3(22f, 6.5f, 0.5f), matRoof);

            // Teto com aberturas de clarabóia
            CreateWall(env.transform, new Vector3(0, 6.5f, 15f), new Vector3(22f, 0.3f, 32f), matRoof);

            // 3. Treliças de Aço Industriais no Teto (Roof Trusses)
            for (float z = 3f; z <= 27f; z += 6f)
            {
                CreateRoofTruss(env.transform, new Vector3(0, 5.8f, z), 20f, matTruss);
            }

            // 4. Lâmpadas Fluorescentes Suspensas de Galpão
            for (float z = 4f; z <= 26f; z += 6f)
            {
                CreateHangingLight(env.transform, new Vector3(0f, 5.2f, z));
                CreateHangingLight(env.transform, new Vector3(-4.5f, 5.2f, z));
                CreateHangingLight(env.transform, new Vector3(4.5f, 5.2f, z));
            }

            // 5. Paredes Táticas do Shoot House em Madeira Compensada (OSB com vigas 2x4)
            // Corredor Principal Central (Idêntico ao da Imagem 3!)
            CreateFramedPlywoodWall(env.transform, new Vector3(-1.8f, 1.4f, 6f), new Vector3(0.12f, 2.8f, 12f), matPlywood, matStud);
            CreateFramedPlywoodWall(env.transform, new Vector3(1.8f, 1.4f, 4f), new Vector3(0.12f, 2.8f, 8f), matPlywood, matStud);

            // Abertura de Porta (Doorway) para a Sala 1 (à direita)
            CreateFramedPlywoodWall(env.transform, new Vector3(5.5f, 1.4f, 8f), new Vector3(7.2f, 2.8f, 0.12f), matPlywood, matStud);
            CreateFramedPlywoodWall(env.transform, new Vector3(9.0f, 1.4f, 13f), new Vector3(0.12f, 2.8f, 10f), matPlywood, matStud);
            CreateFramedPlywoodWall(env.transform, new Vector3(5.5f, 1.4f, 18f), new Vector3(7.2f, 2.8f, 0.12f), matPlywood, matStud);

            // Sala 2 (à esquerda do corredor central)
            CreateFramedPlywoodWall(env.transform, new Vector3(-5.5f, 1.4f, 12f), new Vector3(7.2f, 2.8f, 0.12f), matPlywood, matStud);
            CreateFramedPlywoodWall(env.transform, new Vector3(-9.0f, 1.4f, 17f), new Vector3(0.12f, 2.8f, 10f), matPlywood, matStud);
            CreateFramedPlywoodWall(env.transform, new Vector3(-5.5f, 1.4f, 22f), new Vector3(7.2f, 2.8f, 0.12f), matPlywood, matStud);

            // Corredor em L e Sala Final
            CreateFramedPlywoodWall(env.transform, new Vector3(0f, 1.4f, 18f), new Vector3(0.12f, 2.8f, 8f), matPlywood, matStud);
            CreateFramedPlywoodWall(env.transform, new Vector3(3.5f, 1.4f, 22f), new Vector3(7.0f, 2.8f, 0.12f), matPlywood, matStud);
            CreateFramedPlywoodWall(env.transform, new Vector3(0f, 1.4f, 27f), new Vector3(14.0f, 2.8f, 0.12f), matPlywood, matStud);

            // 6. Alvos Táticos de Treinamento SWAT (Ameaças em Esquinas & Reféns Civis)
            targets.Add(CreateIPSCTarget(root, "Ameaça_Corredor_Centro (12m)", new Vector3(0.5f, 1.4f, 12.0f), false, matPlywood));

            // Sala 1: Varredura de Cômodo (Room Clearing)
            targets.Add(CreateIPSCTarget(root, "Ameaça_Esquina_Direita (9m)", new Vector3(7.5f, 1.4f, 10.5f), false, matPlywood));
            targets.Add(CreateIPSCTarget(root, "Refem_Civil_Sala1", new Vector3(6.5f, 1.35f, 10.0f), true, matPlywood));
            CreateSteelPopper(root, "Popper_Aço_Sala1", new Vector3(4.5f, 0, 15f), matSteel);

            // Sala 2: Ameaça atrás de meia-parede
            CreateFramedPlywoodWall(env.transform, new Vector3(-4.5f, 0.6f, 15f), new Vector3(0.12f, 1.2f, 4f), matPlywood, matStud);
            targets.Add(CreateIPSCTarget(root, "Ameaça_MeiaParede (16m)", new Vector3(-6.5f, 1.4f, 16.5f), false, matPlywood));
            targets.Add(CreateIPSCTarget(root, "Refem_Civil_Sala2", new Vector3(-7.5f, 1.35f, 19.5f), true, matPlywood));

            // Sala Final: Confronto com baterias de alvos
            targets.Add(CreateIPSCTarget(root, "Ameaça_Final_1 (24m)", new Vector3(-2.5f, 1.4f, 24.5f), false, matPlywood));
            targets.Add(CreateIPSCTarget(root, "Ameaça_Final_2 (25m)", new Vector3(2.5f, 1.4f, 25.0f), false, matPlywood));
            CreateSteelPopper(root, "Popper_Aço_Final", new Vector3(0f, 0, 26.5f), matSteel);
        }

        // =========================================================================
        // CENÁRIO 7: STAND DE TIRO ABERTO COM BERMA E FOCO VISUAL (ILUSTRAÇÃO)
        // =========================================================================
        private static void BuildOutdoorPrecisionStand(Transform root, List<IPSCTarget> targets)
        {
            SetupSunLighting(true);

            Material matGround = PBRMaterialFactory.CreateRealisticShootRangeGroundPBRMaterial();
            Material matBerm = PBRMaterialFactory.CreateOutdoorBermEarthPBRMaterial();
            Material matFoliage = PBRMaterialFactory.CreateFoliageGreenPBRMaterial();
            Material matWood = PBRMaterialFactory.CreatePlywoodShootHousePBRMaterial();
            Material matHazard = CreateHazardMaterial();
            Material matSteel = PBRMaterialFactory.CreateGunSteelPBRMaterial(new Color(0.85f, 0.85f, 0.88f));

            GameObject env = new GameObject("Environment_Outdoor_Precision_Stand");
            env.transform.SetParent(root);

            // 1. Chão da Linha de Tiro (Cascalho claro e terra batida de clube de tiro)
            CreateFloor(env.transform, new Vector3(0, -0.25f, 14f), new Vector3(18f, 0.5f, 32f), matGround);

            // 2. Grande Barranco de Contenção Balística (Earth Berm / Backstop) ao Fundo
            CreateBerm(env.transform, new Vector3(0, 3.2f, 22f), new Vector3(22f, 6.4f, 5.0f), matBerm);

            // Rampa/Talude frontal de terra do barranco
            GameObject slope = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slope.name = "Berm_FrontSlope";
            slope.transform.SetParent(env.transform);
            slope.transform.position = new Vector3(0f, 1.6f, 19.5f);
            slope.transform.rotation = Quaternion.Euler(30f, 0f, 0f);
            slope.transform.localScale = new Vector3(22f, 0.8f, 4.2f);
            slope.GetComponent<MeshRenderer>().material = matBerm;

            // 3. Copa de Árvores e Vegetação Verde no Topo e Atrás do Barranco (Conforme Ilustração)
            CreateOutdoorFoliageCrown(env.transform, new Vector3(0, 6.6f, 23.5f), new Vector3(24f, 3.2f, 4.0f), matFoliage);

            // 4. Bermas de Terra Laterais de Segurança da Baía de Tiro
            CreateBerm(env.transform, new Vector3(-9.5f, 2.0f, 12f), new Vector3(3.0f, 4.2f, 28f), matBerm);
            CreateBerm(env.transform, new Vector3(9.5f, 2.0f, 12f), new Vector3(3.0f, 4.2f, 28f), matBerm);

            // Vegetação lateral nos topos das bermas laterais
            CreateOutdoorFoliageCrown(env.transform, new Vector3(-9.5f, 4.2f, 12f), new Vector3(2.5f, 1.8f, 26f), matFoliage);
            CreateOutdoorFoliageCrown(env.transform, new Vector3(9.5f, 4.2f, 12f), new Vector3(2.5f, 1.8f, 26f), matFoliage);

            // 5. Shooting Box (Caixa regulamentar de onde o atirador dispara)
            CreateShootingBox(env.transform, Vector3.zero, matHazard);

            // Bancada de apoio rústica de madeira à esquerda
            CreateShootingBench(env.transform, new Vector3(-2.8f, 0, 0.6f), matWood);

            // 6. Alvo Central com FOCO VISUAL (Exatamente como na ilustração enviada) a 7 metros
            targets.Add(CreateIPSCTarget(root, "Target_FocoVisual_Centro (7m)", new Vector3(0f, 1.4f, 7.0f), false, matWood, true));

            // Alvos de treino tático nas distâncias de 11m e 15m
            targets.Add(CreateIPSCTarget(root, "Target_Esquerda (11m)", new Vector3(-2.8f, 1.4f, 11.0f), false, matWood, false));
            targets.Add(CreateIPSCTarget(root, "Target_Direita (15m)", new Vector3(2.8f, 1.4f, 15.0f), false, matWood, false));

            // Popper reativo de aço a 9 metros
            CreateSteelPopper(root, "Popper_Aco_Centro (9m)", new Vector3(1.6f, 0, 9.0f), matSteel);
        }

        private static void CreateOutdoorFoliageCrown(Transform parent, Vector3 center, Vector3 size, Material matFoliage)
        {
            GameObject foliageGroup = new GameObject("Foliage_Trees_Ridge");
            foliageGroup.transform.SetParent(parent);
            foliageGroup.transform.position = center;

            // Cria copas orgânicas de árvores e arbustos usando esferas com variação natural
            int bushCount = Mathf.Max(3, Mathf.RoundToInt(size.x / 2.2f));
            float startX = -size.x * 0.45f;
            float stepX = size.x / bushCount;

            for (int i = 0; i < bushCount; i++)
            {
                GameObject bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bush.name = $"TreeCrown_{i}";
                bush.transform.SetParent(foliageGroup.transform);
                float posX = startX + (i * stepX) + Random.Range(-0.4f, 0.4f);
                float posY = Random.Range(-0.3f, 0.4f);
                float posZ = Random.Range(-0.5f, 0.5f);
                bush.transform.localPosition = new Vector3(posX, posY, posZ);
                float scale = Random.Range(2.2f, 3.4f);
                bush.transform.localScale = new Vector3(scale * 1.2f, scale * 0.9f, scale * 1.1f);
                bush.GetComponent<MeshRenderer>().material = matFoliage;
                SafeDestroy(bush.GetComponent<Collider>());
            }
        }

        private static void CreateFramedPlywoodWall(Transform parent, Vector3 pos, Vector3 scale, Material matPlywood, Material matStud)
        {
            GameObject wallGroup = new GameObject("Plywood_Wall_Framed");
            wallGroup.transform.SetParent(parent);
            wallGroup.transform.position = pos;

            // Painel central de madeira compensada OSB com escala proporcional de textura
            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "OSB_Panel";
            panel.transform.SetParent(wallGroup.transform);
            panel.transform.localPosition = Vector3.zero;
            panel.transform.localScale = scale;

            Material instMat = new Material(matPlywood);
            float length = scale.x > scale.z ? scale.x : scale.z;
            bool alongX = scale.x > scale.z;
            float tileX = Mathf.Max(1f, length / 1.2f);
            float tileY = Mathf.Max(1f, scale.y / 2.4f);
            instMat.mainTextureScale = new Vector2(tileX, tileY);
            if (instMat.HasProperty("_BumpMap")) instMat.SetTextureScale("_BumpMap", new Vector2(tileX, tileY));
            panel.GetComponent<MeshRenderer>().material = instMat;

            // Viga superior de acabamento (Top Header Plate)
            GameObject topPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topPlate.name = "Stud_TopHeader";
            topPlate.transform.SetParent(wallGroup.transform);
            topPlate.transform.localPosition = new Vector3(0, scale.y * 0.5f + 0.04f, 0);
            topPlate.transform.localScale = new Vector3(alongX ? scale.x : 0.16f, 0.08f, alongX ? 0.16f : scale.z);
            topPlate.GetComponent<MeshRenderer>().material = matStud;
            SafeDestroy(topPlate.GetComponent<Collider>());

            // Montantes verticais 2x4 (Framing Studs) a cada 1.2m criando profundidade e sombras lineares reais
            int studCount = Mathf.Max(2, Mathf.RoundToInt(length / 1.2f) + 1);
            for (int s = 0; s < studCount; s++)
            {
                float t = ((float)s / (studCount - 1)) - 0.5f;
                GameObject stud = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stud.name = $"Stud_2x4_{s}";
                stud.transform.SetParent(wallGroup.transform);
                Vector3 studLocalPos = alongX ? new Vector3(t * (length - 0.12f), 0, 0.07f) : new Vector3(0.07f, 0, t * (length - 0.12f));
                stud.transform.localPosition = studLocalPos;
                stud.transform.localScale = alongX ? new Vector3(0.06f, scale.y, 0.08f) : new Vector3(0.08f, scale.y, 0.06f);
                stud.GetComponent<MeshRenderer>().material = matStud;
                SafeDestroy(stud.GetComponent<Collider>());
            }
        }

        private static void CreateRoofTruss(Transform parent, Vector3 pos, float spanWidth, Material matTruss)
        {
            GameObject truss = new GameObject("Roof_Truss");
            truss.transform.SetParent(parent);
            truss.transform.position = pos;

            // Viga horizontal inferior
            GameObject bottomBeam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bottomBeam.transform.SetParent(truss.transform);
            bottomBeam.transform.localPosition = Vector3.zero;
            bottomBeam.transform.localScale = new Vector3(spanWidth, 0.15f, 0.15f);
            bottomBeam.GetComponent<MeshRenderer>().material = matTruss;
            SafeDestroy(bottomBeam.GetComponent<Collider>());

            // Viga angular esquerda
            GameObject leftSlope = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftSlope.transform.SetParent(truss.transform);
            leftSlope.transform.localPosition = new Vector3(-spanWidth * 0.25f, 0.5f, 0);
            leftSlope.transform.localScale = new Vector3(spanWidth * 0.55f, 0.12f, 0.12f);
            leftSlope.transform.localRotation = Quaternion.Euler(0, 0, 12f);
            leftSlope.GetComponent<MeshRenderer>().material = matTruss;
            SafeDestroy(leftSlope.GetComponent<Collider>());

            // Viga angular direita
            GameObject rightSlope = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightSlope.transform.SetParent(truss.transform);
            rightSlope.transform.localPosition = new Vector3(spanWidth * 0.25f, 0.5f, 0);
            rightSlope.transform.localScale = new Vector3(spanWidth * 0.55f, 0.12f, 0.12f);
            rightSlope.transform.localRotation = Quaternion.Euler(0, 0, -12f);
            rightSlope.GetComponent<MeshRenderer>().material = matTruss;
            SafeDestroy(rightSlope.GetComponent<Collider>());
        }

        private static void CreateHangingLight(Transform parent, Vector3 pos)
        {
            GameObject lightObj = new GameObject("Hanging_Warehouse_Light");
            lightObj.transform.SetParent(parent);
            lightObj.transform.position = pos;

            // Fio de sustentação
            GameObject wire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wire.transform.SetParent(lightObj.transform);
            wire.transform.localPosition = new Vector3(0, 0.4f, 0);
            wire.transform.localScale = new Vector3(0.015f, 0.4f, 0.015f);
            wire.GetComponent<MeshRenderer>().material = ProceduralMaterials.CreateMaterial(Color.black, "Mat_Wire");
            SafeDestroy(wire.GetComponent<Collider>());

            // Calha fluorescente emissiva
            GameObject fixture = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fixture.transform.SetParent(lightObj.transform);
            fixture.transform.localPosition = Vector3.zero;
            fixture.transform.localScale = new Vector3(1.4f, 0.08f, 0.25f);
            fixture.GetComponent<MeshRenderer>().material = ProceduralMaterials.CreateEmissiveMaterial(new Color(1f, 0.98f, 0.92f), "Mat_Fluorescent_Tube", 3.0f);
            SafeDestroy(fixture.GetComponent<Collider>());

            // Luz pontual para iluminar o corredor
            Light pl = lightObj.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.color = new Color(1f, 0.96f, 0.88f);
            pl.range = 10f;
            pl.intensity = 1.4f;
        }

        private static void SetupBodycamWarehouseLighting()
        {
            Light sun = Object.FindAnyObjectByType<Light>();
            if (sun == null)
            {
                GameObject sunObj = new GameObject("Directional_Sun");
                sun = sunObj.AddComponent<Light>();
                sun.type = LightType.Directional;
            }

            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.intensity = 1.30f;
            sun.transform.rotation = Quaternion.Euler(52f, -38f, 0f);
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.85f;
            sun.shadowBias = 0.03f;
            sun.shadowNormalBias = 0.25f;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.60f, 0.68f);
            RenderSettings.ambientEquatorColor = new Color(0.48f, 0.46f, 0.44f);
            RenderSettings.ambientGroundColor = new Color(0.32f, 0.30f, 0.28f);

#if UNITY_EDITOR
            Material skyMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Starter Assets/Sample/Environment/Art/Skybox/SkyboxLiteWarm.mat");
            if (skyMat != null)
            {
                RenderSettings.skybox = skyMat;
            }
#endif

            GameObject probeObj = GameObject.Find("Bodycam_Reflection_Probe");
            if (probeObj == null)
            {
                probeObj = new GameObject("Bodycam_Reflection_Probe");
                ReflectionProbe probe = probeObj.AddComponent<ReflectionProbe>();
                probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
                probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;
                probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
                probe.size = new Vector3(40f, 15f, 50f);
                probeObj.transform.position = new Vector3(0, 3f, 15f);
            }
        }

        // =========================================================================
        // JOGADOR E SELEÇÃO DE ARMAS
        // =========================================================================
        private static void CreatePlayerWithWeapon(Transform parent, GameObject bulletPrefab, GameObject decalPrefab, SelectedWeaponType weaponType)
        {
            GameObject player = new GameObject("Player");
            player.transform.SetParent(parent);
            player.transform.position = new Vector3(0f, 1.0f, 0f);

            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0, 0.9f, 0);

            PlayerController controller = player.AddComponent<PlayerController>();

            // Câmera do Jogador (Posicionada na altura do colete para efeito Bodycam)
            GameObject camObj = new GameObject("PlayerCamera");
            camObj.transform.SetParent(player.transform);
            camObj.transform.localPosition = new Vector3(0f, 1.55f, 0.05f);
            Camera cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            camObj.AddComponent<AudioListener>();
            cam.fieldOfView = 78f; // Visão grande angular tática de Bodycam
            cam.nearClipPlane = 0.02f;
            cam.clearFlags = CameraClearFlags.Skybox;

            // Desativa câmeras antigas
            foreach (Camera c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (c != cam && c != null) c.gameObject.SetActive(false);
            }

            SetFieldValue(controller, "playerCameraTransform", camObj.transform);

            // Suporte da Arma (calibrado para visão FPS tática centralizada e imersiva)
            GameObject holder = new GameObject("WeaponHolder");
            holder.transform.SetParent(camObj.transform);
            holder.transform.localPosition = new Vector3(0.085f, -0.125f, 0.30f);

            WeaponHandling handling = holder.AddComponent<WeaponHandling>();
            WeaponProceduralMotion procMotion = holder.AddComponent<WeaponProceduralMotion>();
            SetFieldValue(procMotion, "playerCamera", cam);
            SetFieldValue(procMotion, "characterController", cc);
            SetFieldValue(procMotion, "weaponTransform", holder.transform);

            // Adiciona o controlador de câmera corporal Bodycam com Free Aim e inércia física
            BodycamCameraController bodycam = camObj.AddComponent<BodycamCameraController>();
            SetFieldValue(bodycam, "playerBody", player.transform);
            SetFieldValue(bodycam, "weaponHolder", holder.transform);
            SetFieldValue(bodycam, "cam", cam);

            // Adiciona pós-processamento cinemático de lente Bodycam (Fisheye, ACES, Aberração Cromática e Granulação)
            camObj.AddComponent<BodycamPostProcessing>();

            // Instancia ou constrói a arma selecionada
            AttachSelectedWeapon(holder, handling, bulletPrefab, decalPrefab, weaponType);
        }

        private static void AttachSelectedWeapon(GameObject holder, WeaponHandling handling, GameObject bulletPrefab, GameObject decalPrefab, SelectedWeaponType weaponType)
        {
            Material matMetal = ProceduralMaterials.CreateMaterial(new Color(0.10f, 0.10f, 0.11f), "Mat_GunBlack", 0.6f, 0.7f);
            Material matSlide = ProceduralMaterials.CreateMaterial(new Color(0.18f, 0.18f, 0.20f), "Mat_GunSlide", 0.75f, 0.85f);
            Material matBrass = ProceduralMaterials.CreateMaterial(new Color(0.92f, 0.78f, 0.32f), "Mat_BrassGold", 0.85f, 0.95f);
            Material matFiber = ProceduralMaterials.CreateEmissiveMaterial(new Color(1f, 0.1f, 0.05f), "Mat_FiberOpticGlow", 2.0f);

            GameObject gunRoot = new GameObject($"Weapon_{weaponType}");
            gunRoot.transform.SetParent(holder.transform);
            gunRoot.transform.localPosition = Vector3.zero;
            gunRoot.transform.localRotation = Quaternion.identity;

            GameObject muzzleObj = new GameObject("MuzzlePoint");
            muzzleObj.transform.SetParent(gunRoot.transform);

            Transform slideObj = null;

            int capacity = 17;
            float velocity = 380f;
            bool isAuto = false;
            float recoilMult = 1.0f;
            Vector3 adsPos = new Vector3(0f, -0.024f, 0.18f);
            Vector3 adsRot = new Vector3(-3.2f, 0f, 0f);
            Vector3 hipPos = new Vector3(0.18f, -0.22f, 0.42f);

            GameObject modelInstance = null;

#if UNITY_EDITOR
            string prefabPath = null;
            switch (weaponType)
            {
                case SelectedWeaponType.Pistol_Beretta_M9T_Black:
                    prefabPath = "Assets/GILD/FA FPS Weapons - Free/FA M9T/Prefabs/Built-In/FA M9T (Black).prefab";
                    break;
                case SelectedWeaponType.Pistol_Beretta_M9T_Tan:
                    prefabPath = "Assets/GILD/FA FPS Weapons - Free/FA M9T/Prefabs/Built-In/FA M9T (Tan).prefab";
                    break;
                case SelectedWeaponType.Pistol_Tactical_Glock:
                    prefabPath = "Assets/Low Poly Pistol Weapon Pack 1/Prefabs/Weapons/Pistol_A.prefab";
                    break;
                case SelectedWeaponType.Pistol_Combat_Custom:
                    prefabPath = "Assets/Low Poly Pistol Weapon Pack 1/Prefabs/Weapons/Pistol_B.prefab";
                    break;

                case SelectedWeaponType.Revolver_DanWesson_357:
                    prefabPath = "Assets/3D Models/Props/Weapons/DWm715 FPS Model/Prefabs/DW_Set0.prefab";
                    break;
                case SelectedWeaponType.Revolver_Superhawk_Tactical:
                    prefabPath = "Assets/GILD/FA FPS Weapons - Free/FA Superhawk/Prefabs/Built-In/FA Superhawk.prefab";
                    break;

                case SelectedWeaponType.SMG_Strongarm_Tactical:
                    prefabPath = "Assets/GILD/FA FPS Weapons - Free/WF Strongarm/Prefabs/Built-In/WF Strongarm (Black).prefab";
                    break;
                case SelectedWeaponType.SMG_MP_Vector:
                    prefabPath = "Assets/Low Poly SMG Weapon Pack 2/Prefabs/Weapons/SMG_F.prefab";
                    break;
                case SelectedWeaponType.SMG_P90_Compact:
                    prefabPath = "Assets/Low Poly SMG Weapon Pack 2/Prefabs/Weapons/SMG_G.prefab";
                    break;

                case SelectedWeaponType.Rifle_AKM_Tactical:
                    prefabPath = "Assets/Weapons_ChamferZone/AKM/WPN_AKM.FBX";
                    break;
            }

            if (!string.IsNullOrEmpty(prefabPath))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null)
                {
                    modelInstance = Object.Instantiate(prefab, gunRoot.transform);
                    modelInstance.name = "Real_3D_Model";
                    // Remove eventuais colliders para não colidir com o jogador
                    foreach (Collider col in modelInstance.GetComponentsInChildren<Collider>())
                    {
                        SafeDestroy(col);
                    }
                }
            }
#endif

            Transform magObj = null;

            if (modelInstance != null)
            {
                switch (weaponType)
                {
                    case SelectedWeaponType.Pistol_Beretta_M9T_Black:
                    case SelectedWeaponType.Pistol_Beretta_M9T_Tan:
                        capacity = 17;
                        velocity = 380f;
                        isAuto = false;
                        recoilMult = 1.0f;
                        modelInstance.transform.localPosition = Vector3.zero;
                        modelInstance.transform.localRotation = Quaternion.identity;
                        modelInstance.transform.localScale = Vector3.one;
                        muzzleObj.transform.localPosition = new Vector3(0f, 0.028f, 0.22f);
                        adsPos = new Vector3(0f, -0.024f, 0.18f); // Eleva a arma aos olhos e nivelamento de alça/massa
                        adsRot = new Vector3(-3.2f, 0f, 0f);      // Pitch horizontal no eixo X (ferrolho alinhado ao horizonte)
                        hipPos = new Vector3(0.085f, -0.125f, 0.28f);
                        slideObj = FindChildRecursive(modelInstance.transform, "Slide", "Pistol_A_Bolt");
                        magObj = FindChildRecursive(modelInstance.transform, "Magazine", "Pistol_A_Mag");
                        break;

                    case SelectedWeaponType.Pistol_Tactical_Glock:
                        capacity = 17;
                        velocity = 375f;
                        isAuto = false;
                        recoilMult = 0.95f;
                        modelInstance.transform.localPosition = Vector3.zero;
                        modelInstance.transform.localRotation = Quaternion.identity;
                        modelInstance.transform.localScale = Vector3.one * 1.15f;
                        muzzleObj.transform.localPosition = new Vector3(0f, 0.030f, 0.22f);
                        adsPos = new Vector3(0f, -0.024f, 0.18f);
                        adsRot = new Vector3(-3.0f, 0f, 0f);
                        hipPos = new Vector3(0.085f, -0.125f, 0.28f);
                        slideObj = FindChildRecursive(modelInstance.transform, "Slide", "Pistol_A_Bolt");
                        magObj = FindChildRecursive(modelInstance.transform, "Magazine", "Pistol_A_Mag");
                        break;

                    case SelectedWeaponType.Pistol_Combat_Custom:
                        capacity = 15;
                        velocity = 360f;
                        isAuto = false;
                        recoilMult = 1.15f;
                        modelInstance.transform.localPosition = Vector3.zero;
                        modelInstance.transform.localRotation = Quaternion.identity;
                        modelInstance.transform.localScale = Vector3.one * 1.15f;
                        muzzleObj.transform.localPosition = new Vector3(0f, 0.032f, 0.24f);
                        adsPos = new Vector3(0f, -0.025f, 0.18f);
                        adsRot = new Vector3(-3.0f, 0f, 0f);
                        hipPos = new Vector3(0.085f, -0.125f, 0.28f);
                        slideObj = FindChildRecursive(modelInstance.transform, "Slide", "Pistol_B_Bolt");
                        magObj = FindChildRecursive(modelInstance.transform, "Magazine", "Pistol_B_Mag");
                        break;

                    case SelectedWeaponType.Revolver_DanWesson_357:
                        capacity = 6;
                        velocity = 450f;
                        isAuto = false;
                        recoilMult = 1.8f;
                        modelInstance.transform.localPosition = Vector3.zero;
                        modelInstance.transform.localRotation = Quaternion.identity;
                        modelInstance.transform.localScale = Vector3.one * 0.95f;
                        muzzleObj.transform.localPosition = new Vector3(0f, 0.035f, 0.28f);
                        adsPos = new Vector3(0f, -0.030f, 0.18f);
                        adsRot = new Vector3(-2.8f, 0f, 0f);
                        hipPos = new Vector3(0.085f, -0.130f, 0.28f);
                        slideObj = FindChildRecursive(modelInstance.transform, "DW_Hummer", "Hammer");
                        magObj = FindChildRecursive(modelInstance.transform, "DW_MagazineAssembly", "Cylinder", "Speedloader");
                        break;

                    case SelectedWeaponType.Revolver_Superhawk_Tactical:
                        capacity = 6;
                        velocity = 480f;
                        isAuto = false;
                        recoilMult = 2.0f;
                        modelInstance.transform.localPosition = Vector3.zero;
                        modelInstance.transform.localRotation = Quaternion.identity;
                        modelInstance.transform.localScale = Vector3.one;
                        muzzleObj.transform.localPosition = new Vector3(0f, 0.038f, 0.32f);
                        adsPos = new Vector3(0f, -0.032f, 0.18f);
                        adsRot = new Vector3(-2.8f, 0f, 0f);
                        hipPos = new Vector3(0.085f, -0.130f, 0.28f);
                        slideObj = FindChildRecursive(modelInstance.transform, "Hammer", "DW_Hummer");
                        magObj = FindChildRecursive(modelInstance.transform, "Cylinder", "SH Speedloader");
                        break;

                    case SelectedWeaponType.SMG_Strongarm_Tactical:
                        capacity = 30;
                        velocity = 400f;
                        isAuto = true;
                        recoilMult = 0.9f;
                        modelInstance.transform.localPosition = Vector3.zero;
                        modelInstance.transform.localRotation = Quaternion.identity;
                        modelInstance.transform.localScale = Vector3.one;
                        muzzleObj.transform.localPosition = new Vector3(0f, 0.035f, 0.45f);
                        adsPos = new Vector3(0f, -0.038f, 0.22f);
                        adsRot = new Vector3(-2.0f, 0f, 0f);
                        hipPos = new Vector3(0.090f, -0.135f, 0.30f);
                        slideObj = FindChildRecursive(modelInstance.transform, "Bolt", "Slide");
                        magObj = FindChildRecursive(modelInstance.transform, "Magazine", "Mag");
                        break;

                    case SelectedWeaponType.SMG_MP_Vector:
                        capacity = 30;
                        velocity = 390f;
                        isAuto = true;
                        recoilMult = 0.85f;
                        modelInstance.transform.localPosition = Vector3.zero;
                        modelInstance.transform.localRotation = Quaternion.identity;
                        modelInstance.transform.localScale = Vector3.one * 1.15f;
                        muzzleObj.transform.localPosition = new Vector3(0f, 0.035f, 0.42f);
                        adsPos = new Vector3(0f, -0.038f, 0.22f);
                        adsRot = new Vector3(-2.0f, 0f, 0f);
                        hipPos = new Vector3(0.090f, -0.135f, 0.30f);
                        slideObj = FindChildRecursive(modelInstance.transform, "SMG_F_Bolt", "Bolt", "Slide");
                        magObj = FindChildRecursive(modelInstance.transform, "SMG_F_Mag", "Magazine", "Mag");
                        break;

                    case SelectedWeaponType.SMG_P90_Compact:
                        capacity = 50;
                        velocity = 715f;
                        isAuto = true;
                        recoilMult = 0.75f;
                        modelInstance.transform.localPosition = Vector3.zero;
                        modelInstance.transform.localRotation = Quaternion.identity;
                        modelInstance.transform.localScale = Vector3.one * 1.15f;
                        muzzleObj.transform.localPosition = new Vector3(0f, 0.030f, 0.40f);
                        adsPos = new Vector3(0f, -0.035f, 0.20f);
                        adsRot = new Vector3(-1.8f, 0f, 0f);
                        hipPos = new Vector3(0.090f, -0.135f, 0.30f);
                        slideObj = FindChildRecursive(modelInstance.transform, "SMG_G_Bolt", "Bolt");
                        magObj = FindChildRecursive(modelInstance.transform, "SMG_G_Mag", "Magazine");
                        break;

                    case SelectedWeaponType.Rifle_AKM_Tactical:
                        capacity = 30;
                        velocity = 715f;
                        isAuto = true;
                        recoilMult = 1.25f;
                        modelInstance.transform.localPosition = new Vector3(0f, -0.07f, 0.22f);
                        modelInstance.transform.localRotation = Quaternion.identity;
                        modelInstance.transform.localScale = Vector3.one * 0.90f;
                        muzzleObj.transform.localPosition = new Vector3(0f, 0.040f, 0.65f);
                        adsPos = new Vector3(0f, -0.042f, 0.24f);
                        adsRot = new Vector3(-1.8f, 0f, 0f);
                        hipPos = new Vector3(0.12f, -0.16f, 0.36f);
                        slideObj = FindChildRecursive(modelInstance.transform, "WPN_AKM_bolt_carrier", "SMG_F_Bolt", "Slide", "Bolt");
                        magObj = FindChildRecursive(modelInstance.transform, "WPN_AKM_magazine", "SMG_F_Mag", "Magazine", "Mag");
                        break;
                }

#if UNITY_EDITOR
                if (weaponType == SelectedWeaponType.SMG_Tactical)
                {
                    // Oculta a bala de exibição 7.62x39mm que vem dentro do FBX e ficava flutuando sobre a arma
                    foreach (Transform child in modelInstance.GetComponentsInChildren<Transform>(true))
                    {
                        if (child != null && (child.name.IndexOf("cal", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                              child.name.IndexOf("7_62", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                              child.name.IndexOf("bullet", System.StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            child.gameObject.SetActive(false);
                        }
                    }

                    Material akmMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Weapons_ChamferZone/AKM/Materials/WPNM_AKM.mat");
                    if (akmMat != null)
                    {
                        foreach (MeshRenderer mr in modelInstance.GetComponentsInChildren<MeshRenderer>())
                        {
                            if (mr.gameObject.activeInHierarchy)
                                mr.sharedMaterial = akmMat;
                        }
                    }
                }
                else if (weaponType == SelectedWeaponType.Pistol_9mm)
                {
                    Material m9tMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/GILD/FA FPS Weapons - Free/FA M9T/Materials/Built-In/FA M9T (Black).mat");
                    if (m9tMat != null)
                    {
                        foreach (MeshRenderer mr in modelInstance.GetComponentsInChildren<MeshRenderer>())
                        {
                            mr.sharedMaterial = m9tMat;
                        }
                    }
                }
#endif
            }
            else
            {
                // Fallback procedural garantido para qualquer armamento
                switch (weaponType)
                {
                    case SelectedWeaponType.Revolver_DanWesson_357:
                    case SelectedWeaponType.Revolver_Superhawk_Tactical:
                        capacity = 6;
                        velocity = 450f;
                        isAuto = false;
                        recoilMult = 1.75f;
                        adsPos = new Vector3(0f, -0.030f, 0.20f);
                        adsRot = new Vector3(-2.8f, 0f, 0f);
                        hipPos = new Vector3(0.10f, -0.13f, 0.32f);
                        slideObj = BuildDetailedRevolverMesh(gunRoot.transform, muzzleObj.transform, matMetal, matSlide, matFiber, matBrass);
                        break;

                    case SelectedWeaponType.SMG_Strongarm_Tactical:
                    case SelectedWeaponType.SMG_MP_Vector:
                    case SelectedWeaponType.SMG_P90_Compact:
                    case SelectedWeaponType.Rifle_AKM_Tactical:
                        capacity = 30;
                        velocity = 715f;
                        isAuto = true;
                        recoilMult = 1.1f;
                        adsPos = new Vector3(0f, -0.038f, 0.22f);
                        adsRot = new Vector3(-2.0f, 0f, 0f);
                        hipPos = new Vector3(0.12f, -0.16f, 0.36f);
                        slideObj = BuildDetailedSMGMesh(gunRoot.transform, muzzleObj.transform, matMetal, matSlide, matFiber);
                        break;

                    default: // Pistolas e modelos padrão
                        capacity = 17;
                        velocity = 380f;
                        isAuto = false;
                        recoilMult = 1.0f;
                        adsPos = new Vector3(0f, -0.024f, 0.18f);
                        adsRot = new Vector3(-3.2f, 0f, 0f);
                        hipPos = new Vector3(0.10f, -0.13f, 0.32f);
                        slideObj = BuildDetailedPistolMesh(gunRoot.transform, muzzleObj.transform, matMetal, matSlide, matFiber, matBrass);
                        break;
                }
            }

            Weapon weapon = gunRoot.AddComponent<Weapon>();
            AudioSource audio = gunRoot.AddComponent<AudioSource>();
            audio.playOnAwake = false;

            SetFieldValue(weapon, "muzzlePoint", muzzleObj.transform);
            SetFieldValue(weapon, "bulletPrefab", bulletPrefab);
            SetFieldValue(weapon, "bulletHoleDecalPrefab", decalPrefab);
            SetFieldValue(weapon, "slideTransform", slideObj != null ? slideObj : gunRoot.transform);
            SetFieldValue(weapon, "audioSource", audio);
            SetFieldValue(weapon, "fireSound", ProceduralAudio.CreateGunshotClip());
            SetFieldValue(weapon, "dryFireSound", ProceduralAudio.CreateDryFireClip());
            SetFieldValue(weapon, "slideLockSound", ProceduralAudio.CreateSlideLockClip());
            SetFieldValue(weapon, "hitLayers", (LayerMask)(~0));

            // Criação dos Sockets de Alinhamento de Mira Óptica (Front Sight e Rear Sight)
            GameObject frontSightObj = new GameObject("FrontSightSocket");
            GameObject rearSightObj = new GameObject("RearSightSocket");

            switch (weaponType)
            {
                case SelectedWeaponType.Pistol_Beretta_M9T_Black:
                case SelectedWeaponType.Pistol_Beretta_M9T_Tan:
                    frontSightObj.transform.SetParent(slideObj != null ? slideObj : gunRoot.transform);
                    frontSightObj.transform.localPosition = new Vector3(0f, 0.0925f, 0.0222f);
                    frontSightObj.transform.localRotation = Quaternion.identity;
                    rearSightObj.transform.SetParent(slideObj != null ? slideObj : gunRoot.transform);
                    rearSightObj.transform.localPosition = new Vector3(0f, 0.0905f, -0.0189f);
                    rearSightObj.transform.localRotation = Quaternion.identity;
                    break;

                case SelectedWeaponType.Pistol_Tactical_Glock:
                case SelectedWeaponType.Pistol_Combat_Custom:
                    frontSightObj.transform.SetParent(slideObj != null ? slideObj : gunRoot.transform);
                    frontSightObj.transform.localPosition = new Vector3(0f, 0.038f, 0.12f);
                    frontSightObj.transform.localRotation = Quaternion.identity;
                    rearSightObj.transform.SetParent(slideObj != null ? slideObj : gunRoot.transform);
                    rearSightObj.transform.localPosition = new Vector3(0f, 0.038f, -0.04f);
                    rearSightObj.transform.localRotation = Quaternion.identity;
                    break;

                case SelectedWeaponType.Revolver_DanWesson_357:
                case SelectedWeaponType.Revolver_Superhawk_Tactical:
                    frontSightObj.transform.SetParent(gunRoot.transform);
                    frontSightObj.transform.localPosition = new Vector3(0f, 0.045f, 0.20f);
                    frontSightObj.transform.localRotation = Quaternion.identity;
                    rearSightObj.transform.SetParent(gunRoot.transform);
                    rearSightObj.transform.localPosition = new Vector3(0f, 0.042f, -0.04f);
                    rearSightObj.transform.localRotation = Quaternion.identity;
                    break;

                case SelectedWeaponType.SMG_Strongarm_Tactical:
                case SelectedWeaponType.SMG_MP_Vector:
                case SelectedWeaponType.SMG_P90_Compact:
                    frontSightObj.transform.SetParent(gunRoot.transform);
                    frontSightObj.transform.localPosition = new Vector3(0f, 0.052f, 0.28f);
                    frontSightObj.transform.localRotation = Quaternion.identity;
                    rearSightObj.transform.SetParent(gunRoot.transform);
                    rearSightObj.transform.localPosition = new Vector3(0f, 0.050f, 0.05f);
                    rearSightObj.transform.localRotation = Quaternion.identity;
                    break;

                case SelectedWeaponType.Rifle_AKM_Tactical:
                    frontSightObj.transform.SetParent(gunRoot.transform);
                    frontSightObj.transform.localPosition = new Vector3(0f, 0.065f, 0.42f);
                    frontSightObj.transform.localRotation = Quaternion.identity;
                    rearSightObj.transform.SetParent(gunRoot.transform);
                    rearSightObj.transform.localPosition = new Vector3(0f, 0.058f, 0.14f);
                    rearSightObj.transform.localRotation = Quaternion.identity;
                    break;

                default: // Modelo procedural
                    frontSightObj.transform.SetParent(slideObj != null ? slideObj : gunRoot.transform);
                    frontSightObj.transform.localPosition = new Vector3(0f, 0.72f, 0.44f);
                    frontSightObj.transform.localRotation = Quaternion.identity;
                    rearSightObj.transform.SetParent(slideObj != null ? slideObj : gunRoot.transform);
                    rearSightObj.transform.localPosition = new Vector3(0f, 0.68f, -0.44f);
                    rearSightObj.transform.localRotation = Quaternion.identity;
                    break;
            }

            Camera mainCam = Camera.main;

            SetFieldValue(weapon, "frontSightSocket", frontSightObj.transform);
            SetFieldValue(weapon, "rearSightSocket", rearSightObj.transform);
            if (mainCam != null) SetFieldValue(weapon, "playerCamera", mainCam);
            SetFieldValue(weapon, "fireFromCameraCenter", true);

            // Atribui o carregador para animação física de recarga se encontrado
            if (magObj != null)
            {
                SetFieldValue(weapon, "magazineTransform", magObj);
            }

            weapon.ConfigureWeapon(capacity, velocity);
            handling.ConfigureHandling(isAuto, recoilMult, adsPos);

            SetFieldValue(handling, "weapon", weapon);
            SetFieldValue(handling, "weaponTransform", holder.transform);
            SetFieldValue(handling, "hipPosition", hipPos);

            // Anexa e configura WeaponProceduralMotion na hierarquia da arma (WeaponHolder)
            WeaponProceduralMotion procMotion = holder.GetComponent<WeaponProceduralMotion>();
            if (procMotion == null) procMotion = holder.AddComponent<WeaponProceduralMotion>();

            procMotion.SetSightSockets(frontSightObj.transform, rearSightObj.transform, 0.18f);

            WeaponData wData = ScriptableObject.CreateInstance<WeaponData>();
            wData.name = $"Data_{weaponType}";
            SetFieldValue(wData, "weaponName", weaponType.ToString().Replace("_", " "));
            SetFieldValue(wData, "adsLocalPosition", adsPos);
            SetFieldValue(wData, "adsLocalRotation", adsRot);
            SetFieldValue(wData, "adsSpeed", 14.0f);
            SetFieldValue(wData, "adsCameraFOV", 48.0f);
            SetFieldValue(wData, "bulletSpeed", velocity);
            SetFieldValue(wData, "penetrationPower", isAuto ? 55f : 40f);
            SetFieldValue(wData, "baseSpread", isAuto ? 0.08f : 0.035f);
            SetFieldValue(wData, "movementSpreadMultiplier", 2.5f);

            procMotion.ConfigureStance(wData, hipPos);
            SetFieldValue(procMotion, "weaponTransform", holder.transform);
            if (mainCam != null) SetFieldValue(procMotion, "playerCamera", mainCam);
            procMotion.CalibrateADSFromSockets();

            CharacterController charCtrl = holder.GetComponentInParent<CharacterController>();
            if (charCtrl != null) SetFieldValue(procMotion, "characterController", charCtrl);

            // Anexa o sistema balístico WeaponBallistics
            WeaponBallistics ballistics = gunRoot.GetComponent<WeaponBallistics>();
            if (ballistics == null) ballistics = gunRoot.AddComponent<WeaponBallistics>();
            ballistics.CurrentWeaponData = wData;
            SetFieldValue(ballistics, "muzzlePoint", muzzleObj.transform);
            if (mainCam != null) SetFieldValue(ballistics, "playerCamera", mainCam);
            SetFieldValue(ballistics, "bulletDecalPrefab", decalPrefab);

            // Desativa a movimentação anterior para que WeaponProceduralMotion assuma 100%
            ProceduralViewmodelMotion motion = holder.GetComponent<ProceduralViewmodelMotion>();
            if (motion != null) motion.enabled = false;

            TacticalWeaponCycles cycles = holder.GetComponent<TacticalWeaponCycles>();
            if (cycles == null) cycles = holder.AddComponent<TacticalWeaponCycles>();
            cycles.BindTransforms(weapon, slideObj, magObj, motion);

            // Configura postura tática refinada de viewmodel
            TacticalViewmodelArms arms = holder.GetComponent<TacticalViewmodelArms>();
            if (arms == null) arms = holder.AddComponent<TacticalViewmodelArms>();
            arms.InitializeArms(holder.transform);
            arms.SetWeaponIKStance(weaponType);
        }

        /// <summary>
        /// Troca a arma equipada do jogador em tempo real durante a partida (teclas 1, 2, 3).
        /// </summary>
        public static void SwitchEquippedWeapon(SelectedWeaponType newWeapon)
        {
            GameObject holder = GameObject.Find("WeaponHolder");
            if (holder == null) return;

            WeaponHandling handling = holder.GetComponent<WeaponHandling>();
            if (handling == null) handling = holder.AddComponent<WeaponHandling>();

            for (int i = holder.transform.childCount - 1; i >= 0; i--)
            {
                SafeDestroy(holder.transform.GetChild(i).gameObject);
            }

            GameObject rangeRoot = GameObject.Find("--- IPSC SHOOTING RANGE ---");
            Transform bulletTrans = rangeRoot != null ? rangeRoot.transform.Find("Bullet_Prefab") : null;
            Transform decalTrans = rangeRoot != null ? rangeRoot.transform.Find("Decal_BulletHole_Prefab") : null;

            GameObject decalObj = decalTrans != null ? decalTrans.gameObject : CreateBulletHoleDecalObject();
            GameObject bulletObj = bulletTrans != null ? bulletTrans.gameObject : CreateBulletPrefab(decalObj);

            AttachSelectedWeapon(holder, handling, bulletObj, decalObj, newWeapon);
            GameplayHUD.Instance?.UpdateWeaponName(newWeapon.ToString().Replace("_", " "));
            Debug.Log($"[Arsenal] Arma equipada alterada para: {newWeapon}");
        }



        private static Transform BuildDetailedPistolMesh(Transform parent, Transform muzzle, Material matMetal, Material matSlide, Material matFiber, Material matBrass)
        {
            // Grip
            GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grip.name = "Pistol_Grip";
            grip.transform.SetParent(parent);
            grip.transform.localPosition = new Vector3(0f, -0.06f, -0.02f);
            grip.transform.localScale = new Vector3(0.028f, 0.12f, 0.046f);
            grip.transform.localRotation = Quaternion.Euler(18f, 0, 0);
            grip.GetComponent<MeshRenderer>().material = matMetal;
            SafeDestroy(grip.GetComponent<Collider>());

            // Guarda-mato e Gatilho
            GameObject guard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            guard.transform.SetParent(parent);
            guard.transform.localPosition = new Vector3(0f, -0.03f, 0.035f);
            guard.transform.localScale = new Vector3(0.018f, 0.035f, 0.045f);
            guard.GetComponent<MeshRenderer>().material = matMetal;
            SafeDestroy(guard.GetComponent<Collider>());

            // Ferrolho móvel
            GameObject slide = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slide.name = "Pistol_Slide";
            slide.transform.SetParent(parent);
            slide.transform.localPosition = new Vector3(0f, 0.026f, 0.04f);
            slide.transform.localScale = new Vector3(0.031f, 0.028f, 0.18f);
            slide.GetComponent<MeshRenderer>().material = matSlide;
            SafeDestroy(slide.GetComponent<Collider>());

            // Miras com Fibra Ótica
            GameObject sightBlade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sightBlade.transform.SetParent(slide.transform);
            sightBlade.transform.localPosition = new Vector3(0f, 0.58f, 0.44f);
            sightBlade.transform.localScale = new Vector3(0.12f, 0.28f, 0.06f);
            sightBlade.GetComponent<MeshRenderer>().material = matMetal;
            SafeDestroy(sightBlade.GetComponent<Collider>());

            GameObject fiber = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fiber.transform.SetParent(sightBlade.transform);
            fiber.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            fiber.transform.localScale = new Vector3(0.4f, 0.7f, 0.4f);
            fiber.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            fiber.GetComponent<MeshRenderer>().material = matFiber;
            SafeDestroy(fiber.GetComponent<Collider>());

            muzzle.localPosition = new Vector3(0f, 0.022f, 0.145f);
            return slide.transform;
        }

        private static Transform BuildDetailedRevolverMesh(Transform parent, Transform muzzle, Material matMetal, Material matSlide, Material matFiber, Material matBrass)
        {
            // Empunhadura de Madeira clássica
            Material matRevolverWood = ProceduralMaterials.CreateMaterial(new Color(0.45f, 0.25f, 0.12f), "Mat_RevolverWood", 0.6f, 0.0f);
            GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grip.name = "Revolver_Grip";
            grip.transform.SetParent(parent);
            grip.transform.localPosition = new Vector3(0f, -0.07f, -0.04f);
            grip.transform.localScale = new Vector3(0.030f, 0.13f, 0.052f);
            grip.transform.localRotation = Quaternion.Euler(22f, 0, 0);
            grip.GetComponent<MeshRenderer>().material = matRevolverWood;
            SafeDestroy(grip.GetComponent<Collider>());

            // Armação de Aço Inox
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Revolver_Frame";
            frame.transform.SetParent(parent);
            frame.transform.localPosition = new Vector3(0f, 0.01f, 0.01f);
            frame.transform.localScale = new Vector3(0.032f, 0.045f, 0.08f);
            frame.GetComponent<MeshRenderer>().material = matSlide;
            SafeDestroy(frame.GetComponent<Collider>());

            // Tambor Cilindrico Rotativo (.357 Magnum)
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = "Revolver_Cylinder";
            cylinder.transform.SetParent(parent);
            cylinder.transform.localPosition = new Vector3(0f, 0.01f, 0.01f);
            cylinder.transform.localScale = new Vector3(0.042f, 0.035f, 0.042f);
            cylinder.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            cylinder.GetComponent<MeshRenderer>().material = matMetal;
            SafeDestroy(cylinder.GetComponent<Collider>());

            // Cano Longo de 6 polegadas (Dan Wesson Barrel)
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrel.name = "Revolver_Barrel";
            barrel.transform.SetParent(parent);
            barrel.transform.localPosition = new Vector3(0f, 0.022f, 0.13f);
            barrel.transform.localScale = new Vector3(0.024f, 0.032f, 0.16f);
            barrel.GetComponent<MeshRenderer>().material = matSlide;
            SafeDestroy(barrel.GetComponent<Collider>());

            // Massa de mira alta
            GameObject sight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sight.transform.SetParent(barrel.transform);
            sight.transform.localPosition = new Vector3(0f, 0.65f, 0.45f);
            sight.transform.localScale = new Vector3(0.15f, 0.35f, 0.08f);
            sight.GetComponent<MeshRenderer>().material = matFiber;
            SafeDestroy(sight.GetComponent<Collider>());

            muzzle.localPosition = new Vector3(0f, 0.022f, 0.22f);
            return barrel.transform;
        }

        private static Transform BuildDetailedSMGMesh(Transform parent, Transform muzzle, Material matMetal, Material matSlide, Material matFiber)
        {
            // Corpo Principal da SMG
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "SMG_Receiver";
            body.transform.SetParent(parent);
            body.transform.localPosition = new Vector3(0f, 0.02f, 0.08f);
            body.transform.localScale = new Vector3(0.038f, 0.055f, 0.26f);
            body.GetComponent<MeshRenderer>().material = matMetal;
            SafeDestroy(body.GetComponent<Collider>());

            // Empunhadura
            GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grip.transform.SetParent(parent);
            grip.transform.localPosition = new Vector3(0f, -0.06f, 0.01f);
            grip.transform.localScale = new Vector3(0.028f, 0.12f, 0.045f);
            grip.transform.localRotation = Quaternion.Euler(15f, 0, 0);
            grip.GetComponent<MeshRenderer>().material = matMetal;
            SafeDestroy(grip.GetComponent<Collider>());

            // Carregador Curvo Longo de 30 tiros
            GameObject mag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mag.transform.SetParent(parent);
            mag.transform.localPosition = new Vector3(0f, -0.09f, 0.12f);
            mag.transform.localScale = new Vector3(0.024f, 0.16f, 0.035f);
            mag.transform.localRotation = Quaternion.Euler(-12f, 0, 0);
            mag.GetComponent<MeshRenderer>().material = matSlide;
            SafeDestroy(mag.GetComponent<Collider>());

            // Cano e Quebra-Chamas
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.transform.SetParent(parent);
            barrel.transform.localPosition = new Vector3(0f, 0.025f, 0.24f);
            barrel.transform.localScale = new Vector3(0.018f, 0.06f, 0.018f);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            barrel.GetComponent<MeshRenderer>().material = matMetal;
            SafeDestroy(barrel.GetComponent<Collider>());

            muzzle.localPosition = new Vector3(0f, 0.025f, 0.31f);
            return body.transform;
        }

        // =========================================================================
        // UTILITÁRIOS DE CRIAÇÃO E MATERIAIS
        // =========================================================================
        private static void SetupSunLighting(bool warm)
        {
            CinematicLightingSetup.ApplyCinematicLighting(null, new Vector3(38f, 45f, 0f), warm ? 1.35f : 1.15f);

#if UNITY_EDITOR
            Material skyMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Starter Assets/Sample/Environment/Art/Skybox/SkyboxLiteWarm.mat");
            if (skyMat != null)
            {
                RenderSettings.skybox = skyMat;
            }
#endif

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.0035f;
            RenderSettings.fogColor = new Color(0.85f, 0.88f, 0.92f);
        }

        private static void SetupIndoorLighting()
        {
            Light sun = Object.FindAnyObjectByType<Light>();
            if (sun != null)
            {
                sun.color = new Color(0.85f, 0.90f, 0.95f);
                sun.intensity = 0.45f;
                sun.transform.rotation = Quaternion.Euler(85f, 0, 0);
                sun.shadows = LightShadows.Soft;
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.35f, 0.38f, 0.45f);
            RenderSettings.ambientEquatorColor = new Color(0.25f, 0.25f, 0.28f);
            RenderSettings.ambientGroundColor = new Color(0.15f, 0.15f, 0.16f);

            RenderSettings.fog = false;
        }

        private static void SetupTacticalCSLighting()
        {
            Light sun = Object.FindAnyObjectByType<Light>();
            if (sun == null)
            {
                GameObject sunObj = new GameObject("CS_Sun_Light");
                sun = sunObj.AddComponent<Light>();
                sun.type = LightType.Directional;
            }

            sun.color = new Color(1.0f, 0.95f, 0.85f);
            sun.intensity = 1.25f;
            sun.transform.rotation = Quaternion.Euler(50f, 40f, 0);
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.85f;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.60f, 0.68f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.45f, 0.45f);
            RenderSettings.ambientGroundColor = new Color(0.28f, 0.28f, 0.28f);

#if UNITY_EDITOR
            Material skyMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Starter Assets/Sample/Environment/Art/Skybox/SkyboxLiteWarm.mat");
            if (skyMat != null) RenderSettings.skybox = skyMat;
#endif

            GameObject probeObj = GameObject.Find("CS_Reflection_Probe");
            if (probeObj == null)
            {
                probeObj = new GameObject("CS_Reflection_Probe");
                ReflectionProbe probe = probeObj.AddComponent<ReflectionProbe>();
                probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
                probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;
                probe.size = new Vector3(50f, 20f, 60f);
                probeObj.transform.position = new Vector3(0, 2.5f, 16f);
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.003f;
            RenderSettings.fogColor = new Color(0.70f, 0.72f, 0.75f);
        }

        private static Material CreateGravelMaterial()
        {
            Material mat = ProceduralMaterials.CreateMaterial(new Color(0.48f, 0.43f, 0.36f), "Mat_Gravel_PBR", 0.15f, 0.05f);
            mat.mainTexture = ProceduralMaterials.CreateGravelTexture();
            mat.mainTextureScale = new Vector2(10f, 18f);
            return mat;
        }

        private static Material CreateWoodMaterial()
        {
            Material mat = ProceduralMaterials.CreateMaterial(new Color(0.65f, 0.50f, 0.33f), "Mat_Wood_Planks", 0.25f, 0.0f);
            mat.mainTexture = ProceduralMaterials.CreateWoodPlankTexture();
            mat.mainTextureScale = new Vector2(2f, 8f);
            return mat;
        }

        private static Material CreateConcreteMaterial()
        {
            Material mat = ProceduralMaterials.CreateMaterial(new Color(0.40f, 0.40f, 0.42f), "Mat_Concrete_Industrial", 0.35f, 0.1f);
            mat.mainTexture = ProceduralMaterials.CreateConcreteTexture();
            mat.mainTextureScale = new Vector2(8f, 12f);
            return mat;
        }

        private static Material CreateCrateMaterial()
        {
            Material mat = ProceduralMaterials.CreateMaterial(new Color(0.38f, 0.42f, 0.28f), "Mat_MilitaryCrate", 0.2f, 0.0f);
            mat.mainTexture = ProceduralMaterials.CreateMilitaryCrateTexture();
            return mat;
        }

        private static Material CreateBermMaterial()
        {
            Material mat = ProceduralMaterials.CreateMaterial(new Color(0.32f, 0.22f, 0.15f), "Mat_EarthBerm", 0.05f, 0.0f);
            mat.mainTexture = ProceduralMaterials.CreateGravelTexture();
            mat.mainTextureScale = new Vector2(6f, 4f);
            return mat;
        }

        private static Material CreateHazardMaterial()
        {
            Material mat = ProceduralMaterials.CreateMaterial(Color.white, "Mat_HazardStripes", 0.3f, 0.0f);
            mat.mainTexture = ProceduralMaterials.CreateWarningStripeTexture();
            mat.mainTextureScale = new Vector2(1f, 8f);
            return mat;
        }

        private static Material CreateBarrelMaterial(Color color)
        {
            Material mat = ProceduralMaterials.CreateMaterial(color, "Mat_Barrel", 0.55f, 0.45f);
            mat.mainTexture = ProceduralMaterials.CreateBarrelTexture(color);
            return mat;
        }

        private static Material CreateSteelMaterial()
        {
            return ProceduralMaterials.CreateMaterial(new Color(0.90f, 0.90f, 0.92f), "Mat_SteelPlate", 0.5f, 0.65f);
        }

        private static void CreateFloor(Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Floor";
            ground.transform.SetParent(parent);
            ground.transform.position = pos;
            ground.transform.localScale = scale;

            Material instMat = new Material(mat);
            float tileX = Mathf.Max(1f, scale.x * 0.5f);
            float tileZ = Mathf.Max(1f, scale.z * 0.5f);
            instMat.mainTextureScale = new Vector2(tileX, tileZ);
            if (instMat.HasProperty("_BumpMap")) instMat.SetTextureScale("_BumpMap", new Vector2(tileX, tileZ));
            ground.GetComponent<MeshRenderer>().material = instMat;
        }

        private static void CreateBerm(Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject berm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            berm.name = "Backstop_Berm";
            berm.transform.SetParent(parent);
            berm.transform.position = pos;
            berm.transform.localScale = scale;
            berm.GetComponent<MeshRenderer>().material = mat;
        }

        private static void CreateWall(Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.SetParent(parent);
            wall.transform.position = pos;
            wall.transform.localScale = scale;

            Material instMat = new Material(mat);
            float length = scale.x > scale.z ? scale.x : scale.z;
            float tileX = Mathf.Max(1f, length * 0.4f);
            float tileY = Mathf.Max(1f, scale.y * 0.4f);
            instMat.mainTextureScale = new Vector2(tileX, tileY);
            if (instMat.HasProperty("_BumpMap")) instMat.SetTextureScale("_BumpMap", new Vector2(tileX, tileY));
            wall.GetComponent<MeshRenderer>().material = instMat;
        }

        private static void CreateWallWithFraming(Transform parent, Vector3 pos, Vector3 size, Material matWood)
        {
            // Painel central de compensado naval / OSB
            GameObject wallObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallObj.name = "Tactical_ShootHouse_Wall";
            wallObj.transform.SetParent(parent);
            wallObj.transform.position = pos;
            wallObj.transform.localScale = size;
            wallObj.GetComponent<MeshRenderer>().material = matWood;

            // Montantes verticais de reforço (Studs 2x4) espaçados para gerar sombras e relevo arquitetônico realista
            float halfLength = size.z * 0.5f;
            float step = 2.0f;
            Material studMat = ProceduralMaterials.CreateMaterial(new Color(0.48f, 0.35f, 0.22f), "Mat_WallStud", 0.1f, 0.2f);

            for (float z = -halfLength; z <= halfLength + 0.1f; z += step)
            {
                GameObject studIn = GameObject.CreatePrimitive(PrimitiveType.Cube);
                studIn.name = "Stud_Vertical";
                studIn.transform.SetParent(parent);
                float xOffset = (pos.x > 0) ? -0.12f : 0.12f;
                studIn.transform.position = new Vector3(pos.x + xOffset, pos.y, pos.z + z);
                studIn.transform.localScale = new Vector3(0.08f, size.y, 0.08f);
                studIn.GetComponent<MeshRenderer>().material = studMat;
                SafeDestroy(studIn.GetComponent<Collider>());
            }
        }

        private static void CreateRoofRafters(Transform parent, float centerZ, float length, float heightY, Material matWood)
        {
            GameObject raftersRoot = new GameObject("Roof_Trusses_Rafters");
            raftersRoot.transform.SetParent(parent);

            float halfLen = length * 0.5f;
            for (float z = centerZ - halfLen; z <= centerZ + halfLen; z += 3.5f)
            {
                GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                beam.name = "Rafter_CrossBeam";
                beam.transform.SetParent(raftersRoot.transform);
                beam.transform.position = new Vector3(0f, heightY, z);
                beam.transform.localScale = new Vector3(17.2f, 0.18f, 0.15f);
                beam.GetComponent<MeshRenderer>().material = matWood;
                SafeDestroy(beam.GetComponent<Collider>());
            }
        }

        private static void CreateTacticalCover(Transform parent, Vector3 pos, Material matWood, Material matBarrel)
        {
            GameObject coverRoot = new GameObject("Tactical_Cover_Station");
            coverRoot.transform.SetParent(parent);
            coverRoot.transform.position = pos;

            GameObject cratePrefab = null;
#if UNITY_EDITOR
            cratePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Starter Assets/Sample/Environment/Prefabs/Box_350x250x200_Prefab.prefab");
            if (cratePrefab == null)
            {
                cratePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Starter Assets/Sample/Environment/Prefabs/Box_100x100x100_Prefab.prefab");
            }
#endif

            if (cratePrefab != null)
            {
                GameObject crateInstance = Object.Instantiate(cratePrefab, coverRoot.transform);
                crateInstance.transform.localPosition = Vector3.zero;
                crateInstance.transform.localRotation = Quaternion.Euler(0, UnityEngine.Random.Range(-15f, 15f), 0);
                crateInstance.transform.localScale = Vector3.one * 0.85f;
            }
            else
            {
                GameObject crateObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                crateObj.transform.SetParent(coverRoot.transform);
                crateObj.transform.localPosition = new Vector3(0, 0.6f, 0);
                crateObj.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
                crateObj.GetComponent<MeshRenderer>().material = matWood;
            }

            CreateBarrel(coverRoot.transform, new Vector3(1.1f, 0.45f, 0.2f), matBarrel);
        }

        private static void CreateBarricade(Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject barricade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barricade.name = "Barricade";
            barricade.transform.SetParent(parent);
            barricade.transform.position = pos;
            barricade.transform.localScale = scale;
            barricade.GetComponent<MeshRenderer>().material = mat;
        }

        private static void CreateBarrel(Transform parent, Vector3 pos, Material mat)
        {
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "Tactical_Barrel_55Gal";
            barrel.transform.SetParent(parent);
            barrel.transform.position = pos;
            barrel.transform.localScale = new Vector3(0.62f, 0.48f, 0.62f);
            barrel.GetComponent<MeshRenderer>().material = mat;

            Rigidbody rb = barrel.AddComponent<Rigidbody>();
            rb.mass = 12f;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.5f;
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
            SafeDestroy(rip.GetComponent<Collider>());
        }

        private static void CreateShootingBench(Transform parent, Vector3 pos, Material woodMat)
        {
            GameObject bench = new GameObject("Safety_Area_Bench");
            bench.transform.SetParent(parent);
            bench.transform.position = pos;

            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.transform.SetParent(bench.transform);
            top.transform.localPosition = new Vector3(0, 0.85f, 0);
            top.transform.localScale = new Vector3(1.4f, 0.08f, 0.75f);
            top.GetComponent<MeshRenderer>().material = woodMat;
        }

        private static void CreateSteelPopper(Transform parent, string name, Vector3 pos, Material mat)
        {
            GameObject popperRoot = new GameObject(name);
            popperRoot.transform.SetParent(parent);
            popperRoot.transform.position = pos;

            GameObject basePlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            basePlate.name = "Popper_Base";
            basePlate.transform.SetParent(popperRoot.transform);
            basePlate.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            basePlate.transform.localScale = new Vector3(0.45f, 0.08f, 0.55f);
            basePlate.GetComponent<MeshRenderer>().material = mat;

            GameObject hinge = new GameObject("Popper_Hinge");
            hinge.transform.SetParent(popperRoot.transform);
            hinge.transform.localPosition = new Vector3(0f, 0.08f, 0f);

            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pole.name = "Popper_Body";
            pole.transform.SetParent(hinge.transform);
            pole.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            pole.transform.localScale = new Vector3(0.16f, 0.75f, 0.035f);
            pole.GetComponent<MeshRenderer>().material = mat;

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            head.name = "Popper_Head";
            head.transform.SetParent(hinge.transform);
            head.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            head.transform.localScale = new Vector3(0.35f, 0.02f, 0.35f);
            head.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            head.GetComponent<MeshRenderer>().material = mat;

            IPSCSteelPopper popperScript = popperRoot.AddComponent<IPSCSteelPopper>();
            SetFieldValue(popperScript, "targetName", name);
            SetFieldValue(popperScript, "hingeTransform", hinge.transform);
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
            SafeDestroy(decal.GetComponent<Collider>());
            return decal;
        }

        private static GameObject CreateBulletPrefab(GameObject decalPrefab)
        {
            GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bullet.name = "Bullet_Prefab";
            bullet.transform.localScale = new Vector3(0.015f, 0.015f, 0.035f);

            Material bulletMat = ProceduralMaterials.CreateMaterial(new Color(0.95f, 0.85f, 0.35f), "Mat_Bullet_Brass", 0.9f, 0.95f);
            bullet.GetComponent<MeshRenderer>().material = bulletMat;
            SafeDestroy(bullet.GetComponent<Collider>());

            bullet.AddComponent<Bullet>();
            return bullet;
        }

        public static IPSCTarget CreateIPSCTarget(Transform parent, string name, Vector3 position, bool isNoShoot, Material woodMat, bool withVisualFocus = false)
        {
            GameObject targetObj = new GameObject(name);
            targetObj.transform.SetParent(parent);
            targetObj.transform.position = position;

            IPSCTarget targetScript = targetObj.AddComponent<IPSCTarget>();
            SetFieldValue(targetScript, "targetName", name);
            SetFieldValue(targetScript, "isNoShoot", isNoShoot);

            // Suportes de ripas de madeira
            GameObject stickL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stickL.transform.SetParent(targetObj.transform);
            stickL.transform.localPosition = new Vector3(-0.18f, -0.5f, -0.02f);
            stickL.transform.localScale = new Vector3(0.04f, 1.4f, 0.04f);
            stickL.GetComponent<MeshRenderer>().material = woodMat;

            GameObject stickR = GameObject.CreatePrimitive(PrimitiveType.Cube);
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
            targetMat.mainTexture = ProceduralMaterials.CreateIPSCTargetTexture(isNoShoot, withVisualFocus);
            board.GetComponent<MeshRenderer>().material = targetMat;
            SafeDestroy(board.GetComponent<Collider>());

            // Zonas Alpha, Charlie e Delta
            CreateTargetZone(targetObj.transform, "Zone_Alpha", Vector3.zero, new Vector3(0.15f, 0.32f, 0.02f), isNoShoot ? IPSCZoneType.NoShoot : IPSCZoneType.Alpha, targetScript);
            CreateTargetZone(targetObj.transform, "Zone_Charlie", Vector3.zero, new Vector3(0.30f, 0.55f, 0.02f), isNoShoot ? IPSCZoneType.NoShoot : IPSCZoneType.Charlie, targetScript);
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

            // Torna o alvo de papelão penetrável por projéteis
            PenetrableSurface penetrable = zoneObj.AddComponent<PenetrableSurface>();
            SetFieldValue(penetrable, "materialType", SurfaceMaterialType.PapelaoAlvoIPSC);
            penetrable.ApplyPresetDefaults();
        }

        private static void SetFieldValue(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (field != null) field.SetValue(target, value);
        }

        private static Transform FindChildRecursive(Transform parent, params string[] targetNames)
        {
            if (parent == null) return null;
            foreach (string name in targetNames)
            {
                Transform found = parent.Find(name);
                if (found != null) return found;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                foreach (string name in targetNames)
                {
                    if (child.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        return child;
                }
                Transform deep = FindChildRecursive(child, targetNames);
                if (deep != null) return deep;
            }
            return null;
        }
    }
}
