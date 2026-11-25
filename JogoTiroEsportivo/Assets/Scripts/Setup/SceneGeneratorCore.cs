using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    /// Construtor procedural de cena: monta o estande de tiro completo, alvos IPSC,
    /// jogador com arma modelada proceduralmente, sistema de áudio e UI.
    /// Pode ser executado em tempo de execução ou via Editor.
    /// </summary>
    public static class SceneGeneratorCore
    {
        public static void GenerateCompleteShootingRange()
        {
            Debug.Log("[IPSC Setup] Iniciando montagem procedural do estande de tiro...");

            // 1. Limpeza ou organização inicial
            GameObject root = new GameObject("--- IPSC SHOOTING RANGE ---");

            // 2. Materiais básicos
            Material matGround = ProceduralMaterials.CreateMaterial(new Color(0.42f, 0.38f, 0.32f), "Mat_Gravel", 0.1f);
            Material matBerm = ProceduralMaterials.CreateMaterial(new Color(0.35f, 0.25f, 0.18f), "Mat_EarthBerm", 0.05f);
            Material matWood = ProceduralMaterials.CreateMaterial(new Color(0.55f, 0.40f, 0.25f), "Mat_Wood", 0.1f);
            Material matGunMetal = ProceduralMaterials.CreateMaterial(new Color(0.12f, 0.12f, 0.13f), "Mat_GunMetal", 0.5f);
            Material matGunSlide = ProceduralMaterials.CreateMaterial(new Color(0.20f, 0.20f, 0.22f), "Mat_GunSlide", 0.7f);
            Material matSightDot = ProceduralMaterials.CreateMaterial(Color.red, "Mat_FiberOptic", 0.9f);
            Material matFaultLine = ProceduralMaterials.CreateMaterial(new Color(0.9f, 0.75f, 0.1f), "Mat_FaultLine", 0.2f);

            // 3. Montagem do Estande (Chão, Para-balas, Baias)
            GameObject rangeObj = new GameObject("ShootingRange_Environment");
            rangeObj.transform.SetParent(root.transform);

            // Chão do Estande
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Range_Floor";
            ground.transform.SetParent(rangeObj.transform);
            ground.transform.position = new Vector3(0, -0.25f, 12f);
            ground.transform.localScale = new Vector3(18f, 0.5f, 32f);
            ground.GetComponent<MeshRenderer>().material = matGround;

            // Para-balas de fundo (Berm)
            GameObject berm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            berm.name = "Backstop_Berm";
            berm.transform.SetParent(rangeObj.transform);
            berm.transform.position = new Vector3(0, 2f, 25f);
            berm.transform.localScale = new Vector3(20f, 5f, 4f);
            berm.GetComponent<MeshRenderer>().material = matBerm;

            // Barreiras laterais de segurança
            CreateWall(rangeObj.transform, new Vector3(-8.5f, 1.5f, 12f), new Vector3(1f, 3.5f, 30f), matWood);
            CreateWall(rangeObj.transform, new Vector3(8.5f, 1.5f, 12f), new Vector3(1f, 3.5f, 30f), matWood);

            // Shooting Box (caixa de tiro no chão)
            CreateShootingBox(rangeObj.transform, Vector3.zero, matFaultLine);

            // 4. Criação do Prefab de Decalque (Furo de Bala)
            GameObject decalPrefab = CreateBulletHoleDecalObject();
            decalPrefab.transform.SetParent(root.transform);
            decalPrefab.SetActive(false);

            // 5. Criação do Projétil
            GameObject bulletPrefab = CreateBulletPrefab(decalPrefab);
            bulletPrefab.transform.SetParent(root.transform);
            bulletPrefab.SetActive(false);

            // 6. Alvos IPSC na pista
            List<IPSCTarget> targetsList = new List<IPSCTarget>();

            // Alvo 1: Centro a 7 metros
            targetsList.Add(CreateIPSCTarget(root.transform, "Target_01 (7m)", new Vector3(0f, 1.4f, 7f), false, matWood));

            // Alvo 2: Esquerda a 12 metros
            targetsList.Add(CreateIPSCTarget(root.transform, "Target_02 (12m)", new Vector3(-3.5f, 1.4f, 12f), false, matWood));

            // Alvo No-Shoot: Ao lado do Alvo 2 (Penalidade)
            targetsList.Add(CreateIPSCTarget(root.transform, "NoShoot_Target", new Vector3(-2.8f, 1.35f, 11.5f), true, matWood));

            // Alvo 3: Direita a 16 metros
            targetsList.Add(CreateIPSCTarget(root.transform, "Target_03 (16m)", new Vector3(3.2f, 1.4f, 16f), false, matWood));

            // 7. Jogador e Arma Procedural com Visada Alinhada
            GameObject player = CreatePlayerWithWeapon(root.transform, bulletPrefab, decalPrefab, matGunMetal, matGunSlide, matSightDot);

            // 8. Range Officer Timer
            GameObject roObj = new GameObject("RangeOfficer_Timer");
            roObj.transform.SetParent(root.transform);
            AudioSource roAudio = roObj.AddComponent<AudioSource>();
            roAudio.playOnAwake = false;
            RangeOfficerTimer timer = roObj.AddComponent<RangeOfficerTimer>();

            // Injeta propriedades privadas do timer via reflexão amigável
            SetFieldValue(timer, "audioSource", roAudio);
            SetFieldValue(timer, "beepSound", ProceduralAudio.CreateTimerBeepClip());
            SetFieldValue(timer, "stageTargets", targetsList);

            // 9. Canvas e UI
            CreateIPSCUI(root.transform);

            Debug.Log("[IPSC Setup] Estande montado com sucesso! Pressione PLAY e aperte 'T' para iniciar a pista.");
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
            float thickness = 0.06f;

            CreateRip(box.transform, new Vector3(0, 0.02f, size * 0.5f), new Vector3(size, 0.03f, thickness), mat);
            CreateRip(box.transform, new Vector3(0, 0.02f, -size * 0.5f), new Vector3(size, 0.03f, thickness), mat);
            CreateRip(box.transform, new Vector3(size * 0.5f, 0.02f, 0), new Vector3(thickness, 0.03f, size), mat);
            CreateRip(box.transform, new Vector3(-size * 0.5f, 0.02f, 0), new Vector3(thickness, 0.03f, size), mat);
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
            decalMat.SetFloat("_Mode", 2); // Fade / Transparent
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
            bullet.transform.localScale = new Vector3(0.02f, 0.02f, 0.06f);

            Material bulletMat = ProceduralMaterials.CreateMaterial(new Color(0.85f, 0.65f, 0.2f), "Mat_BrassBullet", 0.8f);
            bullet.GetComponent<MeshRenderer>().material = bulletMat;

            // Remove o colisor convencional da Unity; a detecção é via Raycast contínuo
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
            Object.DestroyImmediate(board.GetComponent<Collider>()); // Colisores ficam nas zonas

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

        private static GameObject CreatePlayerWithWeapon(Transform parent, GameObject bulletPrefab, GameObject decalPrefab, Material matMetal, Material matSlide, Material matDot)
        {
            GameObject player = new GameObject("Player");
            player.transform.SetParent(parent);
            player.transform.position = new Vector3(0f, 1.0f, 0f);

            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0, 0.9f, 0);

            PlayerController controller = player.AddComponent<PlayerController>();

            // Câmera
            GameObject camObj = new GameObject("PlayerCamera");
            camObj.transform.SetParent(player.transform);
            camObj.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            Camera cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
            cam.fieldOfView = 75f;
            cam.nearClipPlane = 0.02f;

            SetFieldValue(controller, "playerCameraTransform", camObj.transform);

            // Suporte da Arma (WeaponHolder)
            GameObject holder = new GameObject("WeaponHolder");
            holder.transform.SetParent(camObj.transform);
            holder.transform.localPosition = new Vector3(0.18f, -0.22f, 0.42f); // Posição de quadril

            WeaponHandling handling = holder.AddComponent<WeaponHandling>();

            // Construção 3D Procedural da Pistola
            GameObject pistol = new GameObject("Pistol_9mm");
            pistol.transform.SetParent(holder.transform);
            pistol.transform.localPosition = Vector3.zero;
            pistol.transform.localRotation = Quaternion.identity;

            // Armação (Grip/Frame)
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Pistol_Frame";
            frame.transform.SetParent(pistol.transform);
            frame.transform.localPosition = new Vector3(0f, -0.04f, 0.02f);
            frame.transform.localScale = new Vector3(0.03f, 0.11f, 0.05f);
            frame.transform.localRotation = Quaternion.Euler(15f, 0, 0);
            frame.GetComponent<MeshRenderer>().material = matMetal;
            Object.DestroyImmediate(frame.GetComponent<Collider>());

            // Ferrolho móvel (Slide)
            GameObject slide = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slide.name = "Pistol_Slide";
            slide.transform.SetParent(pistol.transform);
            slide.transform.localPosition = new Vector3(0f, 0.025f, 0.04f);
            slide.transform.localScale = new Vector3(0.032f, 0.035f, 0.18f);
            slide.GetComponent<MeshRenderer>().material = matSlide;
            Object.DestroyImmediate(slide.GetComponent<Collider>());

            // Aparelho de Pontaria: Massa de Mira (Front Sight com fibra ótica)
            GameObject frontSight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frontSight.name = "FrontSight";
            frontSight.transform.SetParent(slide.transform);
            frontSight.transform.localPosition = new Vector3(0f, 0.6f, 0.45f);
            frontSight.transform.localScale = new Vector3(0.15f, 0.35f, 0.08f);
            frontSight.GetComponent<MeshRenderer>().material = matDot;
            Object.DestroyImmediate(frontSight.GetComponent<Collider>());

            // Alça de Mira (Rear Sight com entalhe)
            GameObject rearSightL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rearSightL.name = "RearSight_Left";
            rearSightL.transform.SetParent(slide.transform);
            rearSightL.transform.localPosition = new Vector3(-0.25f, 0.6f, -0.45f);
            rearSightL.transform.localScale = new Vector3(0.18f, 0.35f, 0.08f);
            rearSightL.GetComponent<MeshRenderer>().material = matMetal;
            Object.DestroyImmediate(rearSightL.GetComponent<Collider>());

            GameObject rearSightR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rearSightR.name = "RearSight_Right";
            rearSightR.transform.SetParent(slide.transform);
            rearSightR.transform.localPosition = new Vector3(0.25f, 0.6f, -0.45f);
            rearSightR.transform.localScale = new Vector3(0.18f, 0.35f, 0.08f);
            rearSightR.GetComponent<MeshRenderer>().material = matMetal;
            Object.DestroyImmediate(rearSightR.GetComponent<Collider>());

            // Muzzle Point (cano real da arma com Height-Over-Bore)
            GameObject muzzle = new GameObject("MuzzlePoint");
            muzzle.transform.SetParent(pistol.transform);
            muzzle.transform.localPosition = new Vector3(0f, 0.015f, 0.14f); // Abaixo da linha de mira!

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

            // Configura WeaponHandling
            SetFieldValue(handling, "weapon", weapon);
            SetFieldValue(handling, "weaponTransform", holder.transform);
            SetFieldValue(handling, "hipPosition", new Vector3(0.18f, -0.22f, 0.42f));
            SetFieldValue(handling, "adsPosition", new Vector3(0f, -0.0465f, 0.32f)); // Alinhamento matemático com a mira frontal!

            return player;
        }

        private static void CreateIPSCUI(Transform parent)
        {
            GameObject canvasObj = new GameObject("IPSC_HUD_Canvas");
            canvasObj.transform.SetParent(parent);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            Font defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Painel Superior do Shot Timer
            GameObject timerPanel = CreateUIPanel(canvasObj.transform, "TimerPanel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(360, 90), new Color(0, 0, 0, 0.65f));

            Text statusText = CreateUIText(timerPanel.transform, "StatusText", "Pressione [T] para Standby", 16, defaultFont, Color.yellow, new Vector2(0, 20), TextAnchor.MiddleCenter);
            Text timerText = CreateUIText(timerPanel.transform, "TimerText", "00.00s", 34, defaultFont, Color.white, new Vector2(0, -15), TextAnchor.MiddleCenter);

            // Split lateral
            Text splitText = CreateUIText(canvasObj.transform, "SplitText", "Split: --", 20, defaultFont, Color.cyan, new Vector2(160, -115), TextAnchor.MiddleLeft);

            // Contador de Munição (inferior direito)
            GameObject ammoPanel = CreateUIPanel(canvasObj.transform, "AmmoPanel", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-100, 50), new Vector2(160, 60), new Color(0, 0, 0, 0.7f));
            Text ammoText = CreateUIText(ammoPanel.transform, "AmmoText", "17 / 17", 26, defaultFont, Color.white, Vector2.zero, TextAnchor.MiddleCenter);

            // Painel de Resultados (Sumário Final)
            GameObject resultPanel = CreateUIPanel(canvasObj.transform, "ResultPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500, 380), new Color(0.08f, 0.08f, 0.1f, 0.94f));
            Text summaryText = CreateUIText(resultPanel.transform, "SummaryText", "Resumo da Pista", 18, defaultFont, Color.white, Vector2.zero, TextAnchor.MiddleCenter);
            resultPanel.SetActive(false);

            // Script UI Manager
            IPSCUIManager uiManager = canvasObj.AddComponent<IPSCUIManager>();
            SetFieldValue(uiManager, "timerStatusText", statusText);
            SetFieldValue(uiManager, "currentTimeText", timerText);
            SetFieldValue(uiManager, "lastSplitText", splitText);
            SetFieldValue(uiManager, "ammoCounterText", ammoText);
            SetFieldValue(uiManager, "resultPanel", resultPanel);
            SetFieldValue(uiManager, "resultSummaryText", summaryText);
        }

        private static GameObject CreateUIPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            Image img = panel.AddComponent<Image>();
            img.color = color;
            return panel;
        }

        private static Text CreateUIText(Transform parent, string name, string text, int fontSize, Font font, Color color, Vector2 anchoredPos, TextAnchor align)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(480, 280);

            Text t = textObj.AddComponent<Text>();
            t.text = text;
            t.font = font;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = align;
            return t;
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
