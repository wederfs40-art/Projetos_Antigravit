#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IPSCSimulator.EditorTools
{
    using IPSCSimulator.Setup;

    /// <summary>
    /// Adiciona um menu no Unity Editor para montar o estande de tiro completo
    /// com 1 único clique no menu superior.
    /// </summary>
    public static class IPSCSceneBuilder
    {
        [MenuItem("IPSC Simulator/Montar Pista Completa (Auto-Setup)", false, 1)]
        public static void BuildSceneFromMenu()
        {
            if (EditorUtility.DisplayDialog(
                "Montador de Pista IPSC",
                "Deseja gerar o estande de tiro completo (Paredes, Alvos IPSC, Pistola 3D com miras, HUD de Timer e Jogador)?",
                "Sim, Montar Pista!",
                "Cancelar"))
            {
                SceneGeneratorCore.GenerateCompleteShootingRange();
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorUtility.DisplayDialog("Sucesso!", "Estande de tiro IPSC montado com sucesso na sua cena!\n\nAperte PLAY para atirar e 'T' para iniciar o Shot Timer.", "OK");
            }
        }
    }
}
#endif
