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

    public enum SelectedWeaponType
    {
        Pistol_9mm,         // Pistola 9mm (17 munições, cadência rápida)
        Revolver_DanWesson, // Revólver Dan Wesson .357 (6 tiros de alto impacto)
        SMG_Tactical        // Submetralhadora SMG (30 tiros automática)
    }

    public enum GameState
    {
        MainMenu,
        Playing,
        Paused
    }
}
