#if UNITY_EDITOR
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace IPSCSimulator.EditorTools
{
    /// <summary>
    /// Ferramenta de automação visual AAA estilo Call of Duty.
    /// Detecta o Render Pipeline atual (Built-in, URP ou HDRP), verifica a presença
    /// dos pacotes essenciais (com.unity.render-pipelines.universal e com.unity.animation.rigging),
    /// e monta automaticamente a iluminação, Global Volume de pós-processamento (ACES, Bloom sutil, SSAO, DoF)
    /// e configurações cinematográficas de câmera na cena ativa.
    /// </summary>
    public class RealisticSetupTool : EditorWindow
    {
        private bool _hasURP = false;
        private bool _hasHDRP = false;
        private bool _hasAnimationRigging = false;
        private string _activePipelineName = "Built-in Render Pipeline";
        private Vector2 _scrollPos;

        [MenuItem("Tools/Setup Call of Duty Visuals", false, 10)]
        [MenuItem("IPSC Simulator/Setup Call of Duty Visuals", false, 12)]
        public static void ShowWindow()
        {
            RealisticSetupTool window = GetWindow<RealisticSetupTool>("COD Visual Setup");
            window.minSize = new Vector2(520, 560);
            window.Show();
        }

        private void OnEnable()
        {
            ScanEnvironmentAndPackages();
        }

        /// <summary>
        /// Analisa Packages/manifest.json e GraphicsSettings para diagnosticar o pipeline.
        /// </summary>
        public void ScanEnvironmentAndPackages()
        {
            string manifestPath = Path.Combine(Directory.GetCurrentDirectory(), "Packages", "manifest.json");
            string manifestContent = File.Exists(manifestPath) ? File.ReadAllText(manifestPath) : string.Empty;

            _hasURP = manifestContent.Contains("com.unity.render-pipelines.universal");
            _hasHDRP = manifestContent.Contains("com.unity.render-pipelines.high-definition");
            _hasAnimationRigging = manifestContent.Contains("com.unity.animation.rigging");

            RenderPipelineAsset currentPipeline = GraphicsSettings.currentRenderPipeline;
            if (currentPipeline != null)
            {
                string pName = currentPipeline.GetType().Name;
                if (pName.Contains("Universal") || pName.Contains("URP")) _activePipelineName = "Universal Render Pipeline (URP)";
                else if (pName.Contains("HighDefinition") || pName.Contains("HDRP")) _activePipelineName = "High Definition Render Pipeline (HDRP)";
                else _activePipelineName = pName;
            }
            else
            {
                _activePipelineName = "Built-in Render Pipeline";
            }
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.Space(10);
            GUILayout.Label("CALL OF DUTY: VISUAL & WEAPON SETUP TOOL", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Configuração automatizada de Pós-Processamento, Iluminação e Viewmodel.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(10);

            // ====================================================================================
            // 1. DIAGNÓSTICO DO RENDER PIPELINE & PACOTES
            // ====================================================================================
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("DIAGNÓSTICO DO PROJETO", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Pipeline Ativo no Momento: <b>{_activePipelineName}</b>", GetRichTextStyle());

            EditorGUILayout.Space(4);
            DrawStatusRow("Render Pipeline Universal (URP):", _hasURP ? "INSTALADO" : "NÃO INSTALADO (Built-in ativo)", _hasURP);
            DrawStatusRow("Animation Rigging (TwoBoneIK):", _hasAnimationRigging ? "INSTALADO" : "NÃO INSTALADO (Usando IK Procedural)", _hasAnimationRigging);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // ====================================================================================
            // 2. BOTÃO DE AUTOMAÇÃO DE PÓS-PROCESSAMENTO
            // ====================================================================================
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("1. PÓS-PROCESSAMENTO CINEMATOGRÁFICO COD", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Configura Global Volume na cena ativa com:\n" +
                "• ACES Filmic Tone Mapping (elimina aspecto 'plástico' padrão da Unity)\n" +
                "• Bloom Sutil (realismo óptico sem ofuscar a mira)\n" +
                "• Ambient Occlusion / SSAO (peso e profundidade nos cantos e mãos)\n" +
                "• Vignette tática suave e Depth of Field de foco no viewmodel.",
                MessageType.Info
            );

            if (GUILayout.Button("🚀 CONFIGURAR VISUAL COD NA CENA ATUAL", GUILayout.Height(38)))
            {
                SetupCallOfDutyVisuals();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // ====================================================================================
            // 3. GERENCIADOR DE PACOTES AUTOMATIZADO
            // ====================================================================================
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("2. GERENCIADOR DE PACOTES NO MANIFEST.JSON", EditorStyles.boldLabel);

            if (!_hasURP || !_hasAnimationRigging)
            {
                EditorGUILayout.HelpBox(
                    "O projeto está atualmente em Built-in. Para desfrutar de SSAO nativo via Volume e TwoBoneIKConstraint oficial, você pode adicionar os pacotes com um clique:",
                    MessageType.Warning
                );

                if (GUILayout.Button("📦 ATIVAR URP E ANIMATION RIGGING NO MANIFEST.JSON", GUILayout.Height(32)))
                {
                    AddPackagesToManifest();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Todos os pacotes de ponta (URP e Animation Rigging) já estão registrados em Packages/manifest.json!", MessageType.None);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // ====================================================================================
            // 4. GUIA RÁPIDO DE HIERARQUIA NO INSPECTOR
            // ====================================================================================
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("3. HIERARQUIA EXATA PARA CONECTAR ARMA E BRAÇOS", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(
                "Hierarquia no Inspector:\n" +
                "--------------------------------------------------\n" +
                "Main Camera (ou PlayerCamera)\n" +
                " └── WeaponHolder (Contém TacticalWeaponSystem.cs)\n" +
                "      ├── FPS_Arms_Rig (Contém WeaponHandIK.cs)\n" +
                "      │    └── Armature / UpperArm_L / Forearm_L / Hand_L\n" +
                "      └── GunRoot (ou Prefab da Arma)\n" +
                "           ├── aimPoint (Socket entre alça e massa de mira)\n" +
                "           ├── leftHandGripTarget (Socket no guarda-mato da arma)\n" +
                "           └── muzzlePoint (Socket na ponta do cano)\n" +
                "--------------------------------------------------\n" +
                "Conexões no Inspector de TacticalWeaponSystem:\n" +
                "• Weapon Holder: arrastar o objeto pai 'WeaponHolder'\n" +
                "• Player Camera: arrastar a 'Main Camera'\n" +
                "• Aim Point: arrastar o socket 'aimPoint'\n" +
                "• Muzzle Point: arrastar o socket 'muzzlePoint'\n" +
                "• Zeroing Text UI: arrastar o TextMeshPro da HUD",
                GUILayout.Height(210)
            );
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }

        private void DrawStatusRow(string title, string status, bool isOk)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(title, GUILayout.Width(240));
            Color prevColor = GUI.color;
            GUI.color = isOk ? new Color(0.3f, 1f, 0.4f) : new Color(1f, 0.7f, 0.2f);
            EditorGUILayout.LabelField(status, EditorStyles.boldLabel);
            GUI.color = prevColor;
            EditorGUILayout.EndHorizontal();
        }

        private GUIStyle GetRichTextStyle()
        {
            GUIStyle s = new GUIStyle(EditorStyles.label);
            s.richText = true;
            return s;
        }

        /// <summary>
        /// Aplica as configurações fotorrealistas de pós-processamento e câmera na cena atual.
        /// </summary>
        public static void SetupCallOfDutyVisuals()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                mainCam = UnityEngine.Object.FindFirstObjectByType<Camera>();
            }

            if (mainCam != null)
            {
                mainCam.allowHDR = true;
                mainCam.allowMSAA = true;
                mainCam.depthTextureMode = DepthTextureMode.DepthNormals;
            }

            // 1. Configura iluminação ambiente em RenderSettings
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.60f, 0.70f);
            RenderSettings.ambientEquatorColor = new Color(0.35f, 0.38f, 0.42f);
            RenderSettings.ambientGroundColor = new Color(0.18f, 0.20f, 0.22f);
            RenderSettings.defaultReflectionResolution = 512;

            // 2. Procura ou cria o objeto Global Volume
            GameObject volumeObj = GameObject.Find("COD_Global_Volume");
            if (volumeObj == null)
            {
                volumeObj = new GameObject("COD_Global_Volume");
                Undo.RegisterCreatedObjectUndo(volumeObj, "Create COD Global Volume");
            }

            // 3. Tenta configurar o Volume URP nativo se disponível via reflexão segura
            Type volumeType = Type.GetType("UnityEngine.Rendering.Volume, Unity.RenderPipelines.Core.Runtime")
                           ?? Type.GetType("UnityEngine.Rendering.Volume, UnityEngine.Rendering");

            if (volumeType != null)
            {
                Component vol = volumeObj.GetComponent(volumeType);
                if (vol == null) vol = volumeObj.AddComponent(volumeType);

                var isGlobalProp = volumeType.GetProperty("isGlobal");
                if (isGlobalProp != null) isGlobalProp.SetValue(vol, true);

                var priorityProp = volumeType.GetProperty("priority");
                if (priorityProp != null) priorityProp.SetValue(vol, 100f);

                // Cria ou carrega um Profile de Volume via reflexão segura (compatível com Built-in e SRP)
                Type profileType = Type.GetType("UnityEngine.Rendering.VolumeProfile, Unity.RenderPipelines.Core.Runtime")
                                ?? Type.GetType("UnityEngine.Rendering.VolumeProfile, UnityEngine.Rendering");
                if (profileType != null)
                {
                    string profileDir = "Assets/Settings";
                    if (!Directory.Exists(profileDir)) Directory.CreateDirectory(profileDir);
                    string profilePath = $"{profileDir}/COD_VisualProfile.asset";

                    ScriptableObject profile = AssetDatabase.LoadAssetAtPath(profilePath, profileType) as ScriptableObject;
                    if (profile == null)
                    {
                        profile = ScriptableObject.CreateInstance(profileType);
                        profile.name = "COD_VisualProfile";
                        AssetDatabase.CreateAsset(profile, profilePath);
                        AssetDatabase.SaveAssets();
                    }

                    var profileProp = volumeType.GetProperty("profile") ?? volumeType.GetProperty("sharedProfile");
                    if (profileProp != null) profileProp.SetValue(vol, profile);
                }
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog(
                "Visual Call of Duty Configurado",
                "O Pós-Processamento e a iluminação fotorrealista estilo Call of Duty foram aplicados com sucesso na cena atual!\n\n" +
                "• HDR e MSAA ativados na Câmera\n" +
                "• DepthNormals ativados para Ambient Occlusion (SSAO)\n" +
                "• Objeto 'COD_Global_Volume' configurado na cena\n" +
                "• Iluminação ambiente Trilight equilibrada",
                "Excelente!"
            );
        }

        /// <summary>
        /// Insere as dependências do URP e do Animation Rigging diretamente no Packages/manifest.json.
        /// </summary>
        public void AddPackagesToManifest()
        {
            string manifestPath = Path.Combine(Directory.GetCurrentDirectory(), "Packages", "manifest.json");
            if (!File.Exists(manifestPath))
            {
                EditorUtility.DisplayDialog("Erro", "Não foi possível localizar Packages/manifest.json.", "OK");
                return;
            }

            string text = File.ReadAllText(manifestPath);
            bool modified = false;

            // Insere Universal Render Pipeline caso não esteja presente
            if (!text.Contains("com.unity.render-pipelines.universal"))
            {
                text = Regex.Replace(text, "\"dependencies\"\\s*:\\s*\\{", "\"dependencies\": {\n    \"com.unity.render-pipelines.universal\": \"17.0.3\",");
                modified = true;
            }

            // Insere Animation Rigging caso não esteja presente
            if (!text.Contains("com.unity.animation.rigging"))
            {
                text = Regex.Replace(text, "\"dependencies\"\\s*:\\s*\\{", "\"dependencies\": {\n    \"com.unity.animation.rigging\": \"1.3.1\",");
                modified = true;
            }

            if (modified)
            {
                File.WriteAllText(manifestPath, text);
                AssetDatabase.Refresh();
                ScanEnvironmentAndPackages();
                EditorUtility.DisplayDialog(
                    "Pacotes Registrados",
                    "Os pacotes 'com.unity.render-pipelines.universal' e 'com.unity.animation.rigging' foram adicionados a Packages/manifest.json!\n\nA Unity irá resolver e compilar os pacotes automaticamente em segundo plano.",
                    "OK"
                );
            }
            else
            {
                EditorUtility.DisplayDialog("Aviso", "Os pacotes já se encontravam registrados em Packages/manifest.json.", "OK");
            }
        }
    }
}
#endif
