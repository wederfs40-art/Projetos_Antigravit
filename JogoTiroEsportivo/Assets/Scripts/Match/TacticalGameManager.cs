using UnityEngine;

namespace IPSCSimulator.Match
{
    using IPSCSimulator.Setup;

    /// <summary>
    /// Gerenciador central do jogo: controla o Menu Principal, a seleção de ambientes,
    /// a seleção do arsenal (Pistola, Dan Wesson, SMG), o menu de pausa (ESC) e a transição entre mapas.
    /// </summary>
    public class TacticalGameManager : MonoBehaviour
    {
        public static TacticalGameManager Instance { get; private set; }

        [Header("Configuração Atual")]
        public GameState CurrentState = GameState.MainMenu;
        public GameEnvironmentType SelectedEnvironment = GameEnvironmentType.IPSC_Outdoor_Short;
        public SelectedWeaponType SelectedWeapon = SelectedWeaponType.Pistol_9mm;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Se já houver um estande na cena ao dar Play direto do Editor, entra no modo jogando
            if (GameObject.Find("--- IPSC SHOOTING RANGE ---") != null)
            {
                CurrentState = GameState.Playing;
                SetCursorLocked(true);
            }
            else
            {
                CurrentState = GameState.MainMenu;
                SetCursorLocked(false);
            }
        }

        private void Update()
        {
            // Tecla ESC: alterna entre Pausa e Jogo
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (CurrentState == GameState.Playing)
                {
                    PauseGame();
                }
                else if (CurrentState == GameState.Paused)
                {
                    ResumeGame();
                }
            }
        }

        public void StartGame(GameEnvironmentType env, SelectedWeaponType weapon)
        {
            SelectedEnvironment = env;
            SelectedWeapon = weapon;
            CurrentState = GameState.Playing;

            Time.timeScale = 1.0f;
            SetCursorLocked(true);

            // Constrói o cenário 3D e arma solicitada
            SceneGeneratorCore.BuildSelectedEnvironment(env, weapon);
        }

        public void PauseGame()
        {
            CurrentState = GameState.Paused;
            Time.timeScale = 0.0f;
            SetCursorLocked(false);
        }

        public void ResumeGame()
        {
            CurrentState = GameState.Playing;
            Time.timeScale = 1.0f;
            SetCursorLocked(true);
        }

        public void ReturnToMainMenu()
        {
            CurrentState = GameState.MainMenu;
            Time.timeScale = 1.0f;
            SetCursorLocked(false);

            // Remove o cenário 3D do mundo para liberar memória
            GameObject activeRange = GameObject.Find("--- IPSC SHOOTING RANGE ---");
            if (activeRange != null)
            {
                Destroy(activeRange);
            }
        }

        public void ChangeWeaponInGame(SelectedWeaponType newWeapon)
        {
            if (SelectedWeapon == newWeapon) return;
            SelectedWeapon = newWeapon;
            SceneGeneratorCore.SwitchEquippedWeapon(newWeapon);
        }

        public void CycleWeapon(int direction)
        {
            var values = (SelectedWeaponType[])System.Enum.GetValues(typeof(SelectedWeaponType));
            int idx = System.Array.IndexOf(values, SelectedWeapon);
            if (idx < 0) idx = 0;
            idx = (idx + direction) % values.Length;
            if (idx < 0) idx += values.Length;
            ChangeWeaponInGame(values[idx]);
        }

        public void RestartCurrentMap()
        {
            StartGame(SelectedEnvironment, SelectedWeapon);
        }

        public void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
