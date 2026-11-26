using UnityEngine;

namespace IPSCSimulator.Weapons
{
    /// <summary>
    /// Define a categoria operacional do armamento.
    /// </summary>
    public enum WeaponCategory
    {
        Pistolas = 0,
        Espingardas = 1,
        Submetralhadoras = 2,
        Fuzis = 3,
        Snipers = 4,
        Equipamentos = 5,
        Revolvers = 6,

        // Aliases para compatibilidade total
        Pistols = Pistolas,
        Shotguns = Espingardas,
        SubmachineGuns = Submetralhadoras,
        AssaultRifles = Fuzis,
        Equipment = Equipamentos
    }

    /// <summary>
    /// Modo de cálculo balístico do disparo.
    /// </summary>
    public enum BallisticMode
    {
        Hitscan,
        PhysicalProjectile
    }

    /// <summary>
    /// ScriptableObject modular para configuração centralizada de dados balísticos,
    /// identificação visual, recuo procedural, penetração e alinhamento de mira (ADS).
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeaponData", menuName = "Tactical Shooter/Weapons/Weapon Data", order = 100)]
    public class WeaponData : ScriptableObject
    {
        // ========================================================================================
        // 1. IDENTIFICAÇÃO E ESTATÍSTICAS
        // ========================================================================================
        [Header("1. Identificação e Estatísticas Visuais")]
        [Tooltip("Nome de exibição oficial da arma (ex: Beretta M9T, AKM Tactical, Glock 17).")]
        [SerializeField] private string weaponName = "Nova Arma";

        [Tooltip("Categoria operacional do armamento no arsenal.")]
        [SerializeField] private WeaponCategory category = WeaponCategory.Pistolas;

        [Tooltip("Ícone 2D renderizado para inventário, HUD e menu de compra/seleção.")]
        [SerializeField] private Sprite weaponIcon;

        [Tooltip("Prefab 3D do modelo da arma que será instanciado na mão ou viewmodel do jogador.")]
        [SerializeField] private GameObject weaponPrefab;

        [Header("Estatísticas Visuais para Interface (0 a 100)")]
        [Tooltip("Poder de parada e letalidade relativa da arma exibida no menu (0 a 100).")]
        [Range(0f, 100f)]
        [SerializeField] private float damageStat = 50f;

        [Tooltip("Precisão intrínseca do cano a distâncias médias/longas (0 a 100).")]
        [Range(0f, 100f)]
        [SerializeField] private float accuracyStat = 70f;

        [Tooltip("Taxa de cadência de disparo contínuo ou ciclagem do ferrolho (0 a 100).")]
        [Range(0f, 100f)]
        [SerializeField] private float fireRateStat = 60f;


        // ========================================================================================
        // 2. MODO BALÍSTICO
        // ========================================================================================
        [Header("2. Modo Balístico e Trajetória")]
        [Tooltip("Tipo de simulação balística: Hitscan (cálculo de raio instantâneo) ou PhysicalProjectile (projétil físico com velocidade real e queda).")]
        [SerializeField] private BallisticMode ballisticMode = BallisticMode.PhysicalProjectile;

        [Tooltip("Velocidade inicial de saída do projétil na boca do cano (Muzzle Velocity) em m/s. Relevante no modo Projétil Físico.")]
        [SerializeField] private float bulletSpeed = 380f;

        [Tooltip("Aceleração gravitacional aplicada ao projétil (ex: 9.81 m/s²). Controla a queda balística (Bullet Drop) a longas distâncias.")]
        [SerializeField] private float bulletGravity = 9.81f;


        // ========================================================================================
        // 3. PRECISÃO E RECUO
        // ========================================================================================
        [Header("3. Precisão e Padrão de Recuo")]
        [Tooltip("Curva de recuo vertical progressivo ao longo dos disparos contínuos (spray pattern).")]
        [SerializeField] private AnimationCurve verticalRecoilCurve = AnimationCurve.EaseInOut(0f, 0.5f, 1f, 2.5f);

