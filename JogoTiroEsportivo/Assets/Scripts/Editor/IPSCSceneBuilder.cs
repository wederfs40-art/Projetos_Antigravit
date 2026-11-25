#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

namespace IPSCSimulator.EditorTools
{
    using IPSCSimulator.Setup;

    /// <summary>
    /// Adiciona menus no Unity Editor para montar o estande de tiro completo
    /// de alta fidelidade e importar com 1 clique os modelos 3D baixados da Asset Store.
    /// </summary>
    public static class IPSCSceneBuilder
    {
        [MenuItem("IPSC Simulator/Montar Pista Completa (Alta Fidelidade)", false, 1)]
        public static void BuildSceneFromMenu()
        {
            if (EditorUtility.DisplayDialog(
                "Montador de Pista IPSC Realista",
                "Deseja gerar o estande de tiro de alta fidelidade visual?\n\nInclui:\n- Chão PBR de cascalho e areia\n- Barris táticos (55 galões)\n- Biombos de madeira\n- Bancada de segurança com estojos e caixas de munição\n- Alvos de papelão e Alvos Metálicos Reativos (Poppers)\n- Pistola 3D detalhada com miras de fibra ótica\n- Iluminação solar com sombras suaves e atmosfera",
                "Sim, Montar Pista!",
                "Cancelar"))
            {
                SceneGeneratorCore.GenerateCompleteShootingRange();
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorUtility.DisplayDialog("Sucesso!", "Estande de tiro IPSC de alta fidelidade montado na cena!\n\nAperte PLAY para atirar e 'T' para iniciar o Shot Timer.", "OK");
            }
        }

        [MenuItem("IPSC Simulator/Importar Armas da Asset Store/Importar Dan Wesson 715 (147MB - Arma Ultra Realista)", false, 10)]
        public static void ImportDanWessonPackage()
        {
            string pkgPath = @"C:\Users\Silvio\AppData\Roaming\Unity\Asset Store-5.x\MyNameIsVoo\3D ModelsPropsWeapons\Dan Wesson Model 715.unitypackage";
            ImportLocalPackage(pkgPath, "Dan Wesson Model 715 (Ultra Realista)");
        }

        [MenuItem("IPSC Simulator/Importar Armas da Asset Store/Importar Low Poly Pistol Pack 1", false, 11)]
        public static void ImportPistolPack1()
        {
            string pkgPath = @"C:\Users\Silvio\AppData\Roaming\Unity\Asset Store-5.x\CASTLE BRAVO\3D ModelsPropsWeaponsGuns\Low Poly Pistol Weapon Pack 1.unitypackage";
            ImportLocalPackage(pkgPath, "Low Poly Pistol Weapon Pack 1");
        }

        [MenuItem("IPSC Simulator/Importar Armas da Asset Store/Importar Low Poly Pistol Pack 2", false, 12)]
        public static void ImportPistolPack2()
        {
            string pkgPath = @"C:\Users\Silvio\AppData\Roaming\Unity\Asset Store-5.x\CASTLE BRAVO\3D ModelsPropsWeaponsGuns\Low Poly Pistol Weapon Pack 2.unitypackage";
            ImportLocalPackage(pkgPath, "Low Poly Pistol Weapon Pack 2");
        }

        [MenuItem("IPSC Simulator/Importar Armas da Asset Store/Importar Low Poly Pistol Pack 3", false, 13)]
        public static void ImportPistolPack3()
        {
            string pkgPath = @"C:\Users\Silvio\AppData\Roaming\Unity\Asset Store-5.x\CASTLE BRAVO\3D ModelsPropsWeaponsGuns\Low Poly Pistol Weapon Pack 3.unitypackage";
            ImportLocalPackage(pkgPath, "Low Poly Pistol Weapon Pack 3");
        }

        [MenuItem("IPSC Simulator/Importar Armas da Asset Store/Importar SMG Weapon Pack", false, 14)]
        public static void ImportSmgPack()
        {
            string pkgPath = @"C:\Users\Silvio\AppData\Roaming\Unity\Asset Store-5.x\CASTLE BRAVO\3D ModelsPropsWeaponsGuns\Low Poly SMG Weapon Pack 2.unitypackage";
            ImportLocalPackage(pkgPath, "Low Poly SMG Weapon Pack 2");
        }

        private static void ImportLocalPackage(string path, string packageName)
        {
            if (File.Exists(path))
            {
                Debug.Log($"[Asset Store Import] Importando pacote '{packageName}' de: {path}");
                AssetDatabase.ImportPackage(path, true); // true abre o popup para o usuário confirmar e importar
            }
            else
            {
                EditorUtility.DisplayDialog("Arquivo não encontrado", $"Não foi possível localizar o pacote '{packageName}' no caminho:\n{path}", "OK");
            }
        }
    }
}
#endif
