using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace IPSCSimulator.UI
{
    using IPSCSimulator.Match;
    using IPSCSimulator.Setup;
    using IPSCSimulator.Weapons;

    /// <summary>
    /// Gerenciador de UI/UX de alto nível para o Menu Principal e Arsenal (Loadout) estilo Counter-Strike 2.
    /// Funcionalidades integradas:
    /// 1. Navegação por abas de categorias com grade dinâmica rolável (ScrollRect + GridLayoutGroup).
    /// 2. Cards táticos refinados com silhuetas procedurais de armas, nomes e barras proporcionais sem estouro visual.
    /// 3. Garantia automática de EventSystem e controle inteligente de cursor (Lock/Unlock) e teclas de atalho [B] e [ESC].
    /// 4. Preview 3D em tempo real com turntable e iluminação spot na bancada.
    /// 5. Painel lateral de seleção de pistas (Indoor, Outdoor, Precisão e CQB) e Iniciar Simulação.
    /// </summary>
    public class LoadoutMenuManager : MonoBehaviour
    {
        public static LoadoutMenuManager Instance { get; private set; }

        [Header("1. Banco de Armas Registradas (WeaponData)")]
        [Tooltip("Lista de ScriptableObjects de armamento disponíveis no arsenal.")]
        [SerializeField] private List<WeaponData> registeredWeapons = new List<WeaponData>();

        [Header("2. Painéis Principais do Menu")]
        [SerializeField] private GameObject menuContentRoot;
        [SerializeField] private GameObject loadoutPanel;
        [SerializeField] private GameObject mapSelectionPanel;

        [Header("3. Grade de Armas (Grid & Scroll)")]
        [SerializeField] private Transform weaponGridContainer;
        [SerializeField] private GridLayoutGroup gridLayoutGroup;
        [SerializeField] private ScrollRect weaponScrollRect;

        [Header("4. Abas de Categorias")]
        [SerializeField] private Button btnPistolas;
        [SerializeField] private Button btnEspingardas;
        [SerializeField] private Button btnSubmetralhadoras;
        [SerializeField] private Button btnFuzis;
        [SerializeField] private Button btnSnipers;
        [SerializeField] private Button btnEquipamentos;

        [Header("5. Botão de Fechar e Atalho")]
        [SerializeField] private Button btnCloseMenu;

        [Header("6. Preview 3D em Tempo Real (Bancada & Turntable)")]
        [SerializeField] private Transform previewPedestalTransform;
        [SerializeField] private Light previewSpotlight;
        [SerializeField] private float turntableRotationSpeed = 18f;
        [SerializeField] private bool rotateTurntable = true;

        [Header("7. Seleção de Cenários")]
        [SerializeField] private Button btnIndoorStand;
        [SerializeField] private Button btnOutdoorStand;
        [SerializeField] private Button btnPrecisionIPSC;
        [SerializeField] private Button btnCombatCQB;
        [SerializeField] private Button btnStartSimulation;
        [SerializeField] private TextMeshProUGUI selectedMapDescriptionText;

        // Estados internos
        public bool IsMenuOpen { get; private set; } = true;
        private WeaponCategory _activeCategory = WeaponCategory.Pistolas;
        private WeaponData _selectedWeaponData;
        private SelectedWeaponType _selectedWeaponType = SelectedWeaponType.Pistol_9mm;
        private GameEnvironmentType _selectedEnvironment = GameEnvironmentType.Outdoor_Precision_Stand;
        private GameObject _spawned3DPreviewInstance;

        // Cache de sprites gerados proceduralmente
        private static Dictionary<string, Sprite> _proceduralSpriteCache = new Dictionary<string, Sprite>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // Garante que o EventSystem exista para que cliques no Canvas funcionem
            EnsureEventSystemExists();

            InitializeDefaultWeaponDatabaseIfEmpty();
        }

        private void Start()
        {
            SetupNavigationListeners();
            SelectCategory(WeaponCategory.Pistolas);
            SelectEnvironment(GameEnvironmentType.Outdoor_Precision_Stand);
            SetupPreviewEnvironment();

            // Se o jogo já estiver com estande ativo na cena, inicia com menu fechado pronto para jogar (e alternar via [B])
            if (GameObject.Find("--- IPSC SHOOTING RANGE ---") != null)
            {
                SetMenuOpen(false);
            }
            else
            {
                SetMenuOpen(true);
            }
        }

        private void Update()
        {
            // Tecla [B] ou [M]: Alterna o menu de arsenal estilo Counter-Strike 2
            if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.M))
            {
                ToggleMenu();
            }

            // Tecla [ESC]: Fecha o menu se estiver aberto
            if (IsMenuOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                SetMenuOpen(false);
            }

            // Rotação suave da arma na bancada 3D
            if (IsMenuOpen && rotateTurntable && _spawned3DPreviewInstance != null)
            {
                _spawned3DPreviewInstance.transform.Rotate(Vector3.up, turntableRotationSpeed * Time.deltaTime, Space.World);
            }
        }

        // ========================================================================================
        // 1. CONTROLE DE ESTADO DO MENU (ABRIR, FECHAR E CURSOR)
        // ========================================================================================
        public void ToggleMenu()
        {
            SetMenuOpen(!IsMenuOpen);
        }

        public void SetMenuOpen(bool open)
        {
            IsMenuOpen = open;

            if (menuContentRoot != null)
            {
                menuContentRoot.SetActive(open);
            }

            // Controle de Cursor: no menu o cursor fica livre e visível; no jogo fica travado no centro
            if (open)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                if (previewPedestalTransform != null) previewPedestalTransform.gameObject.SetActive(true);
                if (previewSpotlight != null) previewSpotlight.enabled = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                if (previewPedestalTransform != null) previewPedestalTransform.gameObject.SetActive(false);
                if (previewSpotlight != null) previewSpotlight.enabled = false;
            }
        }

        private void EnsureEventSystemExists()
        {
            EventSystem[] existing = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            if (existing != null && existing.Length > 1)
            {
                for (int i = 1; i < existing.Length; i++)
                {
                    if (existing[i] != null)
                    {
                        existing[i].enabled = false;
                        Destroy(existing[i].gameObject);
                    }
                }
            }
            else if (existing == null || existing.Length == 0)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<StandaloneInputModule>();
            }
        }

        // ========================================================================================
        // 2. NAVEGAÇÃO DE CATEGORIAS E FILTRAGEM NA GRADE
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
            if (btnCloseMenu != null) btnCloseMenu.onClick.AddListener(() => SetMenuOpen(false));
        }

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
            cb.normalColor = isSelected ? new Color(0.95f, 0.72f, 0.15f, 1f) : new Color(0.10f, 0.13f, 0.18f, 0.90f);
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

            if (categoryWeapons.Count == 0)
            {
                CreateEmptyCategoryNotice();
                return;
            }

            foreach (WeaponData weapon in categoryWeapons)
            {
                CreateWeaponCard(weapon);
            }
        }

        // ========================================================================================
        // 3. CONSTRUÇÃO TÁTICA E ELEGANTE DO CARD DE ARMA (SEM DISTORÇÃO)
        // ========================================================================================
        private void CreateWeaponCard(WeaponData weapon)
        {
            GameObject cardObj = new GameObject($"Card_{weapon.WeaponName}", typeof(RectTransform));
            cardObj.transform.SetParent(weaponGridContainer, false);

            RectTransform rt = cardObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(250f, 260f);

            bool isEquipped = (_selectedWeaponData == weapon);

            // Fundo escuro com borda de destaque
            Image bg = cardObj.AddComponent<Image>();
            bg.color = isEquipped ? new Color(0.12f, 0.22f, 0.32f, 0.95f) : new Color(0.08f, 0.10f, 0.14f, 0.90f);

            Button cardBtn = cardObj.AddComponent<Button>();
            ColorBlock cb = cardBtn.colors;
            cb.highlightedColor = new Color(0.18f, 0.28f, 0.40f, 1f);
            cardBtn.colors = cb;
            cardBtn.onClick.AddListener(() => EquipWeapon(weapon));

            // Borda visual ao redor do card
            CreateOutlineBorder(cardObj.transform, isEquipped ? new Color(0f, 0.85f, 1f, 0.9f) : new Color(0.2f, 0.25f, 0.32f, 0.5f));

            // 1. Selo Superior de Categoria / Calibre
            GameObject tagObj = new GameObject("CategoryTag", typeof(RectTransform));
            tagObj.transform.SetParent(cardObj.transform, false);
            RectTransform tagRt = tagObj.GetComponent<RectTransform>();
            tagRt.anchorMin = new Vector2(0.05f, 0.87f);
            tagRt.anchorMax = new Vector2(0.95f, 0.97f);
            tagRt.offsetMin = Vector2.zero;
            tagRt.offsetMax = Vector2.zero;
            TextMeshProUGUI tagTmp = tagObj.AddComponent<TextMeshProUGUI>();
            tagTmp.text = weapon.Category.ToString().ToUpper();
            tagTmp.fontSize = 10;
            tagTmp.fontStyle = FontStyles.Bold;
            tagTmp.alignment = TextAlignmentOptions.MidlineLeft;
            tagTmp.color = new Color(0.6f, 0.7f, 0.85f);
            tagTmp.raycastTarget = false;

            // Selo de Equipado no canto superior direito
            if (isEquipped)
            {
                GameObject badgeObj = new GameObject("EquippedBadge", typeof(RectTransform));
                badgeObj.transform.SetParent(cardObj.transform, false);
                RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
                badgeRt.anchorMin = new Vector2(0.50f, 0.87f);
                badgeRt.anchorMax = new Vector2(0.95f, 0.97f);
                badgeRt.offsetMin = Vector2.zero;
                badgeRt.offsetMax = Vector2.zero;
                TextMeshProUGUI badgeTmp = badgeObj.AddComponent<TextMeshProUGUI>();
                badgeTmp.text = "[ EQUIPADA ]";
                badgeTmp.fontSize = 11;
                badgeTmp.fontStyle = FontStyles.Bold;
                badgeTmp.alignment = TextAlignmentOptions.MidlineRight;
                badgeTmp.color = new Color(0f, 1f, 0.65f);
                badgeTmp.raycastTarget = false;
            }

            // 2. Ícone / Silhueta Tática 2D Centralizada
            GameObject iconObj = new GameObject("WeaponSilhouette", typeof(RectTransform));
            iconObj.transform.SetParent(cardObj.transform, false);
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.08f, 0.44f);
            iconRt.anchorMax = new Vector2(0.92f, 0.85f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = (weapon.WeaponIcon != null) ? weapon.WeaponIcon : TryLoadWeaponSprite(weapon.WeaponName, weapon.Category);
            iconImg.color = isEquipped ? Color.white : new Color(0.85f, 0.88f, 0.92f, 0.95f);
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            // 3. Nome da Arma em Destaque
            GameObject nameObj = new GameObject("WeaponName", typeof(RectTransform));
            nameObj.transform.SetParent(cardObj.transform, false);
            RectTransform nameRt = nameObj.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0.05f, 0.32f);
            nameRt.anchorMax = new Vector2(0.95f, 0.43f);
            nameRt.offsetMin = Vector2.zero;
            nameRt.offsetMax = Vector2.zero;
            TextMeshProUGUI nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
            nameTmp.text = weapon.WeaponName.ToUpper();
            nameTmp.fontSize = 14;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.color = isEquipped ? new Color(0.98f, 0.85f, 0.35f) : Color.white;
            nameTmp.raycastTarget = false;

            // 4. Barras de Atributos Finitas e Alinhadas (Dano, Precisão e Cadência)
            CreateStatBar(cardObj.transform, "Stat_Dano", "DANO", weapon.DamageStat / 100f, 0.22f, 0.30f, new Color(1f, 0.30f, 0.30f));
            CreateStatBar(cardObj.transform, "Stat_Precisao", "PRECISÃO", weapon.AccuracyStat / 100f, 0.12f, 0.20f, new Color(0.20f, 0.85f, 1f));
            CreateStatBar(cardObj.transform, "Stat_Cadencia", "CADÊNCIA", weapon.FireRateStat / 100f, 0.02f, 0.10f, new Color(1f, 0.80f, 0.25f));
        }

        private void CreateOutlineBorder(Transform parent, Color borderColor)
        {
            GameObject borderObj = new GameObject("BorderOutline", typeof(RectTransform));
            borderObj.transform.SetParent(parent, false);
            RectTransform rt = borderObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Outline outline = borderObj.AddComponent<Outline>();
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(1.5f, 1.5f);

            Image img = borderObj.AddComponent<Image>();
            img.color = Color.clear;
            img.raycastTarget = false;
        }

        private void CreateStatBar(Transform parent, string name, string label, float fillAmount, float yMin, float yMax, Color barColor)
        {
            GameObject barObj = new GameObject(name, typeof(RectTransform));
            barObj.transform.SetParent(parent, false);
            RectTransform barRt = barObj.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.06f, yMin);
            barRt.anchorMax = new Vector2(0.94f, yMax);
            barRt.offsetMin = Vector2.zero;
            barRt.offsetMax = Vector2.zero;

            // Rótulo à esquerda (ex: DANO)
            GameObject lblObj = new GameObject("Label", typeof(RectTransform));
            lblObj.transform.SetParent(barObj.transform, false);
            RectTransform lblRt = lblObj.GetComponent<RectTransform>();
            lblRt.anchorMin = new Vector2(0f, 0f);
            lblRt.anchorMax = new Vector2(0.38f, 1f);
            lblRt.offsetMin = Vector2.zero;
            lblRt.offsetMax = Vector2.zero;
            TextMeshProUGUI lblTmp = lblObj.AddComponent<TextMeshProUGUI>();
            lblTmp.text = label;
            lblTmp.fontSize = 10;
            lblTmp.fontStyle = FontStyles.Bold;
            lblTmp.color = new Color(0.72f, 0.76f, 0.82f);
            lblTmp.alignment = TextAlignmentOptions.MidlineLeft;
            lblTmp.raycastTarget = false;

            // Fundo escuro da barra
            GameObject bgObj = new GameObject("BgTrack", typeof(RectTransform));
            bgObj.transform.SetParent(barObj.transform, false);
            RectTransform bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0.40f, 0.20f);
            bgRt.anchorMax = new Vector2(0.85f, 0.80f);
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.12f, 0.15f, 0.20f, 1f);
            bgImg.raycastTarget = false;

            // Preenchimento colorido proporcional
            GameObject fillObj = new GameObject("FillProgress", typeof(RectTransform));
            fillObj.transform.SetParent(bgObj.transform, false);
            RectTransform fillRt = fillObj.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(Mathf.Clamp01(fillAmount), 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.color = barColor;
            fillImg.raycastTarget = false;

            // Valor numérico à direita (ex: 85)
            GameObject valObj = new GameObject("Value", typeof(RectTransform));
            valObj.transform.SetParent(barObj.transform, false);
            RectTransform valRt = valObj.GetComponent<RectTransform>();
            valRt.anchorMin = new Vector2(0.86f, 0f);
            valRt.anchorMax = new Vector2(1f, 1f);
            valRt.offsetMin = Vector2.zero;
            valRt.offsetMax = Vector2.zero;
            TextMeshProUGUI valTmp = valObj.AddComponent<TextMeshProUGUI>();
            valTmp.text = Mathf.RoundToInt(fillAmount * 100f).ToString();
            valTmp.fontSize = 10;
            valTmp.color = Color.white;
            valTmp.alignment = TextAlignmentOptions.MidlineRight;
            valTmp.raycastTarget = false;
        }

        private void CreateEmptyCategoryNotice()
        {
            GameObject noticeObj = new GameObject("Notice_Empty", typeof(RectTransform));
            noticeObj.transform.SetParent(weaponGridContainer, false);
            RectTransform rt = noticeObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400f, 80f);
            TextMeshProUGUI tmp = noticeObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "<i>Nenhum modelo cadastrado nesta categoria.</i>";
            tmp.fontSize = 15;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.6f, 0.65f, 0.75f, 0.8f);
        }

        // ========================================================================================
        // 4. PREVIEW 3D EM TEMPO REAL & EQUIPAMENTO DO ARSENAL
        // ========================================================================================
        public void EquipWeapon(WeaponData weapon)
        {
            _selectedWeaponData = weapon;

            // Mapeia para SelectedWeaponType
            string wName = weapon.WeaponName.ToLower();
            if (wName.Contains("akm")) _selectedWeaponType = SelectedWeaponType.Rifle_AKM_Tactical;
            else if (wName.Contains("wesson") || wName.Contains(".357")) _selectedWeaponType = SelectedWeaponType.Revolver_DanWesson_357;
            else if (wName.Contains("strongarm") || wName.Contains("mp5")) _selectedWeaponType = SelectedWeaponType.SMG_Strongarm_Tactical;
            else if (wName.Contains("beretta") || wName.Contains("m9t")) _selectedWeaponType = SelectedWeaponType.Pistol_Beretta_M9T_Black;
            else if (wName.Contains("glock")) _selectedWeaponType = SelectedWeaponType.Pistol_Tactical_Glock;
            else _selectedWeaponType = SelectedWeaponType.Pistol_9mm;

            if (TacticalGameManager.Instance != null)
            {
                TacticalGameManager.Instance.SelectedWeapon = _selectedWeaponType;
            }

            // Se já estiver na simulação ativa, troca a arma empunhada pelo jogador em tempo real!
            if (GameObject.Find("--- IPSC SHOOTING RANGE ---") != null)
            {
                SceneGeneratorCore.SwitchEquippedWeapon(_selectedWeaponType);
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

            if (weapon.WeaponPrefab != null)
            {
                _spawned3DPreviewInstance = Instantiate(weapon.WeaponPrefab, previewPedestalTransform.position + new Vector3(0f, 0.12f, 0f), Quaternion.Euler(0f, 45f, 0f));
                _spawned3DPreviewInstance.transform.SetParent(previewPedestalTransform);
            }
            else
            {
                _spawned3DPreviewInstance = CreateShowcaseMesh(weapon);
                _spawned3DPreviewInstance.transform.position = previewPedestalTransform.position + new Vector3(0f, 0.15f, 0f);
                _spawned3DPreviewInstance.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
                _spawned3DPreviewInstance.transform.SetParent(previewPedestalTransform);
            }

            if (previewSpotlight != null)
            {
                previewSpotlight.enabled = true;
                previewSpotlight.transform.LookAt(_spawned3DPreviewInstance.transform.position);
            }
        }

        private void SetupPreviewEnvironment()
        {
            if (previewPedestalTransform != null) return;

            GameObject pedestalObj = new GameObject("3D_WeaponPreview_Pedestal");
            pedestalObj.transform.position = new Vector3(0.70f, 0.90f, 1.8f);

            GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "TableMesh";
            table.transform.SetParent(pedestalObj.transform);
            table.transform.localPosition = new Vector3(0f, -0.4f, 0f);
            table.transform.localScale = new Vector3(1.1f, 0.8f, 0.7f);

            Material tableMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            tableMat.color = new Color(0.14f, 0.16f, 0.20f);
            tableMat.SetFloat("_Smoothness", 0.75f);
            tableMat.SetFloat("_Metallic", 0.40f);
            table.GetComponent<MeshRenderer>().material = tableMat;

            previewPedestalTransform = pedestalObj.transform;

            GameObject lightObj = new GameObject("Preview_StudioSpotlight");
            lightObj.transform.SetParent(pedestalObj.transform);
            lightObj.transform.localPosition = new Vector3(0.4f, 1.6f, -0.5f);

            previewSpotlight = lightObj.AddComponent<Light>();
            previewSpotlight.type = LightType.Spot;
            previewSpotlight.color = new Color(0.96f, 0.98f, 1.0f);
            previewSpotlight.intensity = 3.2f;
            previewSpotlight.spotAngle = 55f;
            previewSpotlight.range = 5.0f;
            previewSpotlight.shadows = LightShadows.Soft;
            lightObj.transform.LookAt(pedestalObj.transform.position);
        }

        private GameObject CreateShowcaseMesh(WeaponData weapon)
        {
            GameObject root = new GameObject($"Showcase_{weapon.WeaponName}");
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(root.transform);
            body.transform.localScale = new Vector3(0.045f, 0.12f, 0.28f);

            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(0.12f, 0.14f, 0.18f);
            mat.SetFloat("_Smoothness", 0.85f);
            mat.SetFloat("_Metallic", 0.75f);
            body.GetComponent<MeshRenderer>().material = mat;

            return root;
        }

        // ========================================================================================
        // 5. SELEÇÃO DE PISTAS E TRANSIÇÃO DE JOGO
        // ========================================================================================
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
                        selectedMapDescriptionText.text = "<b>ESTANDE INDOOR:</b> Cabines acústicas individuais, alvos em trilhos de 5m a 25m com iluminação fluorescente e reflexos realistas no piso.";
                        break;
                    case GameEnvironmentType.Outdoor_Precision_Stand:
                        selectedMapDescriptionText.text = "<b>ESTANDE OUTDOOR:</b> Pista aberta sob sol forte, piso de cascalho claro, berma de contenção e alvos de silhueta com alça e massa de mira perfeitas.";
                        break;
                    case GameEnvironmentType.IPSC_Outdoor_Short:
                        selectedMapDescriptionText.text = "<b>TIRO DE PRECISÃO IPSC:</b> Pista rápida (Speed Option) com 3 alvos de papelão e 2 poppers metálicos reativos para treino de split time.";
                        break;
                    case GameEnvironmentType.Tactical_CS_Killhouse:
                    case GameEnvironmentType.Bodycam_ShootHouse:
                        selectedMapDescriptionText.text = "<b>PISTA DE COMBATE CQB:</b> Galpão tático com contêineres marítimos, caixas militares e alvos hostis em ambiente tático urbano.";
                        break;
                }
            }
        }

        public void StartSimulationGame()
        {
            // Fecha a UI do menu para liberar visão do jogo
            SetMenuOpen(false);

            if (TacticalGameManager.Instance != null)
            {
                TacticalGameManager.Instance.StartGame(_selectedEnvironment, _selectedWeaponType);
            }
            else
            {
                SceneGeneratorCore.BuildSelectedEnvironment(_selectedEnvironment, _selectedWeaponType);
            }
        }

        // ========================================================================================
        // 6. SISTEMA PROFISSIONAL DE ÍCONES E SILHUETAS TÁTICAS DE ARMAS (ESTILO CS2 / AAA)
        // ========================================================================================

        /// <summary>
        /// Assegura que todas as armas cadastradas possuam ícones/sprites válidos,
        /// carregando imagens reais do projeto ou gerando silhuetas vetoriais suaves.
        /// </summary>
        public void EnsureAllWeaponIcons()
        {
            if (registeredWeapons == null) return;
            for (int i = 0; i < registeredWeapons.Count; i++)
            {
                var w = registeredWeapons[i];
                if (w != null && w.WeaponIcon == null)
                {
                    Sprite icon = TryLoadWeaponSprite(w.WeaponName, w.Category);
                    if (icon != null)
                    {
                        SetField(w, "weaponIcon", icon);
                    }
                }
            }
        }

        /// <summary>
        /// Tenta carregar a imagem 2D real da arma nos pacotes de textura do projeto com recorte automático;
        /// se não existir no disco, gera uma silhueta vetorial de alta definição com anti-aliasing.
        /// </summary>
        public static Sprite TryLoadWeaponSprite(string weaponName, WeaponCategory category, string explicitPath = null)
        {
            if (!string.IsNullOrEmpty(explicitPath))
            {
                Sprite sp = LoadWeaponSpriteWithCrop(explicitPath);
                if (sp != null) return sp;
            }

            string lower = (weaponName ?? "").ToLower();

            // Mapeamento inteligente para os modelos reais renderizados no projeto (Low Poly Pistol Pack 3)
            string detectedPath = null;
            if (category == WeaponCategory.Pistolas)
            {
                if (lower.Contains("beretta") && lower.Contains("tan"))
                    detectedPath = "Assets/Low Poly Pistol Weapon Pack 3/UI/Weapons/Pistol_K_1K_2.png";
                else if (lower.Contains("beretta") || lower.Contains("m9"))
                    detectedPath = "Assets/Low Poly Pistol Weapon Pack 3/UI/Weapons/Pistol_K_1K_1.png";
                else if (lower.Contains("glock"))
                    detectedPath = "Assets/Low Poly Pistol Weapon Pack 3/UI/Weapons/Pistol_L_1K_1.png";
                else if (lower.Contains("1911") || lower.Contains("colt"))
                    detectedPath = "Assets/Low Poly Pistol Weapon Pack 3/UI/Weapons/Pistol_M_1K_1.png";
                else if (lower.Contains("dan wesson") || lower.Contains("357"))
                    detectedPath = "Assets/Low Poly Pistol Weapon Pack 3/UI/Weapons/Pistol_N_1K_1.png";
                else if (lower.Contains("superhawk") || lower.Contains("44"))
                    detectedPath = "Assets/Low Poly Pistol Weapon Pack 3/UI/Weapons/Pistol_O_1K_1.png";
            }

            if (!string.IsNullOrEmpty(detectedPath))
            {
                Sprite sp = LoadWeaponSpriteWithCrop(detectedPath);
                if (sp != null) return sp;
            }

            // Fallback: Silhueta vetorial anti-aliased estilizada e detalhada
            return GetProceduralWeaponSilhouette(category, weaponName);
        }

        /// <summary>
        /// Carrega um arquivo PNG de arma do disco e realiza o recorte automático (Tight Bounding Box)
        /// das margens transparentes, fazendo a arma preencher o card com máxima nitidez e tamanho.
        /// </summary>
        public static Sprite LoadWeaponSpriteWithCrop(string relativeAssetPath)
        {
            if (string.IsNullOrEmpty(relativeAssetPath)) return null;

            string key = $"SpriteFile_{relativeAssetPath}";
            if (_proceduralSpriteCache.TryGetValue(key, out Sprite cached)) return cached;

#if UNITY_EDITOR
            // Tenta obter sub-assets do AssetDatabase primeiro se for Multiple Sprite
            var allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(relativeAssetPath);
            if (allAssets != null)
            {
                foreach (var a in allAssets)
                {
                    if (a is Sprite editorSprite)
                    {
                        _proceduralSpriteCache[key] = editorSprite;
                        return editorSprite;
                    }
                }
            }
#endif

            // Fallback direto em disco (lê os bytes e recorta o enquadramento exato da arma)
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativeAssetPath);
            if (!File.Exists(fullPath)) return null;

            try
            {
                byte[] bytes = File.ReadAllBytes(fullPath);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    Color32[] pixels = tex.GetPixels32();
                    int w = tex.width;
                    int h = tex.height;
                    int minX = w, maxX = 0, minY = h, maxY = 0;
                    bool foundAny = false;

                    for (int y = 0; y < h; y++)
                    {
                        int row = y * w;
                        for (int x = 0; x < w; x++)
                        {
                            if (pixels[row + x].a > 25)
                            {
                                if (x < minX) minX = x;
                                if (x > maxX) maxX = x;
                                if (y < minY) minY = y;
                                if (y > maxY) maxY = y;
                                foundAny = true;
                            }
                        }
                    }

                    // Se encontrou a arma, recorta com pequena margem estética de respiro
                    int pad = 8;
                    int cropX = Mathf.Max(0, minX - pad);
                    int cropY = Mathf.Max(0, minY - pad);
                    int cropW = Mathf.Min(w - cropX, (maxX - minX + 1) + pad * 2);
                    int cropH = Mathf.Min(h - cropY, (maxY - minY + 1) + pad * 2);

                    Rect rect = foundAny ? new Rect(cropX, cropY, cropW, cropH) : new Rect(0, 0, w, h);
                    Sprite sp = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f);
                    sp.name = Path.GetFileNameWithoutExtension(relativeAssetPath);
                    _proceduralSpriteCache[key] = sp;
                    return sp;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LoadoutMenuManager] Falha ao carregar sprite de '{relativeAssetPath}': {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Gera uma silhueta vetorial profissional em alta resolução (512x256) com anti-aliasing,
        /// contornos anatômicos fiéis da arma e gradientes sutis de iluminação metálica.
        /// </summary>
        private static Sprite GetProceduralWeaponSilhouette(WeaponCategory category, string weaponName)
        {
            string key = $"Silhouette_{category}_{weaponName}";
            if (_proceduralSpriteCache.TryGetValue(key, out Sprite cached)) return cached;

            int width = 512;
            int height = 256;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            Color bodyMetal = new Color(0.88f, 0.91f, 0.96f, 0.95f);
            Color barrelSteel = new Color(0.68f, 0.74f, 0.82f, 0.95f);
            Color accentGold = new Color(1f, 0.82f, 0.35f, 0.95f);
            Color darkWood = new Color(0.72f, 0.44f, 0.22f, 0.95f);
            Color gripColor = new Color(0.20f, 0.22f, 0.26f, 0.95f);

            string lower = (weaponName ?? "").ToLower();

            switch (category)
            {
                case WeaponCategory.Pistolas:
                    if (lower.Contains("357") || lower.Contains("44") || lower.Contains("wesson") || lower.Contains("hawk"))
                    {
                        // Revólver Magnum (.357 / .44)
                        DrawSmoothCapsule(pixels, width, height, new Vector2(245, 142), new Vector2(430, 142), 14f, barrelSteel); // Cano longo pesado
                        DrawSmoothRoundedBox(pixels, width, height, 335, 156, 90, 6, 2f, barrelSteel); // Nervura ventilada do cano
                        DrawSmoothRoundedBox(pixels, width, height, 220, 140, 36, 26, 8f, bodyMetal); // Tambor do cilindro
                        DrawSmoothCapsule(pixels, width, height, new Vector2(175, 148), new Vector2(150, 60), 18f, darkWood); // Empunhadura de madeira
                        DrawSmoothCapsule(pixels, width, height, new Vector2(178, 160), new Vector2(165, 172), 5f, accentGold); // Cão / Martelo (Hammer)
                        DrawSmoothCapsule(pixels, width, height, new Vector2(195, 122), new Vector2(230, 105), 4f, barrelSteel); // Guarda-mato
                        DrawSmoothCapsule(pixels, width, height, new Vector2(230, 105), new Vector2(235, 130), 4f, barrelSteel);
                        DrawSmoothCapsule(pixels, width, height, new Vector2(212, 130), new Vector2(208, 115), 3f, accentGold); // Gatilho
                    }
                    else
                    {
                        // Pistola Tática Semiautomática (Beretta / Glock / 1911)
                        DrawSmoothRoundedBox(pixels, width, height, 260, 155, 130, 22, 6f, bodyMetal); // Ferrolho (Slide)
                        DrawSmoothRoundedBox(pixels, width, height, 395, 153, 16, 12, 2f, barrelSteel); // Ponta do cano
                        DrawSmoothCapsule(pixels, width, height, new Vector2(195, 150), new Vector2(160, 55), 22f, gripColor); // Grip empunhadura
                        DrawSmoothRoundedBox(pixels, width, height, 155, 48, 22, 6, 3f, barrelSteel); // Baseplate do carregador
                        DrawSmoothCapsule(pixels, width, height, new Vector2(220, 138), new Vector2(265, 115), 5f, barrelSteel); // Guarda-mato
                        DrawSmoothCapsule(pixels, width, height, new Vector2(265, 115), new Vector2(275, 144), 5f, barrelSteel);
                        DrawSmoothCapsule(pixels, width, height, new Vector2(242, 142), new Vector2(238, 124), 4f, accentGold); // Gatilho
                        DrawSmoothRoundedBox(pixels, width, height, 145, 174, 5, 6, 2f, accentGold); // Alça de mira
                        DrawSmoothRoundedBox(pixels, width, height, 375, 174, 4, 6, 2f, accentGold); // Massa de mira
                    }
                    break;

                case WeaponCategory.Submetralhadoras:
                    if (lower.Contains("p90"))
                    {
                        // P90 Bullpup anatômica
                        DrawSmoothRoundedBox(pixels, width, height, 255, 135, 140, 36, 16f, bodyMetal); // Corpo ergonômico
                        DrawSmoothCapsule(pixels, width, height, new Vector2(185, 125), new Vector2(165, 65), 18f, gripColor); // Empunhadura traseira
                        DrawSmoothCapsule(pixels, width, height, new Vector2(265, 125), new Vector2(250, 75), 16f, gripColor); // Foregrip dianteiro
                        DrawSmoothRoundedBox(pixels, width, height, 275, 168, 115, 10, 3f, accentGold); // Carregador horizontal translúcido
                        DrawSmoothCapsule(pixels, width, height, new Vector2(395, 142), new Vector2(430, 142), 8f, barrelSteel); // Boca do cano
                    }
                    else
                    {
                        // MP5 / UMP / Submetralhadora Tática
                        DrawSmoothRoundedBox(pixels, width, height, 245, 148, 115, 24, 6f, bodyMetal); // Receptor central
                        DrawSmoothCapsule(pixels, width, height, new Vector2(360, 150), new Vector2(435, 150), 10f, barrelSteel); // Cano / Supressor
                        DrawSmoothCapsule(pixels, width, height, new Vector2(295, 138), new Vector2(275, 42), 12f, accentGold); // Magazine curvo
                        DrawSmoothCapsule(pixels, width, height, new Vector2(195, 138), new Vector2(170, 60), 18f, gripColor); // Empunhadura
                        DrawSmoothCapsule(pixels, width, height, new Vector2(130, 154), new Vector2(60, 154), 5f, barrelSteel); // Hastes coronha
                        DrawSmoothCapsule(pixels, width, height, new Vector2(60, 125), new Vector2(60, 175), 8f, bodyMetal); // Soleira
                        DrawSmoothCapsule(pixels, width, height, new Vector2(420, 162), new Vector2(420, 172), 3f, accentGold); // Massa com anel
                    }
                    break;

                case WeaponCategory.Fuzis:
                    if (lower.Contains("akm") || lower.Contains("ak"))
                    {
                        // AKM 7.62x39mm (Madeira e Aço Estampado)
                        DrawSmoothRoundedBox(pixels, width, height, 245, 148, 95, 24, 4f, bodyMetal); // Culatra
                        DrawSmoothCapsule(pixels, width, height, new Vector2(150, 148), new Vector2(60, 120), 18f, darkWood); // Coronha de madeira
                        DrawSmoothCapsule(pixels, width, height, new Vector2(340, 152), new Vector2(415, 152), 14f, darkWood); // Guarda-mão de madeira
                        DrawSmoothCapsule(pixels, width, height, new Vector2(415, 152), new Vector2(465, 152), 7f, barrelSteel); // Cano
                        DrawSmoothCapsule(pixels, width, height, new Vector2(445, 155), new Vector2(445, 176), 4f, bodyMetal); // Torre da massa
                        DrawSmoothCapsule(pixels, width, height, new Vector2(285, 136), new Vector2(265, 80), 15f, accentGold); // Magazine Banana
                        DrawSmoothCapsule(pixels, width, height, new Vector2(265, 80), new Vector2(245, 36), 15f, accentGold);
                        DrawSmoothCapsule(pixels, width, height, new Vector2(195, 138), new Vector2(175, 65), 15f, darkWood); // Pistolgrip
                    }
                    else
                    {
                        // M4A1 Carabina Tática
                        DrawSmoothRoundedBox(pixels, width, height, 235, 152, 90, 22, 5f, bodyMetal); // Upper/Lower
                        DrawSmoothCapsule(pixels, width, height, new Vector2(145, 152), new Vector2(65, 145), 16f, gripColor); // Coronha telescópica
                        DrawSmoothRoundedBox(pixels, width, height, 360, 152, 55, 16, 4f, gripColor); // Guarda-mão picatinny
                        DrawSmoothCapsule(pixels, width, height, new Vector2(415, 152), new Vector2(460, 152), 7f, barrelSteel); // Cano
                        DrawSmoothCapsule(pixels, width, height, new Vector2(415, 155), new Vector2(415, 180), 4f, bodyMetal); // A-Frame sight
                        DrawSmoothCapsule(pixels, width, height, new Vector2(265, 140), new Vector2(252, 50), 14f, barrelSteel); // STANAG Mag
                        DrawSmoothCapsule(pixels, width, height, new Vector2(185, 140), new Vector2(165, 68), 16f, gripColor); // Punho A2
                    }
                    break;

                case WeaponCategory.Espingardas:
                    // Espingarda Tática 12GA Pump Action
                    DrawSmoothCapsule(pixels, width, height, new Vector2(240, 152), new Vector2(465, 152), 10f, barrelSteel); // Cano longo
                    DrawSmoothCapsule(pixels, width, height, new Vector2(250, 138), new Vector2(410, 138), 9f, barrelSteel); // Tubo de depósito
                    DrawSmoothRoundedBox(pixels, width, height, 330, 138, 45, 13, 5f, accentGold); // Telha / Pump forend
                    DrawSmoothRoundedBox(pixels, width, height, 220, 146, 50, 22, 6f, bodyMetal); // Caixa de culatra
                    DrawSmoothCapsule(pixels, width, height, new Vector2(170, 144), new Vector2(60, 118), 20f, gripColor); // Coronha robusta
                    break;

                case WeaponCategory.Snipers:
                    // Fuzil de Precisão Sniper (AWP / CheyTac M200)
                    DrawSmoothCapsule(pixels, width, height, new Vector2(210, 146), new Vector2(480, 146), 9f, barrelSteel); // Cano pesado
                    DrawSmoothRoundedBox(pixels, width, height, 270, 182, 70, 12, 3f, bodyMetal); // Tubo da luneta
                    DrawSmoothRoundedBox(pixels, width, height, 205, 182, 15, 18, 4f, barrelSteel); // Ocular bell
                    DrawSmoothRoundedBox(pixels, width, height, 335, 182, 18, 20, 5f, barrelSteel); // Lente objetiva
                    DrawSmoothCapsule(pixels, width, height, new Vector2(240, 160), new Vector2(240, 174), 4f, accentGold); // Suporte anel 1
                    DrawSmoothCapsule(pixels, width, height, new Vector2(300, 160), new Vector2(300, 174), 4f, accentGold); // Suporte anel 2
                    DrawSmoothRoundedBox(pixels, width, height, 230, 142, 60, 20, 5f, bodyMetal); // Ação/Chassi
                    DrawSmoothCapsule(pixels, width, height, new Vector2(170, 140), new Vector2(65, 125), 18f, darkWood); // Coronha sniper
                    DrawSmoothRoundedBox(pixels, width, height, 260, 105, 18, 22, 3f, barrelSteel); // Carregador destacável
                    DrawSmoothCapsule(pixels, width, height, new Vector2(415, 140), new Vector2(430, 95), 4f, barrelSteel); // Bipé retraído
                    break;

                default: // Equipamentos / Granadas
                    DrawSmoothRoundedBox(pixels, width, height, 256, 120, 36, 46, 20f, new Color(0.25f, 0.65f, 0.35f, 0.95f)); // Corpo cilíndrico
                    DrawSmoothRoundedBox(pixels, width, height, 256, 172, 14, 12, 4f, barrelSteel); // Esvoacador / Fusível
                    DrawSmoothCapsule(pixels, width, height, new Vector2(252, 180), new Vector2(220, 125), 5f, accentGold); // Alavanca (Spoon)
                    DrawSmoothCapsule(pixels, width, height, new Vector2(236, 175), new Vector2(226, 182), 4f, barrelSteel); // Argola
                    break;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = key;
            _proceduralSpriteCache[key] = sprite;
            return sprite;
        }

        private static void DrawSmoothCapsule(Color[] pixels, int width, int height, Vector2 p0, Vector2 p1, float radius, Color col)
        {
            float minX = Mathf.Clamp(Mathf.Min(p0.x, p1.x) - radius - 2, 0, width - 1);
            float maxX = Mathf.Clamp(Mathf.Max(p0.x, p1.x) + radius + 2, 0, width - 1);
            float minY = Mathf.Clamp(Mathf.Min(p0.y, p1.y) - radius - 2, 0, height - 1);
            float maxY = Mathf.Clamp(Mathf.Max(p0.y, p1.y) + radius + 2, 0, height - 1);

            Vector2 d = p1 - p0;
            float l2 = d.sqrMagnitude;

            for (int y = (int)minY; y <= (int)maxY; y++)
            {
                int row = y * width;
                for (int x = (int)minX; x <= (int)maxX; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float t = l2 > 0.0001f ? Mathf.Clamp01(Vector2.Dot(p - p0, d) / l2) : 0f;
                    Vector2 proj = p0 + t * d;
                    float dist = Vector2.Distance(p, proj);
                    float alpha = Mathf.Clamp01(radius + 0.5f - dist);
                    if (alpha > 0.01f)
                    {
                        Color prev = pixels[row + x];
                        Color blended = Color.Lerp(prev, col, alpha * col.a);
                        pixels[row + x] = blended;
                    }
                }
            }
        }

        private static void DrawSmoothRoundedBox(Color[] pixels, int width, int height, float cx, float cy, float halfW, float halfH, float r, Color col)
        {
            float minX = Mathf.Clamp(cx - halfW - 2, 0, width - 1);
            float maxX = Mathf.Clamp(cx + halfW + 2, 0, width - 1);
            float minY = Mathf.Clamp(cy - halfH - 2, 0, height - 1);
            float maxY = Mathf.Clamp(cy + halfH + 2, 0, height - 1);

            for (int y = (int)minY; y <= (int)maxY; y++)
            {
                int row = y * width;
                for (int x = (int)minX; x <= (int)maxX; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float qx = Mathf.Abs(px - cx) - (halfW - r);
                    float qy = Mathf.Abs(py - cy) - (halfH - r);
                    float dist = Mathf.Max(qx, qy);
                    if (qx > 0 && qy > 0) dist = Mathf.Sqrt(qx * qx + qy * qy);
                    float alpha = Mathf.Clamp01(r + 0.5f - dist);
                    if (alpha > 0.01f)
                    {
                        Color prev = pixels[row + x];
                        Color blended = Color.Lerp(prev, col, alpha * col.a);
                        pixels[row + x] = blended;
                    }
                }
            }
        }

        // ========================================================================================
        // 7. BANCO DE DADOS DE ARMAS (INICIALIZAÇÃO AUTOMÁTICA)
        // ========================================================================================
        private void InitializeDefaultWeaponDatabaseIfEmpty()
        {
            if (registeredWeapons.Count > 0)
            {
                EnsureAllWeaponIcons();
                if (_selectedWeaponData == null) _selectedWeaponData = registeredWeapons[0];
                return;
            }

            // Pistolas (Carregadas com imagens reais recortadas dos pacotes 3D do projeto)
            registeredWeapons.Add(CreateDefaultData("Beretta M9T (Black)", WeaponCategory.Pistolas, 52f, 85f, 55f, "Assets/Low Poly Pistol Weapon Pack 3/UI/Weapons/Pistol_K_1K_1.png"));
            registeredWeapons.Add(CreateDefaultData("Beretta M9T (Tan)", WeaponCategory.Pistolas, 52f, 85f, 55f, "Assets/Low Poly Pistol Weapon Pack 3/UI/Weapons/Pistol_K_1K_2.png"));
            registeredWeapons.Add(CreateDefaultData("Glock 17 Tactical", WeaponCategory.Pistolas, 48f, 82f, 62f, "Assets/Low Poly Pistol Weapon Pack 3/UI/Weapons/Pistol_L_1K_1.png"));
            registeredWeapons.Add(CreateDefaultData("Colt 1911 Custom", WeaponCategory.Pistolas, 65f, 75f, 45f, "Assets/Low Poly Pistol Weapon Pack 3/UI/Weapons/Pistol_M_1K_1.png"));
            registeredWeapons.Add(CreateDefaultData("Dan Wesson 715 .357", WeaponCategory.Pistolas, 78f, 90f, 30f, "Assets/Low Poly Pistol Weapon Pack 3/UI/Weapons/Pistol_N_1K_1.png"));
            registeredWeapons.Add(CreateDefaultData("Superhawk .44 Mag", WeaponCategory.Pistolas, 88f, 84f, 25f, "Assets/Low Poly Pistol Weapon Pack 3/UI/Weapons/Pistol_O_1K_1.png"));

            // Submetralhadoras
            registeredWeapons.Add(CreateDefaultData("MP5 Strongarm", WeaponCategory.Submetralhadoras, 55f, 78f, 85f));
            registeredWeapons.Add(CreateDefaultData("UMP-45 Tactical", WeaponCategory.Submetralhadoras, 68f, 72f, 70f));
            registeredWeapons.Add(CreateDefaultData("P90 Bullpup", WeaponCategory.Submetralhadoras, 45f, 84f, 95f));
            registeredWeapons.Add(CreateDefaultData("MAC-10 Compact", WeaponCategory.Submetralhadoras, 42f, 65f, 98f));

            // Fuzis
            registeredWeapons.Add(CreateDefaultData("AKM 7.62x39mm", WeaponCategory.Fuzis, 85f, 74f, 65f));
            registeredWeapons.Add(CreateDefaultData("M4A1 Tactical", WeaponCategory.Fuzis, 76f, 85f, 75f));

            // Espingardas
            registeredWeapons.Add(CreateDefaultData("Nova Pump 12GA", WeaponCategory.Espingardas, 95f, 45f, 28f));
            registeredWeapons.Add(CreateDefaultData("Remington 870 Tactical", WeaponCategory.Espingardas, 92f, 48f, 32f));

            // Snipers
            registeredWeapons.Add(CreateDefaultData("M200 CheyTac Sniper", WeaponCategory.Snipers, 100f, 98f, 15f));
            registeredWeapons.Add(CreateDefaultData("AWP Precision .338", WeaponCategory.Snipers, 98f, 95f, 20f));

            // Equipamentos
            registeredWeapons.Add(CreateDefaultData("Granada Flashbang", WeaponCategory.Equipamentos, 10f, 90f, 10f));
            registeredWeapons.Add(CreateDefaultData("Granada de Fumaça", WeaponCategory.Equipamentos, 0f, 95f, 10f));

            EnsureAllWeaponIcons();
            _selectedWeaponData = registeredWeapons[0];
        }

        private WeaponData CreateDefaultData(string name, WeaponCategory cat, float dmg, float acc, float rate, string iconPath = null)
        {
            WeaponData w = ScriptableObject.CreateInstance<WeaponData>();
            w.name = $"Data_{name.Replace(" ", "_")}";

            SetField(w, "weaponName", name);
            SetField(w, "category", cat);
            SetField(w, "damageStat", dmg);
            SetField(w, "accuracyStat", acc);
            SetField(w, "fireRateStat", rate);
            SetField(w, "adsLocalPosition", new Vector3(0f, -0.024f, 0.18f));
            SetField(w, "adsLocalRotation", new Vector3(-3.2f, 0f, 0f));
            SetField(w, "adsSpeed", 14f);
            SetField(w, "adsCameraFOV", 48f);

            Sprite icon = TryLoadWeaponSprite(name, cat, iconPath);
            if (icon != null) SetField(w, "weaponIcon", icon);

            return w;
        }

        private void SetField(object obj, string name, object value)
        {
            if (obj == null) return;
            try
            {
                var f = obj.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (f != null)
                {
                    if (value == null || f.FieldType.IsAssignableFrom(value.GetType()))
                    {
                        f.SetValue(obj, value);
                    }
                }
            }
            catch {}
        }

        // ========================================================================================
        // 8. GERADOR PROCEDURAL COMPLETO DO CANVAS DE LOADOUT
        // ========================================================================================
        public static LoadoutMenuManager CreateProceduralLoadoutCanvas()
        {
            // Remove instâncias antigas se houver
            LoadoutMenuManager[] existing = FindObjectsByType<LoadoutMenuManager>(FindObjectsSortMode.None);
            foreach (var ex in existing)
            {
                if (ex != null) DestroyImmediate(ex.gameObject);
            }

            // Garante EventSystem único
            EventSystem[] existingES = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            if (existingES == null || existingES.Length == 0)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<StandaloneInputModule>();
            }
            else if (existingES.Length > 1)
            {
                for (int i = 1; i < existingES.Length; i++)
                {
                    if (existingES[i] != null)
                    {
                        existingES[i].enabled = false;
                        DestroyImmediate(existingES[i].gameObject);
                    }
                }
            }

            // Canvas Raiz
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

            // Root de Conteúdo (permite ocultar/exibir tudo com uma única chamada)
            GameObject contentRoot = new GameObject("MenuContentRoot", typeof(RectTransform));
            contentRoot.transform.SetParent(canvasObj.transform, false);
            RectTransform crRt = contentRoot.GetComponent<RectTransform>();
            crRt.anchorMin = Vector2.zero;
            crRt.anchorMax = Vector2.one;
            crRt.offsetMin = Vector2.zero;
            crRt.offsetMax = Vector2.zero;
            mgr.menuContentRoot = contentRoot;

            // Fundo escuro sutil translúcido geral
            Image rootBg = contentRoot.AddComponent<Image>();
            rootBg.color = new Color(0.04f, 0.05f, 0.08f, 0.70f);

            // ------------------------------------------------------------------------------------
            // A) BARRA DE CABEÇALHO SUPERIOR
            // ------------------------------------------------------------------------------------
            GameObject headerObj = new GameObject("HeaderPanel", typeof(RectTransform));
            headerObj.transform.SetParent(contentRoot.transform, false);
            RectTransform hRt = headerObj.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, 0.92f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.offsetMin = Vector2.zero;
            hRt.offsetMax = Vector2.zero;

            Image hBg = headerObj.AddComponent<Image>();
            hBg.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);

            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform));
            titleObj.transform.SetParent(headerObj.transform, false);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.03f, 0f);
            tRt.anchorMax = new Vector2(0.50f, 1f);
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;
            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "<b>TACTICAL SHOOTER & ARSENAL</b> <color=#FFD700>LOADOUT</color>";
            titleTmp.fontSize = 24;
            titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
            titleTmp.color = Color.white;

            // Botão Fechar Menu [ ✕ FECHAR (B) ]
            GameObject closeBtnObj = new GameObject("Btn_CloseMenu", typeof(RectTransform));
            closeBtnObj.transform.SetParent(headerObj.transform, false);
            RectTransform cBtnRt = closeBtnObj.GetComponent<RectTransform>();
            cBtnRt.anchorMin = new Vector2(0.85f, 0.15f);
            cBtnRt.anchorMax = new Vector2(0.98f, 0.85f);
            cBtnRt.offsetMin = Vector2.zero;
            cBtnRt.offsetMax = Vector2.zero;
            Image cBtnImg = closeBtnObj.AddComponent<Image>();
            cBtnImg.color = new Color(0.85f, 0.22f, 0.22f, 0.85f);
            Button cBtn = closeBtnObj.AddComponent<Button>();
            mgr.btnCloseMenu = cBtn;

            GameObject cTxtObj = new GameObject("Text", typeof(RectTransform));
            cTxtObj.transform.SetParent(closeBtnObj.transform, false);
            RectTransform cTxtRt = cTxtObj.GetComponent<RectTransform>();
            cTxtRt.anchorMin = Vector2.zero;
            cTxtRt.anchorMax = Vector2.one;
            cTxtRt.offsetMin = Vector2.zero;
            cTxtRt.offsetMax = Vector2.zero;
            TextMeshProUGUI cTxt = cTxtObj.AddComponent<TextMeshProUGUI>();
            cTxt.text = "[X] FECHAR (B)";
            cTxt.fontSize = 13;
            cTxt.fontStyle = FontStyles.Bold;
            cTxt.alignment = TextAlignmentOptions.Center;
            cTxt.color = Color.white;

            // ------------------------------------------------------------------------------------
            // B) ABAS DE CATEGORIAS DE ARMAS (TOPO)
            // ------------------------------------------------------------------------------------
            GameObject tabsBar = new GameObject("CategoryTabsBar", typeof(RectTransform));
            tabsBar.transform.SetParent(contentRoot.transform, false);
            RectTransform tbRt = tabsBar.GetComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0.03f, 0.84f);
            tbRt.anchorMax = new Vector2(0.68f, 0.90f);
            tbRt.offsetMin = Vector2.zero;
            tbRt.offsetMax = Vector2.zero;

            HorizontalLayoutGroup hlg = tabsBar.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
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
            // C) QUADRO DE GRADE DE ARMAS COM SCROLL SUAVE (GRID LAYOUT GROUP)
            // ------------------------------------------------------------------------------------
            GameObject gridPanel = new GameObject("WeaponGridPanel", typeof(RectTransform));
            gridPanel.transform.SetParent(contentRoot.transform, false);
            RectTransform gpRt = gridPanel.GetComponent<RectTransform>();
            gpRt.anchorMin = new Vector2(0.03f, 0.08f);
            gpRt.anchorMax = new Vector2(0.68f, 0.82f);
            gpRt.offsetMin = Vector2.zero;
            gpRt.offsetMax = Vector2.zero;

            Image gpBg = gridPanel.AddComponent<Image>();
            gpBg.color = new Color(0.05f, 0.07f, 0.10f, 0.80f);

            // Adiciona Máscara para Rolagem (ScrollRect)
            ScrollRect scroll = gridPanel.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 25f;
            mgr.weaponScrollRect = scroll;

            GameObject viewPort = new GameObject("Viewport", typeof(RectTransform));
            viewPort.transform.SetParent(gridPanel.transform, false);
            RectTransform vpRt = viewPort.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = new Vector2(10f, 10f);
            vpRt.offsetMax = new Vector2(-10f, -10f);
            viewPort.AddComponent<RectMask2D>();
            scroll.viewport = vpRt;

            GameObject gridContainer = new GameObject("GridContainer", typeof(RectTransform));
            gridContainer.transform.SetParent(viewPort.transform, false);
            RectTransform gcRt = gridContainer.GetComponent<RectTransform>();
            gcRt.anchorMin = new Vector2(0f, 1f);
            gcRt.anchorMax = new Vector2(1f, 1f);
            gcRt.pivot = new Vector2(0.5f, 1f);
            gcRt.offsetMin = Vector2.zero;
            gcRt.offsetMax = Vector2.zero;

            GridLayoutGroup glg = gridContainer.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(245f, 260f);
            glg.spacing = new Vector2(12f, 12f);
            glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
            glg.startAxis = GridLayoutGroup.Axis.Horizontal;
            glg.childAlignment = TextAnchor.UpperLeft;

            ContentSizeFitter csf = gridContainer.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = gcRt;
            mgr.weaponGridContainer = gridContainer.transform;
            mgr.gridLayoutGroup = glg;
            mgr.loadoutPanel = gridPanel;

            // ------------------------------------------------------------------------------------
            // D) PAINEL LATERAL DIREITO: SELEÇÃO DE PISTAS & INICIAR
            // ------------------------------------------------------------------------------------
            GameObject rightPanel = new GameObject("MapSelectionPanel", typeof(RectTransform));
            rightPanel.transform.SetParent(contentRoot.transform, false);
            RectTransform rpRt = rightPanel.GetComponent<RectTransform>();
            rpRt.anchorMin = new Vector2(0.70f, 0.08f);
            rpRt.anchorMax = new Vector2(0.97f, 0.90f);
            rpRt.offsetMin = Vector2.zero;
            rpRt.offsetMax = Vector2.zero;

            Image rpBg = rightPanel.AddComponent<Image>();
            rpBg.color = new Color(0.06f, 0.08f, 0.12f, 0.90f);
            mgr.mapSelectionPanel = rightPanel;

            // Título do Painel de Pistas
            GameObject rpTitle = new GameObject("MapTitle", typeof(RectTransform));
            rpTitle.transform.SetParent(rightPanel.transform, false);
            RectTransform rptRt = rpTitle.GetComponent<RectTransform>();
            rptRt.anchorMin = new Vector2(0.05f, 0.91f);
            rptRt.anchorMax = new Vector2(0.95f, 0.98f);
            rptRt.offsetMin = Vector2.zero;
            rptRt.offsetMax = Vector2.zero;
            TextMeshProUGUI rptTmp = rpTitle.AddComponent<TextMeshProUGUI>();
            rptTmp.text = "SELEÇÃO DE CENÁRIO";
            rptTmp.fontSize = 17;
            rptTmp.fontStyle = FontStyles.Bold;
            rptTmp.alignment = TextAlignmentOptions.Center;
            rptTmp.color = new Color(0.98f, 0.85f, 0.35f);

            // Botões de Cenários
            mgr.btnOutdoorStand = CreateMapButton(rightPanel.transform, "ESTANDE OUTDOOR (Foco Visual)", new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.88f));
            mgr.btnIndoorStand = CreateMapButton(rightPanel.transform, "ESTANDE INDOOR DE PRECISÃO", new Vector2(0.05f, 0.66f), new Vector2(0.95f, 0.76f));
            mgr.btnPrecisionIPSC = CreateMapButton(rightPanel.transform, "PISTA CURTA IPSC (Speed Option)", new Vector2(0.05f, 0.54f), new Vector2(0.95f, 0.64f));
            mgr.btnCombatCQB = CreateMapButton(rightPanel.transform, "PISTA DE COMBATE CQB (Bodycam)", new Vector2(0.05f, 0.42f), new Vector2(0.95f, 0.52f));

            // Descrição do Mapa
            GameObject descObj = new GameObject("MapDescriptionText", typeof(RectTransform));
            descObj.transform.SetParent(rightPanel.transform, false);
            RectTransform descRt = descObj.GetComponent<RectTransform>();
            descRt.anchorMin = new Vector2(0.06f, 0.16f);
            descRt.anchorMax = new Vector2(0.94f, 0.39f);
            descRt.offsetMin = Vector2.zero;
            descRt.offsetMax = Vector2.zero;
            TextMeshProUGUI descTmp = descObj.AddComponent<TextMeshProUGUI>();
            descTmp.fontSize = 13;
            descTmp.alignment = TextAlignmentOptions.TopLeft;
            descTmp.color = new Color(0.85f, 0.88f, 0.92f);
            mgr.selectedMapDescriptionText = descTmp;

            // Botão Principal de Iniciar Simulação
            GameObject startBtnObj = new GameObject("Btn_StartSimulation", typeof(RectTransform));
            startBtnObj.transform.SetParent(rightPanel.transform, false);
            RectTransform sBtnRt = startBtnObj.GetComponent<RectTransform>();
            sBtnRt.anchorMin = new Vector2(0.05f, 0.03f);
            sBtnRt.anchorMax = new Vector2(0.95f, 0.13f);
            sBtnRt.offsetMin = Vector2.zero;
            sBtnRt.offsetMax = Vector2.zero;

            Image sBtnImg = startBtnObj.AddComponent<Image>();
            sBtnImg.color = new Color(0.12f, 0.68f, 0.35f, 1f);

            Button startBtn = startBtnObj.AddComponent<Button>();
            ColorBlock scb = startBtn.colors;
            scb.highlightedColor = new Color(0.16f, 0.82f, 0.42f, 1f);
            startBtn.colors = scb;
            mgr.btnStartSimulation = startBtn;

            GameObject sBtnTxt = new GameObject("Text", typeof(RectTransform));
            sBtnTxt.transform.SetParent(startBtnObj.transform, false);
            RectTransform sTxtRt = sBtnTxt.GetComponent<RectTransform>();
            sTxtRt.anchorMin = Vector2.zero;
            sTxtRt.anchorMax = Vector2.one;
            sTxtRt.offsetMin = Vector2.zero;
            sTxtRt.offsetMax = Vector2.zero;
            TextMeshProUGUI sTmp = sBtnTxt.AddComponent<TextMeshProUGUI>();
            sTmp.text = "INICIAR SIMULAÇÃO";
            sTmp.fontSize = 17;
            sTmp.fontStyle = FontStyles.Bold;
            sTmp.alignment = TextAlignmentOptions.Center;
            sTmp.color = Color.white;

            // ------------------------------------------------------------------------------------
            // E) RODAPÉ COM DICAS DE ATALHOS
            // ------------------------------------------------------------------------------------
            GameObject footerObj = new GameObject("FooterHint", typeof(RectTransform));
            footerObj.transform.SetParent(contentRoot.transform, false);
            RectTransform fRt = footerObj.GetComponent<RectTransform>();
            fRt.anchorMin = new Vector2(0.03f, 0.01f);
            fRt.anchorMax = new Vector2(0.68f, 0.06f);
            fRt.offsetMin = Vector2.zero;
            fRt.offsetMax = Vector2.zero;
            TextMeshProUGUI fTmp = footerObj.AddComponent<TextMeshProUGUI>();
            fTmp.text = "<color=#FFD700>[ B ]</color> Abrir/Fechar Arsenal a qualquer momento  |  <color=#FFD700>[ ESC ]</color> Pausar  |  <color=#FFD700>[ BOTÃO DIREITO ]</color> Visada Precisa (ADS)";
            fTmp.fontSize = 12;
            fTmp.alignment = TextAlignmentOptions.MidlineLeft;
            fTmp.color = new Color(0.65f, 0.70f, 0.78f);

            return mgr;
        }

        private static Button CreateTabButton(Transform parent, string title)
        {
            GameObject btnObj = new GameObject($"BtnTab_{title}", typeof(RectTransform));
            btnObj.transform.SetParent(parent, false);

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.10f, 0.13f, 0.18f, 0.90f);

            Button btn = btnObj.AddComponent<Button>();

            GameObject txtObj = new GameObject("Text", typeof(RectTransform));
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform rt = txtObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = title;
            tmp.fontSize = 13;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            return btn;
        }

        private static Button CreateMapButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject btnObj = new GameObject($"BtnMap_{label}", typeof(RectTransform));
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.11f, 0.15f, 0.20f, 0.95f);

            Button btn = btnObj.AddComponent<Button>();

            GameObject txtObj = new GameObject("Text", typeof(RectTransform));
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(10f, 0f);
            txtRt.offsetMax = new Vector2(-10f, 0f);
            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 12;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            return btn;
        }
    }
}
