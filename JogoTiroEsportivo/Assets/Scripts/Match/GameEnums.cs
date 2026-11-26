namespace IPSCSimulator.Match
{
    public enum GameEnvironmentType
    {
        IPSC_Outdoor_Short,     // Pista Curta IPSC (3 alvos + 2 poppers)
        IPSC_Outdoor_Medium,    // Pista Média IPSC (Corredores, Biombos, No-Shoots)
        IPSC_Outdoor_Long,      // Pista Longa IPSC (Grande Torneio 30m, 8 alvos)
        Indoor_Precision_Stand, // Estande Fechado com Cabines e Alvos em Trilhos
        Tactical_CS_Killhouse,  // Galpão Tático estilo CS com Contêineres e CQB
        Bodycam_ShootHouse,     // Killhouse Policial em Madeira Compensada (Estilo BODYCAM / UNRECORD)
        Outdoor_Precision_Stand // Stand de Tiro Aberto com Berma e Foco Visual (Conforme Ilustração)
    }

    public enum WeaponCategory
    {
        Pistols,        // Pistolas Semiautomáticas
        Revolvers,      // Revólveres & Magnuns
        SubmachineGuns, // Submetralhadoras (SMGs)
        AssaultRifles,  // Fuzis de Assalto (Rifles)
        Shotguns        // Espingardas Calibre 12
    }

    public enum SelectedWeaponType
    {
        // --- PISTOLAS ---
        Pistol_Beretta_M9T_Black = 0,
        Pistol_Beretta_M9T_Tan = 1,
        Pistol_Tactical_Glock = 2,
        Pistol_Combat_Custom = 3,

        // --- REVÓLVERES ---
        Revolver_DanWesson_357 = 10,
        Revolver_Superhawk_Tactical = 11,

        // --- SUBMETRALHADORAS (SMGs) ---
        SMG_Strongarm_Tactical = 20,
        SMG_MP_Vector = 21,
        SMG_P90_Compact = 22,

        // --- FUZIS DE ASSALTO ---
        Rifle_AKM_Tactical = 30,

        // Aliases mantidos para compatibilidade retroativa total
        Pistol_9mm = Pistol_Beretta_M9T_Black,
        Revolver_DanWesson = Revolver_DanWesson_357,
        SMG_Tactical = Rifle_AKM_Tactical
    }

    public enum GameState
    {
        MainMenu,
        Playing,
        Paused
    }
}