        [Tooltip("Curva de oscilação lateral horizontal progressiva ao longo dos disparos (desvio para esquerda/direita).")]
        [SerializeField] private AnimationCurve horizontalRecoilCurve = AnimationCurve.Linear(0f, -0.2f, 1f, 0.4f);

        [Tooltip("Dispersão angular base inicial em repouso (em graus). Quanto menor, mais cravado no centro da mira.")]
        [SerializeField] private float baseSpread = 0.05f;

        [Tooltip("Multiplicador de dispersão aplicado quando o jogador está se movendo, correndo ou saltando.")]
        [SerializeField] private float movementSpreadMultiplier = 2.5f;


        // ========================================================================================
        // 4. PENETRAÇÃO BALÍSTICA
        // ========================================================================================
        [Header("4. Penetração Balística")]
        [Tooltip("Capacidade de atravessar superfícies sólidas (ex: compensado, portas de madeira, gesso drywall ou metal leve).")]
        [Range(0f, 100f)]
        [SerializeField] private float penetrationPower = 40f;

        [Tooltip("Fator percentual de atenuação do dano por centímetro ou camada de material atravessado (0.0 = sem perda, 1.0 = bloqueio total).")]
        [Range(0f, 1f)]
        [SerializeField] private float penetrationDamageFalloff = 0.35f;


        // ========================================================================================
        // 5. VISADA E ALINHAMENTO DE MIRA (ADS)
        // ========================================================================================
        [Header("5. Visada / ADS (Aim Down Sights)")]
        [Tooltip("Posição local (X, Y, Z) relativa à câmera onde a alça e massa de mira ficam perfeitamente alinhadas no centro do campo visual.")]
        [SerializeField] private Vector3 adsLocalPosition = new Vector3(0f, -0.046f, 0.16f);

        [Tooltip("Rotação local em graus (Euler) para alinhamento axial preciso do cano com a visão do atirador.")]
        [SerializeField] private Vector3 adsLocalRotation = Vector3.zero;

        [Tooltip("Velocidade da interpolação suave (Lerp) para transição entre o porte de guarda baixa (Hipfire) e a visada (ADS).")]
        [SerializeField] private float adsSpeed = 14f;

        [Tooltip("Campo de Visão (Field of View) da câmera ao acionar a mira. Valores menores (ex: 48-52) produzem zoom de foco visual fotorrealista.")]
        [Range(20f, 90f)]
        [SerializeField] private float adsCameraFOV = 48f;


        // ========================================================================================
        // PROPRIEDADES PÚBLICAS DE LEITURA (ENCAPSULAMENTO SÊNIOR)
        // ========================================================================================
        public string WeaponName => weaponName;
        public WeaponCategory Category => category;
        public Sprite WeaponIcon => weaponIcon;
        public GameObject WeaponPrefab => weaponPrefab;
        public float DamageStat => damageStat;
        public float AccuracyStat => accuracyStat;
        public float FireRateStat => fireRateStat;

        public BallisticMode BallisticMode => ballisticMode;
        public float BulletSpeed => bulletSpeed;
        public float BulletGravity => bulletGravity;

        public AnimationCurve VerticalRecoilCurve => verticalRecoilCurve;
        public AnimationCurve HorizontalRecoilCurve => horizontalRecoilCurve;
        public float BaseSpread => baseSpread;
        public float MovementSpreadMultiplier => movementSpreadMultiplier;

        public float PenetrationPower => penetrationPower;
        public float PenetrationDamageFalloff => penetrationDamageFalloff;

        public Vector3 AdsLocalPosition => adsLocalPosition;
        public Vector3 AdsLocalRotation => adsLocalRotation;
        public float AdsSpeed => adsSpeed;
        public float AdsCameraFOV => adsCameraFOV;
    }
}
