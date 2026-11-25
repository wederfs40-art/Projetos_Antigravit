using UnityEngine;

namespace IPSCSimulator.Setup
{
    /// <summary>
    /// Componente de inicialização automática em tempo de execução.
    /// Se o Play for pressionado mesmo em uma cena vazia,
    /// ele cria o estande de tiro, alvos e o jogador automaticamente.
    /// </summary>
    public class IPSCAutoBootstrap : MonoBehaviour
    {
        [SerializeField] private bool buildOnStart = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitOnPlay()
        {
            if (Object.FindAnyObjectByType<IPSCSimulator.Player.PlayerController>() == null)
            {
                Debug.Log("[IPSC Simulator] AutoBootstrap: Nenhum jogador na cena. Montando estande de tiro automaticamente...");
                SceneGeneratorCore.GenerateCompleteShootingRange();
            }
        }

        private void Awake()
        {
            if (buildOnStart)
            {
                if (Object.FindAnyObjectByType<IPSCSimulator.Player.PlayerController>() == null)
                {
                    SceneGeneratorCore.GenerateCompleteShootingRange();
                }
            }
        }
    }
}
