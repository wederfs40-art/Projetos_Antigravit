using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace IPSCSimulator.UI
{
    using IPSCSimulator.Match;
    using IPSCSimulator.Setup;
    using IPSCSimulator.Weapons;

    /// <summary>
    /// Gerenciador de UI/UX para o Menu Principal e Tela de Arsenal (Loadout) estilo Counter-Strike 2.
    /// Utiliza Unity Canvas UGUI, TextMeshPro, GridLayoutGroup e WeaponData (ScriptableObjects):
    /// 1. Quadros por Tipo/Categoria com grid dinâmico de modelos e barras de atributos.
    /// 2. Preview 3D da arma em tempo real com turntable e spotlight na bancada do estande.
    /// 3. Painel de seleção de cenários/pistas (Indoor, Outdoor, Precisão e Combate).
    /// </summary>
    public class LoadoutMenuManager : MonoBehaviour
    {
        public static LoadoutMenuManager Instance { get; private set; }

        [Header("1. Banco de Armas Registradas (WeaponData)")]
        [Tooltip("Lista de ScriptableObjects de armamento disponíveis no arsenal.")]
        [SerializeField] private List<WeaponData> registeredWeapons = new List<WeaponData>();

        [Header("2. Painéis Principais do Menu")]
        [SerializeField] private GameObject loadoutPanel;
        [SerializeField] private GameObject mapSelectionPanel;

        [Header("3. Grade de Armas (Grid Layout)")]
        [SerializeField] private Transform weaponGridContainer;
        [SerializeField] private GridLayoutGroup gridLayoutGroup;

        [Header("4. Abas de Categorias")]
        [SerializeField] private Button btnPistolas;
        [SerializeField] private Button btnEspingardas;
        [SerializeField] private Button btnSubmetralhadoras;
        [SerializeField] private Button btnFuzis;
        [SerializeField] private Button btnSnipers;
        [SerializeField] private Button btnEquipamentos;

        [Header("5. Preview 3D em Tempo Real (Bancada & Turntable)")]
        [Tooltip("Ponto no espaço 3D (bancada de tiro) onde a arma selecionada será exibida.")]
        [SerializeField] private Transform previewPedestalTransform;

        [Tooltip("Spotlight que ilumina com destaque os detalhes metálicos e normais da arma.")]
        [SerializeField] private Light previewSpotlight;

        [Tooltip("Velocidade de rotação suave da arma na bancada.")]
        [SerializeField] private float turntableRotationSpeed = 16f;
        [SerializeField] private bool rotateTurntable = true;

        [Header("6. Seleção de Cenários")]
        [SerializeField] private Button btnIndoorStand;
        [SerializeField] private Button btnOutdoorStand;
        [SerializeField] private Button btnPrecisionIPSC;
        [SerializeField] private Button btnCombatCQB;
        [SerializeField] private Button btnStartSimulation;
        [SerializeField] private TextMeshProUGUI selectedMapDescriptionText;

        // Estados internos
        private WeaponCategory _activeCategory = WeaponCategory.Pistolas;
        private WeaponData _selectedWeaponData;
        private SelectedWeaponType _selectedWeaponType = SelectedWeaponType.Pistol_9mm;
        private GameEnvironmentType _selectedEnvironment = GameEnvironmentType.Outdoor_Precision_Stand;
        private GameObject _spawned3DPreviewInstance;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);

            InitializeDefaultWeaponDatabaseIfEmpty();
        }

        private void Start()
        {
            SetupNavigationListeners();
            SelectCategory(WeaponCategory.Pistolas);
            SelectEnvironment(GameEnvironmentType.Outdoor_Precision_Stand);
            SetupPreviewEnvironment();
        }

        private void Update()
        {
            // Rotação suave da arma na bancada 3D
            if (rotateTurntable && _spawned3DPreviewInstance != null)
            {
                _spawned3DPreviewInstance.transform.Rotate(Vector3.up, turntableRotationSpeed * Time.deltaTime, Space.World);
            }
        }

        // ========================================================================================
        // 1. NAVEGAÇÃO DE CATEGORIAS E FILTRAGEM NA GRADE (GRID LAYOUT)
        // ========================================================================================
        private void SetupNavigationListeners()
        {
            if (btnPistolas != null) btnPistolas.onClick.AddListener(() => SelectCategory(WeaponCategory.Pistolas));
            if (btnEspingardas != null) btnEspingardas.onClick.AddListener(() => SelectCategory(WeaponCategory.Espingardas));
            if (btnSubmetralhadoras != null) btnSubmetralhadoras.onClick.AddListener(() => SelectCategory(WeaponCategory.Submetralhadoras));
            if (btnFuzis != null) btnFuzis.onClick.AddListener(() => SelectCategory(WeaponCategory.Fuzis));
            if (btnSnipers != null) btnSnipers.onClick.AddListener(() => SelectCategory(WeaponCategory.Snipers));
            if (btnEquipamentos != null) btnEquipamentos.onClick.AddListener(() => SelectCategory(WeaponCategory.Equipamentos));

            if (btnIndoorStand != null) btnIndoorStand.onClick.AddListener(() => SelectEnvironment(GameEnvironmentType.Indoor_Precision_Stand));
            if (btnOutdoorStand != null) btnOutdoorStand.onClick.AddListener(() => SelectEnvironment(GameEnvironmentType.Outdoor_Precision_Stand));
            if (btnPrecisionIPSC != null) btnPrecisionIPSC.onClick.AddListener(() => SelectEnvironment(GameEnvironmentType.IPSC_Outdoor_Short));
            if (btnCombatCQB != null) btnCombatCQB.onClick.AddListener(() => SelectEnvironment(GameEnvironmentType.Tactical_CS_Killhouse));

            if (btnStartSimulation != null) btnStartSimulation.onClick.AddListener(StartSimulationGame);
        }

        /// <summary>
        /// Seleciona uma categoria de armas e atualiza a grade de modelos correspondentes.
        /// </summary>
        public void SelectCategory(WeaponCategory category)
        {
            _activeCategory = category;
            HighlightCategoryTab(category);
            RebuildWeaponGridForCategory(category);
        }

        private void HighlightCategoryTab(WeaponCategory cat)
        {
            UpdateButtonHighlight(btnPistolas, cat == WeaponCategory.Pistolas);
            UpdateButtonHighlight(btnEspingardas, cat == WeaponCategory.Espingardas);
            UpdateButtonHighlight(btnSubmetralhadoras, cat == WeaponCategory.Submetralhadoras);
            UpdateButtonHighlight(btnFuzis, cat == WeaponCategory.Fuzis);
            UpdateButtonHighlight(btnSnipers, cat == WeaponCategory.Snipers);
            UpdateButtonHighlight(btnEquipamentos, cat == WeaponCategory.Equipamentos);
        }

        private void UpdateButtonHighlight(Button btn, bool isSelected)
        {
            if (btn == null) return;
            ColorBlock cb = btn.colors;
            cb.normalColor = isSelected ? new Color(0.95f, 0.75f, 0.20f, 1f) : new Color(0.12f, 0.15f, 0.20f, 0.85f);
            btn.colors = cb;
        }

        private void RebuildWeaponGridForCategory(WeaponCategory category)
        {
            if (weaponGridContainer == null) return;

            // Limpa slots anteriores
            for (int i = weaponGridContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(weaponGridContainer.GetChild(i).gameObject);
            }

            List<WeaponData> categoryWeapons = registeredWeapons.FindAll(w => w != null && w.Category == category);

            // Se não houver armas cadastradas para esta categoria, cria slots representativos
            if (categoryWeapons.Count == 0)
            {
                CreateEmptyCategoryNotice();
                return;
            }

            // Instancia cada card na grade
            foreach (WeaponData weapon in categoryWeapons)
            {
                CreateWeaponCard(weapon);
            }
        }

        private void CreateWeaponCard(WeaponData weapon)
        {
            GameObject cardObj = new GameObject($"Card_{weapon.WeaponName}");
            cardObj.transform.SetParent(weaponGridContainer, false);

            RectTransform rt = cardObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(250f, 290f);

            bool isEquipped = (_selectedWeaponData == weapon);

            // Fundo translúcido escuro
            Image bg = cardObj.AddComponent<Image>();
            bg.color = isEquipped ? new Color(0.15f, 0.35f, 0.55f, 0.90f) : new Color(0.07f, 0.09f, 0.13f, 0.85f);

            Button cardBtn = cardObj.AddComponent<Button>();
            cardBtn.onClick.AddListener(() => EquipWeapon(weapon));

            // 1. Ícone / Miniatura 2D
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(cardObj.transform, false);
            RectTransform iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.45f);
            iconRt.anchorMax = new Vector2(1f, 0.95f);
            iconRt.offsetMin = new Vector2(10f, 0f);
            iconRt.offsetMax = new Vector2(-10f, -5f);
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = weapon.WeaponIcon;
            iconImg.color = (weapon.WeaponIcon != null) ? Color.white : new Color(0.4f, 0.5f, 0.6f, 0.4f);
            iconImg.preserveAspect = true;

            // 2. Nome da Arma
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(cardObj.transform, false);
            RectTransform nameRt = nameObj.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 0.33f);
            nameRt.anchorMax = new Vector2(1f, 0.45f);
            nameRt.offsetMin = new Vector2(8f, 0f);
            nameRt.offsetMax = new Vector2(-8f, 0f);
            TextMeshProUGUI nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
            nameTmp.text = weapon.WeaponName.ToUpper();
            nameTmp.fontSize = 15;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.color = Color.white;

            // 3. Barras de Atributos: Dano, Precisão e Cadência
            CreateStatBar(cardObj.transform, "Bar_Damage", "DANO", weapon.DamageStat / 100f, new Vector2(0f, 0.22f), new Vector2(1f, 0.31f), new Color(1f, 0.35f, 0.35f));
            CreateStatBar(cardObj.transform, "Bar_Accuracy", "PRECISÃO", weapon.AccuracyStat / 100f, new Vector2(0f, 0.12f), new Vector2(1f, 0.21f), new Color(0.35f, 0.85f, 1f));
            CreateStatBar(cardObj.transform, "Bar_FireRate", "CADÊNCIA", weapon.FireRateStat / 100f, new Vector2(0f, 0.02f), new Vector2(1f, 0.11f), new Color(1f, 0.85f, 0.35f));

            // 4. Selo de Equipado
            if (isEquipped)
            {
                GameObject badgeObj = new GameObject("EquippedBadge");
                badgeObj.transform.SetParent(cardObj.transform, false);
                RectTransform badgeRt = badgeObj.AddComponent<RectTransform>();
                badgeRt.anchorMin = new Vector2(0f, 0.90f);
                badgeRt.anchorMax = new Vector2(1f, 1f);
                TextMeshProUGUI badgeTmp = badgeObj.AddComponent<TextMeshProUGUI>();
                badgeTmp.text = "[ ✓ EQUIPADA ]";
                badgeTmp.fontSize = 11;
                badgeTmp.fontStyle = FontStyles.Bold;
                badgeTmp.alignment = TextAlignmentOptions.Center;
                badgeTmp.color = new Color(0f, 1f, 0.65f);
            }
        }

        private void CreateStatBar(Transform parent, string name, string label, float fillAmount, Vector2 anchorMin, Vector2 anchorMax, Color barColor)
        {
            GameObject barObj = new GameObject(name);
            barObj.transform.SetParent(parent, false);
            RectTransform barRt = barObj.AddComponent<RectTransform>();
            barRt.anchorMin = anchorMin;
            barRt.anchorMax = anchorMax;
            barRt.offsetMin = new Vector2(12f, 0f);
            barRt.offsetMax = new Vector2(-12f, 0f);

            // Rótulo
            GameObject lblObj = new GameObject("Label");
            lblObj.transform.SetParent(barObj.transform, false);
            RectTransform lblRt = lblObj.AddComponent<RectTransform>();
            lblRt.anchorMin = new Vector2(0f, 0f);
            lblRt.anchorMax = new Vector2(0.4f, 1f);
            TextMeshProUGUI lblTmp = lblObj.AddComponent<TextMeshProUGUI>();
            lblTmp.text = label;
            lblTmp.fontSize = 9;
            lblTmp.color = new Color(0.75f, 0.78f, 0.82f);
            lblTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Fundo da barra
            GameObject bgObj = new GameObject("Bg");
            bgObj.transform.SetParent(barObj.transform, false);
            RectTransform bgRt = bgObj.AddComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0.4f, 0.2f);
            bgRt.anchorMax = new Vector2(1f, 0.8f);
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.18f, 0.22f, 1f);

            // Preenchimento proporcional
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(bgObj.transform, false);
            RectTransform fillRt = fillObj.AddComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(Mathf.Clamp01(fillAmount), 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.color = barColor;
        }

        private void CreateEmptyCategoryNotice()
        {
            GameObject noticeObj = new GameObject("Notice_Empty");
            noticeObj.transform.SetParent(weaponGridContainer, false);
            RectTransform rt = noticeObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(500f, 80f);
            TextMeshProUGUI tmp = noticeObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "<i>Nenhum modelo disponível nesta categoria no momento.</i>";
            tmp.fontSize = 16;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.6f, 0.65f, 0.75f, 0.8f);
        }

        // ========================================================================================
        // 2. PREVIEW 3D EM TEMPO REAL & EQUIPAMENTO DO ARSENAL
        // ========================================================================================
        /// <summary>
        /// Equipa a arma escolhida e atualiza imediatamente o modelo 3D em exibição no estande.
        /// </summary>
        public void EquipWeapon(WeaponData weapon)
        {
            _selectedWeaponData = weapon;

            // Sincroniza com SelectedWeaponType do GameManager
            if (Enum.TryParse<SelectedWeaponType>(weapon.name.Replace("Data_", ""), true, out SelectedWeaponType parsedType))
            {
                _selectedWeaponType = parsedType;
            }
            else
            {
                // Fallback para tipos conhecidos
                string lower = weapon.WeaponName.ToLower();
                if (lower.Contains("akm")) _selectedWeaponType = SelectedWeaponType.Rifle_AKM_Tactical;
                else if (lower.Contains("wesson")) _selectedWeaponType = SelectedWeaponType.Revolver_DanWesson_357;
                else if (lower.Contains("strongarm") || lower.Contains("mp5")) _selectedWeaponType = SelectedWeaponType.SMG_Strongarm_Tactical;
                else _selectedWeaponType = SelectedWeaponType.Pistol_Beretta_M9T_Black;
            }

            if (TacticalGameManager.Instance != null)
            {
                TacticalGameManager.Instance.SelectedWeapon = _selectedWeaponType;
            }

            Update3DPreview(weapon);
            RebuildWeaponGridForCategory(_activeCategory);
        }

        private void Update3DPreview(WeaponData weapon)
        {
            if (_spawned3DPreviewInstance != null)
            {
                Destroy(_spawned3DPreviewInstance);
            }

            if (previewPedestalTransform == null)
            {
                SetupPreviewEnvironment();
            }

            // 1. Instancia o prefab 3D caso exista no WeaponData
            if (weapon.WeaponPrefab != null)
            {
                _spawned3DPreviewInstance = Instantiate(weapon.WeaponPrefab, previewPedestalTransform.position, Quaternion.Euler(0f, 45f, 0f));
                _spawned3DPreviewInstance.transform.SetParent(previewPedestalTransform);
            }
            else
            {
                // Constrói um modelo representativo da arma para o showcase caso o prefab esteja nulo
                _spawned3DPreviewInstance = CreateShowcaseMesh(weapon);
                _spawned3DPreviewInstance.transform.position = previewPedestalTransform.position + new Vector3(0f, 0.15f, 0f);
                _spawned3DPreviewInstance.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
                _spawned3DPreviewInstance.transform.SetParent(previewPedestalTransform);
            }

            // Ativa o holofote de destaque
            if (previewSpotlight != null)
            {
                previewSpotlight.enabled = true;
                previewSpotlight.transform.LookAt(_spawned3DPreviewInstance.transform.position);
            }
        }

        private void SetupPreviewEnvironment()
        {
            if (previewPedestalTransform != null) return;

            // Cria uma bancada de estande de tiro fotorrealista para o Preview
            GameObject pedestalObj = new GameObject("3D_WeaponPreview_Pedestal");
            pedestalObj.transform.position = new Vector3(0.85f, 0.85f, 2.2f);

            GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "TableMesh";
            table.transform.SetParent(pedestalObj.transform);
            table.transform.localPosition = new Vector3(0f, -0.4f, 0f);
            table.transform.localScale = new Vector3(1.2f, 0.8f, 0.8f);

            previewPedestalTransform = pedestalObj.transform;

            // Spotlight com iluminação de estúdio para realçar os detalhes PBR
            GameObject lightObj = new GameObject("Preview_StudioSpotlight");
            lightObj.transform.SetParent(pedestalObj.transform);
            lightObj.transform.localPosition = new Vector3(0.5f, 1.8f, -0.6f);

            previewSpotlight = lightObj.AddComponent<Light>();
            previewSpotlight.type = LightType.Spot;
            previewSpotlight.color = new Color(0.95f, 0.98f, 1.0f);
            previewSpotlight.intensity = 3.5f;
            previewSpotlight.spotAngle = 60f;
            previewSpotlight.range = 5.0f;
            previewSpotlight.shadows = LightShadows.Soft;
            lightObj.transform.LookAt(pedestalObj.transform.position);
        }

        private GameObject CreateShowcaseMesh(WeaponData weapon)
        {
            GameObject root = new GameObject($"Showcase_{weapon.WeaponName}");
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(root.transform);
            body.transform.localScale = new Vector3(0.04f, 0.12f, 0.28f);

            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(0.12f, 0.14f, 0.18f);
            mat.SetFloat("_Smoothness", 0.85f);
            mat.SetFloat("_Metallic", 0.70f);
            body.GetComponent<MeshRenderer>().material = mat;

            return root;
        }

        // ========================================================================================
        // 3. SELEÇÃO DE PISTAS E TRANSIÇÃO DE JOGO
        // ========================================================================================
        /// <summary>
        /// Seleciona o ambiente de tiro e atualiza a descrição na interface.
        /// </summary>
        public void SelectEnvironment(GameEnvironmentType env)
        {
            _selectedEnvironment = env;

            if (TacticalGameManager.Instance != null)
            {
                TacticalGameManager.Instance.SelectedEnvironment = env;
            }

            UpdateButtonHighlight(btnIndoorStand, env == GameEnvironmentType.Indoor_Precision_Stand);
            UpdateButtonHighlight(btnOutdoorStand, env == GameEnvironmentType.Outdoor_Precision_Stand);
            UpdateButtonHighlight(btnPrecisionIPSC, env == GameEnvironmentType.IPSC_Outdoor_Short);
            UpdateButtonHighlight(btnCombatCQB, env == GameEnvironmentType.Tactical_CS_Killhouse || env == GameEnvironmentType.Bodycam_ShootHouse);

            if (selectedMapDescriptionText != null)
            {
                switch (env)
                {
                    case GameEnvironmentType.Indoor_Precision_Stand:
                        selectedMapDescriptionText.text = "<b>ESTANDE INDOOR:</b> Cabines acústicas individuais, alvos em trilhos de 5m a 25m com iluminação fluorescente e piso polido.";
                        break;
                    case GameEnvironmentType.Outdoor_Precision_Stand:
                        selectedMapDescriptionText.text = "<b>ESTANDE OUTDOOR:</b> Pista aberta sob sol forte, piso de cascalho claro, berma de contenção e alvos de silhueta com alça/massa de mira alinhadas.";
                        break;
                    case GameEnvironmentType.IPSC_Outdoor_Short:
                        selectedMapDescriptionText.text = "<b>TIRO DE PRECISÃO IPSC:</b> Pista rápida (Speed Option) com 3 alvos de papelão e 2 poppers metálicos reativos para treino de split time.";
                        break;
                    case GameEnvironmentType.Tactical_CS_Killhouse:
                    case GameEnvironmentType.Bodycam_ShootHouse:
                        selectedMapDescriptionText.text = "<b>PISTA DE COMBATE CQB:</b> Galpão tático com contêineres marítimos, caixas militares e alvos hostis em ambiente urbano.";
                        break;
                }
            }
        }

        /// <summary>
        /// Inicia a simulação com a pista e arma selecionadas.
        /// </summary>
        public void StartSimulationGame()
        {
            if (TacticalGameManager.Instance != null)
            {
                TacticalGameManager.Instance.StartGame(_selectedEnvironment, _selectedWeaponType);
            }
            else
            {
                SceneGeneratorCore.BuildSelectedEnvironment(_selectedEnvironment, _selectedWeaponType);
            }

            // Oculta o menu
            gameObject.SetActive(false);
        }

        // ========================================================================================
        // 4. INICIALIZAÇÃO DE DADOS PADRÃO (FALLBACK ROBUSTO)
        // ========================================================================================
        private void InitializeDefaultWeaponDatabaseIfEmpty()
        {
            if (registeredWeapons.Count > 0)
            {
                if (_selectedWeaponData == null) _selectedWeaponData = registeredWeapons[0];
                return;
            }

            // Cria catálogo automático com os 10 modelos existentes no projeto
            registeredWeapons.Add(CreateDefaultData("Beretta M9T (Black)", WeaponCategory.Pistolas, 52f, 85f, 55f));
            registeredWeapons.Add(CreateDefaultData("Beretta M9T (Tan)", WeaponCategory.Pistolas, 52f, 85f, 55f));
            registeredWeapons.Add(CreateDefaultData("Glock 17 Tactical", WeaponCategory.Pistolas, 48f, 80f, 62f));
            registeredWeapons.Add(CreateDefaultData("Colt 1911 Custom", WeaponCategory.Pistolas, 65f, 75f, 45f));

            registeredWeapons.Add(CreateDefaultData("Dan Wesson 715 .357", WeaponCategory.Pistolas, 78f, 90f, 30f));
            registeredWeapons.Add(CreateDefaultData("Superhawk .44 Mag", WeaponCategory.Pistolas, 88f, 82f, 25f));

            registeredWeapons.Add(CreateDefaultData("MP5 Strongarm", WeaponCategory.Submetralhadoras, 55f, 78f, 85f));
            registeredWeapons.Add(CreateDefaultData("UMP-45 Tactical", WeaponCategory.Submetralhadoras, 68f, 72f, 70f));
            registeredWeapons.Add(CreateDefaultData("P90 Bullpup", WeaponCategory.Submetralhadoras, 45f, 84f, 95f));

            registeredWeapons.Add(CreateDefaultData("AKM 7.62x39mm", WeaponCategory.Fuzis, 85f, 74f, 65f));

            // Modelos representativos para preenchimento de catálogo
            registeredWeapons.Add(CreateDefaultData("Nova Pump 12GA", WeaponCategory.Espingardas, 95f, 45f, 28f));
            registeredWeapons.Add(CreateDefaultData("M200 CheyTac Sniper", WeaponCategory.Snipers, 100f, 98f, 15f));
            registeredWeapons.Add(CreateDefaultData("Granada Flashbang", WeaponCategory.Equipamentos, 10f, 90f, 10f));

            _selectedWeaponData = registeredWeapons[0];
        }

        private WeaponData CreateDefaultData(string name, WeaponCategory cat, float dmg, float acc, float rate)
        {
            WeaponData w = ScriptableObject.CreateInstance<WeaponData>();
            w.name = $"Data_{name.Replace(" ", "_")}";

            // Reflexão segura para preenchimento dos campos privados
            SetField(w, "weaponName", name);
            SetField(w, "category", cat);
            SetField(w, "damageStat", dmg);
            SetField(w, "accuracyStat", acc);
            SetField(w, "fireRateStat", rate);
            SetField(w, "adsLocalPosition", new Vector3(0f, -0.046f, 0.16f));
            SetField(w, "adsSpeed", 14f);
            SetField(w, "adsCameraFOV", 48f);

            return w;
        }

        private void SetField(object obj, string name, object value)
        {
            var f = obj.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (f != null) f.SetValue(obj, value);
        }

        // ========================================================================================
        // 5. GERADOR PROCEDURAL COMPLETO DO CANVAS DE LOADOUT
        // ========================================================================================
        /// <summary>
        /// Gera toda a hierarquia de Canvas, painéis, abas, Grid Layout e botões automaticamente na cena.
        /// </summary>
        public static LoadoutMenuManager CreateProceduralLoadoutCanvas()
        {
            LoadoutMenuManager existing = FindAnyObjectByType<LoadoutMenuManager>();
            if (existing != null) Destroy(existing.gameObject);

            GameObject canvasObj = new GameObject("CS2_Loadout_MainMenu_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            LoadoutMenuManager mgr = canvasObj.AddComponent<LoadoutMenuManager>();

            // ------------------------------------------------------------------------------------
            // A) CABEÇALHO DO MENU
            // ------------------------------------------------------------------------------------
            GameObject headerObj = new GameObject("HeaderPanel");
            headerObj.transform.SetParent(canvasObj.transform, false);
            RectTransform hRt = headerObj.AddComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, 0.90f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.offsetMin = Vector2.zero;
            hRt.offsetMax = Vector2.zero;

            Image hBg = headerObj.AddComponent<Image>();
            hBg.color = new Color(0.04f, 0.05f, 0.08f, 0.85f);

            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(headerObj.transform, false);
            RectTransform tRt = titleObj.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.04f, 0f);
            tRt.anchorMax = new Vector2(0.50f, 1f);
            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "<b>TACTICAL SHOOTER & ARSENAL</b> <color=#FFD700>LOADOUT</color>";
            titleTmp.fontSize = 26;
            titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
            titleTmp.color = Color.white;

            // ------------------------------------------------------------------------------------
            // B) ABAS DE CATEGORIAS (TOPO)
            // ------------------------------------------------------------------------------------
            GameObject tabsBar = new GameObject("CategoryTabsBar");
            tabsBar.transform.SetParent(canvasObj.transform, false);
            RectTransform tbRt = tabsBar.AddComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0.04f, 0.83f);
            tbRt.anchorMax = new Vector2(0.68f, 0.89f);
            tbRt.offsetMin = Vector2.zero;
            tbRt.offsetMax = Vector2.zero;

            HorizontalLayoutGroup hlg = tabsBar.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;

            mgr.btnPistolas = CreateTabButton(tabsBar.transform, "PISTOLAS");
            mgr.btnEspingardas = CreateTabButton(tabsBar.transform, "ESPINGARDAS");
            mgr.btnSubmetralhadoras = CreateTabButton(tabsBar.transform, "SMGs");
            mgr.btnFuzis = CreateTabButton(tabsBar.transform, "FUZIS");
            mgr.btnSnipers = CreateTabButton(tabsBar.transform, "SNIPERS");
            mgr.btnEquipamentos = CreateTabButton(tabsBar.transform, "EQUIPAMENTOS");

            // ------------------------------------------------------------------------------------
            // C) QUADRO DE GRADE DE ARMAS (GRID LAYOUT)
            // ------------------------------------------------------------------------------------
            GameObject gridPanel = new GameObject("WeaponGridPanel");
            gridPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform gpRt = gridPanel.AddComponent<RectTransform>();
            gpRt.anchorMin = new Vector2(0.04f, 0.12f);
            gpRt.anchorMax = new Vector2(0.68f, 0.81f);
            gpRt.offsetMin = Vector2.zero;
            gpRt.offsetMax = Vector2.zero;

            Image gpBg = gridPanel.AddComponent<Image>();
            gpBg.color = new Color(0.04f, 0.05f, 0.08f, 0.65f);

            // Container de Scroll / Grid
            GameObject gridContainer = new GameObject("GridContainer");
            gridContainer.transform.SetParent(gridPanel.transform, false);
            RectTransform gcRt = gridContainer.AddComponent<RectTransform>();
            gcRt.anchorMin = Vector2.zero;
            gcRt.anchorMax = Vector2.one;
            gcRt.offsetMin = new Vector2(15f, 15f);
            gcRt.offsetMax = new Vector2(-15f, -15f);

            GridLayoutGroup glg = gridContainer.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(250f, 290f);
            glg.spacing = new Vector2(16f, 16f);
            glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
            glg.startAxis = GridLayoutGroup.Axis.Horizontal;
            glg.childAlignment = TextAnchor.UpperLeft;

            mgr.weaponGridContainer = gridContainer.transform;
            mgr.gridLayoutGroup = glg;
            mgr.loadoutPanel = gridPanel;

            // ------------------------------------------------------------------------------------
            // D) PAINEL LATERAL DIREITO: SELEÇÃO DE PISTAS & INICIAR
            // ------------------------------------------------------------------------------------
            GameObject rightPanel = new GameObject("MapSelectionPanel");
            rightPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform rpRt = rightPanel.AddComponent<RectTransform>();
            rpRt.anchorMin = new Vector2(0.70f, 0.12f);
            rpRt.anchorMax = new Vector2(0.96f, 0.89f);
            rpRt.offsetMin = Vector2.zero;
            rpRt.offsetMax = Vector2.zero;

            Image rpBg = rightPanel.AddComponent<Image>();
            rpBg.color = new Color(0.05f, 0.06f, 0.10f, 0.85f);
            mgr.mapSelectionPanel = rightPanel;

            // Título do Painel de Pistas
            GameObject rpTitle = new GameObject("MapTitle");
            rpTitle.transform.SetParent(rightPanel.transform, false);
            RectTransform rptRt = rpTitle.AddComponent<RectTransform>();
            rptRt.anchorMin = new Vector2(0.05f, 0.90f);
            rptRt.anchorMax = new Vector2(0.95f, 0.98f);
            TextMeshProUGUI rptTmp = rpTitle.AddComponent<TextMeshProUGUI>();
            rptTmp.text = "SELEÇÃO DE CENÁRIO";
            rptTmp.fontSize = 18;
            rptTmp.fontStyle = FontStyles.Bold;
            rptTmp.alignment = TextAlignmentOptions.Center;
            rptTmp.color = new Color(0.95f, 0.85f, 0.35f);

            // Botões de Cenários
            mgr.btnOutdoorStand = CreateMapButton(rightPanel.transform, "☀️ ESTANDE OUTDOOR (Foco Visual)", new Vector2(0.05f, 0.77f), new Vector2(0.95f, 0.87f));
            mgr.btnIndoorStand = CreateMapButton(rightPanel.transform, "🎯 ESTANDE INDOOR DE PRECISÃO", new Vector2(0.05f, 0.65f), new Vector2(0.95f, 0.75f));
            mgr.btnPrecisionIPSC = CreateMapButton(rightPanel.transform, "⏱️ PISTA CURTA IPSC (Speed Option)", new Vector2(0.05f, 0.53f), new Vector2(0.95f, 0.63f));
            mgr.btnCombatCQB = CreateMapButton(rightPanel.transform, "🏢 PISTA DE COMBATE CQB (Bodycam)", new Vector2(0.05f, 0.41f), new Vector2(0.95f, 0.51f));

            // Descrição do Mapa
            GameObject descObj = new GameObject("MapDescriptionText");
            descObj.transform.SetParent(rightPanel.transform, false);
            RectTransform descRt = descObj.AddComponent<RectTransform>();
            descRt.anchorMin = new Vector2(0.06f, 0.18f);
            descRt.anchorMax = new Vector2(0.94f, 0.38f);
            TextMeshProUGUI descTmp = descObj.AddComponent<TextMeshProUGUI>();
            descTmp.fontSize = 13;
            descTmp.alignment = TextAlignmentOptions.TopLeft;
            descTmp.color = new Color(0.85f, 0.88f, 0.92f);
            mgr.selectedMapDescriptionText = descTmp;

            // Botão Principal de Iniciar
            GameObject startBtnObj = new GameObject("Btn_StartSimulation");
            startBtnObj.transform.SetParent(rightPanel.transform, false);
            RectTransform sBtnRt = startBtnObj.AddComponent<RectTransform>();
            sBtnRt.anchorMin = new Vector2(0.05f, 0.04f);
            sBtnRt.anchorMax = new Vector2(0.95f, 0.14f);

            Image sBtnImg = startBtnObj.AddComponent<Image>();
            sBtnImg.color = new Color(0.12f, 0.68f, 0.35f, 1f);

            Button startBtn = startBtnObj.AddComponent<Button>();
            ColorBlock cb = startBtn.colors;
            cb.highlightedColor = new Color(0.15f, 0.82f, 0.42f, 1f);
            startBtn.colors = cb;
            mgr.btnStartSimulation = startBtn;

            GameObject sBtnTxt = new GameObject("Text");
            sBtnTxt.transform.SetParent(startBtnObj.transform, false);
            RectTransform sTxtRt = sBtnTxt.AddComponent<RectTransform>();
            sTxtRt.anchorMin = Vector2.zero;
            sTxtRt.anchorMax = Vector2.one;
            TextMeshProUGUI sTmp = sBtnTxt.AddComponent<TextMeshProUGUI>();
            sTmp.text = "▶ INICIAR SIMULAÇÃO";
            sTmp.fontSize = 18;
            sTmp.fontStyle = FontStyles.Bold;
            sTmp.alignment = TextAlignmentOptions.Center;
            sTmp.color = Color.white;

            return mgr;
        }

        private static Button CreateTabButton(Transform parent, string title)
        {
            GameObject btnObj = new GameObject($"BtnTab_{title}");
            btnObj.transform.SetParent(parent, false);

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.12f, 0.15f, 0.20f, 0.85f);

            Button btn = btnObj.AddComponent<Button>();

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform rt = txtObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = title;
            tmp.fontSize = 13;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btn;
        }

        private static Button CreateMapButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject btnObj = new GameObject($"BtnMap_{label}");
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.12f, 0.16f, 0.22f, 0.95f);

            Button btn = btnObj.AddComponent<Button>();

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(10f, 0f);
            txtRt.offsetMax = new Vector2(-10f, 0f);
            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 13;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;

            return btn;
        }
    }
}
