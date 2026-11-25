#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

namespace IPSCSimulator.EditorTools
{
    using IPSCSimulator.Match;
    using IPSCSimulator.Setup;

    /// <summary>
    /// Menus no Unity Editor para montar qualquer um dos cenários 3D instantaneamente
    /// e abrir o Menu Principal do Simulador.
    /// </summary>
    public static class IPSCSceneBuilder
    {
        [MenuItem("IPSC Simulator/Montar Menu Principal & Simulador", false, 1)]
        public static void BuildMainMenu()
        {
            SceneGeneratorCore.BuildSelectedEnvironment(GameEnvironmentType.IPSC_Outdoor_Short, SelectedWeaponType.Pistol_9mm);
            if (TacticalGameManager.Instance != null) TacticalGameManager.Instance.ReturnToMainMenu();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Menu Principal Pronto", "Menu Principal carregado! Dê PLAY para escolher entre Pistas IPSC, Estande Indoor ou Armazém estilo CS!", "OK");
        }

        [MenuItem("IPSC Simulator/Cenários Diretos/1. Pista Curta IPSC (Speed Option)", false, 20)]
        public static void BuildIPSCShort()
        {
            SceneGeneratorCore.BuildSelectedEnvironment(GameEnvironmentType.IPSC_Outdoor_Short, SelectedWeaponType.Pistol_9mm);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        [MenuItem("IPSC Simulator/Cenários Diretos/2. Pista Média IPSC (Labirinto Tático)", false, 21)]
        public static void BuildIPSCMedium()
        {
            SceneGeneratorCore.BuildSelectedEnvironment(GameEnvironmentType.IPSC_Outdoor_Medium, SelectedWeaponType.Pistol_9mm);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        [MenuItem("IPSC Simulator/Cenários Diretos/3. Pista Longa IPSC (Torneio 30m)", false, 22)]
        public static void BuildIPSCLong()
        {
            SceneGeneratorCore.BuildSelectedEnvironment(GameEnvironmentType.IPSC_Outdoor_Long, SelectedWeaponType.Pistol_9mm);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        [MenuItem("IPSC Simulator/Cenários Diretos/4. Estande Indoor de Precisão (Cabines 10m-25m)", false, 23)]
        public static void BuildIndoor()
        {
            SceneGeneratorCore.BuildSelectedEnvironment(GameEnvironmentType.Indoor_Precision_Stand, SelectedWeaponType.Pistol_9mm);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        [MenuItem("IPSC Simulator/Cenários Diretos/5. Armazém Tático CQB (Estilo Counter-Strike)", false, 24)]
        public static void BuildCSKillhouse()
        {
            SceneGeneratorCore.BuildSelectedEnvironment(GameEnvironmentType.Tactical_CS_Killhouse, SelectedWeaponType.SMG_Tactical);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        [MenuItem("IPSC Simulator/Cenários Diretos/6. Killhouse Policial em Compensado (Estilo BODYCAM)", false, 25)]
        public static void BuildBodycamShootHouse()
        {
            SceneGeneratorCore.BuildSelectedEnvironment(GameEnvironmentType.Bodycam_ShootHouse, SelectedWeaponType.Pistol_9mm);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }
}
#endif
