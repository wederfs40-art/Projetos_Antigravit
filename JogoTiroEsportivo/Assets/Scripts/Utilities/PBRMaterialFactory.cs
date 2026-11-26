using UnityEngine;

namespace IPSCSimulator.Utilities
{
    /// <summary>
    /// Fábrica de Materiais PBR de Alta Fidelidade (Physically Based Rendering):
    /// Gera texturas Albedo, Normal Maps procedurais via filtro Sobel em tempo de execução,
    /// e configura parâmetros físicos de Metalicidade e Rugosidade (Smoothness/Roughness).
    /// </summary>
    public static class PBRMaterialFactory
    {
        private static Shader _standardShader;

        private static Shader GetStandardShader()
        {
            if (_standardShader == null)
            {
                _standardShader = Shader.Find("Standard");
                if (_standardShader == null) _standardShader = Shader.Find("Universal Render Pipeline/Lit");
                if (_standardShader == null) _standardShader = Shader.Find("Mobile/Diffuse");
            }
            return _standardShader;
        }

        /// <summary>
        /// Gera uma textura de Normal Map tangencial PBR aplicando filtro gradiente Sobel sobre um mapa de altura.
        /// </summary>
        public static Texture2D GenerateNormalMapFromHeight(Texture2D heightMap, float strength = 2.5f)
        {
            int w = heightMap.width;
            int h = heightMap.height;
            Texture2D normalTex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float left = heightMap.GetPixel((x - 1 + w) % w, y).grayscale;
                    float right = heightMap.GetPixel((x + 1) % w, y).grayscale;
                    float down = heightMap.GetPixel(x, (y - 1 + h) % h).grayscale;
                    float up = heightMap.GetPixel(x, (y + 1) % h).grayscale;

                    float dx = (right - left) * strength;
                    float dy = (up - down) * strength;

                    Vector3 normal = new Vector3(-dx, -dy, 1f).normalized;

                    Color normalColor = new Color(
                        normal.x * 0.5f + 0.5f,
                        normal.y * 0.5f + 0.5f,
                        normal.z * 0.5f + 0.5f,
                        1f
                    );

                    normalTex.SetPixel(x, y, normalColor);
                }
            }

