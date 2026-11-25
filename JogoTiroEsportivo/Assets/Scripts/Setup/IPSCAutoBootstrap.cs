using UnityEngine;

namespace IPSCSimulator.Setup
{
    /// <summary>
    /// Componente de inicialização automática em tempo de execução.
    /// Se colocado em qualquer cena vazia e o Play for pressionado,
    /// ele cria o estande de tiro, alvos e o jogador automaticamente.
    /// </summary>
    public class IPSCAutoBootstrap : MonoBehaviour
    {
        [SerializeField] private bool buildOnStart = true;

        private void Awake()
        {
            if (buildOnStart)
            {
                // Se não houver nenhum jogador na cena, monta tudo
                if (FindObjectOfType<IPSCSimulator.Player.PlayerController>() == null)
                {
                    SceneGeneratorCore.GenerateCompleteShootingRange();
                }
            }
        }
    }
}