            normalTex.Apply();
            return normalTex;
        }

        /// <summary>
        /// Material PBR de Luvas Táticas Militares (Couro escuro + Kevlar com relevo de poros).
        /// </summary>
        public static Material CreateCombatGlovePBRMaterial()
        {
            Material mat = new Material(GetStandardShader());
            mat.name = "PBR_CombatGlove";

            int size = 256;
            Texture2D albedo = new Texture2D(size, size);
            Texture2D height = new Texture2D(size, size);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float noise1 = Mathf.PerlinNoise(x * 0.15f, y * 0.15f);
                    float noise2 = Mathf.PerlinNoise(x * 0.35f, y * 0.35f);
                    float composite = (noise1 * 0.7f + noise2 * 0.3f);

                    Color c = Color.Lerp(new Color(0.12f, 0.13f, 0.12f), new Color(0.20f, 0.22f, 0.20f), composite);
                    albedo.SetPixel(x, y, c);
                    height.SetPixel(x, y, new Color(composite, composite, composite));
                }
            }
            albedo.Apply();
            height.Apply();

            Texture2D normalMap = GenerateNormalMapFromHeight(height, 3.2f);

            mat.mainTexture = albedo;
            mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_BumpMap", normalMap);
            mat.SetFloat("_BumpScale", 1.2f);
            mat.SetFloat("_Metallic", 0.18f);
            mat.SetFloat("_Glossiness", 0.45f);

            return mat;
        }

        /// <summary>
        /// Material PBR de Manga Camuflada Tática (Tecido Ripstop militar).
        /// </summary>
        public static Material CreateTacticalCamoPBRMaterial()
        {
            Material mat = new Material(GetStandardShader());
            mat.name = "PBR_TacticalCamo";

            int size = 256;
            Texture2D albedo = new Texture2D(size, size);
            Texture2D height = new Texture2D(size, size);

            Color greenCamo = new Color(0.22f, 0.27f, 0.18f);
            Color tanCamo = new Color(0.38f, 0.35f, 0.25f);
            Color darkCamo = new Color(0.12f, 0.14f, 0.10f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float p = Mathf.PerlinNoise(x * 0.04f, y * 0.04f);
                    Color c = p < 0.42f ? darkCamo : (p < 0.70f ? greenCamo : tanCamo);

                    // Padrão quadriculado de tecido Ripstop anti-rasgo
                    float ripstop = ((x % 8 == 0) || (y % 8 == 0)) ? 0.85f : 1.0f;
                    c *= ripstop;

                    albedo.SetPixel(x, y, c);
                    height.SetPixel(x, y, new Color(p * ripstop, p * ripstop, p * ripstop));
                }
            }
            albedo.Apply();
            height.Apply();

            Texture2D normalMap = GenerateNormalMapFromHeight(height, 2.8f);

            mat.mainTexture = albedo;
            mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_BumpMap", normalMap);
            mat.SetFloat("_BumpScale", 0.85f);
            mat.SetFloat("_Metallic", 0.05f);
            mat.SetFloat("_Glossiness", 0.20f);

            return mat;
        }

        /// <summary>
        /// Material PBR de Aço Fosco Balístico de Armamento com Normal Map de escovação.
        /// </summary>
        public static Material CreateGunSteelPBRMaterial(Color tint)
        {
            Material mat = new Material(GetStandardShader());
            mat.name = "PBR_GunSteel";

            int size = 256;
            Texture2D albedo = new Texture2D(size, size);
            Texture2D height = new Texture2D(size, size);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Ranhuras lineares de aço escovado
                    float brush = Mathf.PerlinNoise(x * 0.05f, y * 0.85f) * 0.15f;
                    Color c = tint + new Color(brush, brush, brush);
                    albedo.SetPixel(x, y, c);
                    height.SetPixel(x, y, new Color(brush, brush, brush));
                }
            }
            albedo.Apply();
            height.Apply();

            Texture2D normalMap = GenerateNormalMapFromHeight(height, 1.8f);

            mat.mainTexture = albedo;
            mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_BumpMap", normalMap);
            mat.SetFloat("_BumpScale", 0.95f);
            mat.SetFloat("_Metallic", 0.88f);
            mat.SetFloat("_Glossiness", 0.78f);

            return mat;
        }

        /// <summary>
        /// Material PBR de Rochas Avermelhadas de Cânion (Inspirado na imagem de referência do usuário).
        /// </summary>
        public static Material CreateRedCanyonRockPBRMaterial()
        {
            Material mat = new Material(GetStandardShader());
            mat.name = "PBR_CanyonRock";

            int size = 256;
            Texture2D albedo = new Texture2D(size, size);
            Texture2D height = new Texture2D(size, size);

            Color rockBase = new Color(0.68f, 0.32f, 0.20f);
            Color rockDark = new Color(0.42f, 0.18f, 0.11f);
            Color rockLight = new Color(0.82f, 0.45f, 0.28f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float n1 = Mathf.PerlinNoise(x * 0.05f, y * 0.05f);
                    float n2 = Mathf.PerlinNoise(x * 0.22f, y * 0.22f) * 0.4f;
                    float total = n1 + n2;

                    Color c = Color.Lerp(rockDark, rockLight, total * 0.7f);
                    albedo.SetPixel(x, y, c);
                    height.SetPixel(x, y, new Color(total, total, total));
                }
            }
            albedo.Apply();
            height.Apply();

            Texture2D normalMap = GenerateNormalMapFromHeight(height, 4.0f);

            mat.mainTexture = albedo;
            mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_BumpMap", normalMap);
            mat.SetFloat("_BumpScale", 1.8f);
            mat.SetFloat("_Metallic", 0.04f);
            mat.SetFloat("_Glossiness", 0.15f);

            return mat;
        }

        /// <summary>
        /// Material PBR de Paredes de Madeira/OSB de Shoot House Tático (Inspirado no visual de Bodycam).
        /// </summary>
        public static Material CreatePlywoodShootHousePBRMaterial()
        {
            Material mat = new Material(GetStandardShader());
            mat.name = "PBR_ShootHouse_OSB";

            int size = 256;
            Texture2D albedo = new Texture2D(size, size);
            Texture2D height = new Texture2D(size, size);

            Color woodLight = new Color(0.80f, 0.65f, 0.45f);
            Color woodDark = new Color(0.58f, 0.44f, 0.28f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Fibras orientadas de madeira com veios e nós
                    float grain = Mathf.Sin((x * 0.25f) + Mathf.PerlinNoise(x * 0.08f, y * 0.02f) * 12f) * 0.5f + 0.5f;
                    float chips = Mathf.PerlinNoise(x * 0.35f, y * 0.35f) * 0.35f;
                    float total = Mathf.Clamp01((grain * 0.65f) + chips);

                    Color c = Color.Lerp(woodDark, woodLight, total);
                    albedo.SetPixel(x, y, c);
                    height.SetPixel(x, y, new Color(total, total, total));
                }
            }
            albedo.Apply();
            height.Apply();

            Texture2D normalMap = GenerateNormalMapFromHeight(height, 2.5f);

            mat.mainTexture = albedo;
            mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_BumpMap", normalMap);
            mat.SetFloat("_BumpScale", 1.2f);
            mat.SetFloat("_Metallic", 0.02f);
            mat.SetFloat("_Glossiness", 0.18f);

            return mat;
        }

        /// <summary>
        /// Material PBR de Solo de Estande Tático (Cascalho e Terra Compactada com Relevo Normal).
        /// </summary>
        public static Material CreateRealisticShootRangeGroundPBRMaterial()
        {
            Material mat = new Material(GetStandardShader());
            mat.name = "PBR_Tactical_Ground";

            int size = 256;
            Texture2D albedo = new Texture2D(size, size);
            Texture2D height = new Texture2D(size, size);

            Color groundBase = new Color(0.55f, 0.48f, 0.38f);
            Color groundDark = new Color(0.40f, 0.34f, 0.26f);
            Color groundLight = new Color(0.68f, 0.60f, 0.48f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float macro = Mathf.PerlinNoise(x * 0.04f, y * 0.04f);
                    float pebbles = Mathf.PerlinNoise(x * 0.45f, y * 0.45f) * 0.4f;
                    float micro = (UnityEngine.Random.value - 0.5f) * 0.12f;
                    float total = Mathf.Clamp01(macro * 0.6f + pebbles + micro);

                    Color c = Color.Lerp(groundDark, groundLight, total);
                    albedo.SetPixel(x, y, c);
                    height.SetPixel(x, y, new Color(total, total, total));
                }
            }
            albedo.Apply();
            height.Apply();

            Texture2D normalMap = GenerateNormalMapFromHeight(height, 3.2f);

            mat.mainTexture = albedo;
            mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_BumpMap", normalMap);
            mat.SetFloat("_BumpScale", 1.6f);
            mat.SetFloat("_Metallic", 0.03f);
            mat.SetFloat("_Glossiness", 0.12f);

            return mat;
        }
    }
}
