using UnityEngine;

namespace IPSCSimulator.Utilities
{
    /// <summary>
    /// Fábrica de Materiais PBR de Alta Fidelidade (Physically Based Rendering):
    /// Gera texturas Albedo limpas e nítidas, Normal Maps procedurais via filtro gradiente Sobel,
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
        public static Texture2D GenerateNormalMapFromHeight(Texture2D heightMap, float strength = 2.0f)
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
        /// Material PBR de Paredes de Madeira Compensada Naval / OSB de Shoot House Tático (Estilo Bodycam / Unrecord).
        /// Tom natural de madeira de construção com micro-fibras suaves, sem listras artificiais.
        /// </summary>
        public static Material CreatePlywoodShootHousePBRMaterial()
        {
            Material mat = new Material(GetStandardShader());
            mat.name = "PBR_ShootHouse_OSB";

            int size = 512;
            Texture2D albedo = new Texture2D(size, size, TextureFormat.RGB24, true);
            Texture2D height = new Texture2D(size, size, TextureFormat.RGB24, false);

            Color woodBase = new Color(0.72f, 0.62f, 0.48f);
            Color woodDark = new Color(0.64f, 0.54f, 0.40f);
            Color woodLight = new Color(0.78f, 0.68f, 0.54f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;
                    float v = (float)y / size;

                    // Micro-fibras de madeira finas e realistas
                    float grainFine = Mathf.PerlinNoise(u * 40f, v * 12f) * 0.4f;
                    float grainCoarse = Mathf.PerlinNoise(u * 8f, v * 3f) * 0.4f;
                    float strands = Mathf.PerlinNoise(u * 80f, v * 20f) * 0.2f;

                    float composite = Mathf.Clamp01(grainFine + grainCoarse + strands);
                    Color c = Color.Lerp(woodDark, woodLight, composite);

                    // Micro-ruído sutil de superfície de madeira crua
                    float microNoise = (Random.value - 0.5f) * 0.035f;
                    c.r = Mathf.Clamp01(c.r + microNoise);
                    c.g = Mathf.Clamp01(c.g + microNoise);
                    c.b = Mathf.Clamp01(c.b + microNoise);

                    albedo.SetPixel(x, y, c);
                    height.SetPixel(x, y, new Color(composite, composite, composite));
                }
            }
            albedo.Apply();
            height.Apply();

            Texture2D normalMap = GenerateNormalMapFromHeight(height, 1.8f);

            mat.mainTexture = albedo;
            mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_BumpMap", normalMap);
            mat.SetFloat("_BumpScale", 0.9f);
            mat.SetFloat("_Metallic", 0.0f);
            mat.SetFloat("_Glossiness", 0.12f);

            return mat;
        }

        /// <summary>
        /// Material PBR de Piso de Concreto Industrial Polido para o Armazém / Galpão Bodycam.
        /// </summary>
        public static Material CreateWarehouseConcretePBRMaterial()
        {
            Material mat = new Material(GetStandardShader());
            mat.name = "PBR_Warehouse_Concrete";

            int size = 512;
            Texture2D albedo = new Texture2D(size, size, TextureFormat.RGB24, true);
            Texture2D height = new Texture2D(size, size, TextureFormat.RGB24, false);

            Color concBase = new Color(0.35f, 0.35f, 0.37f);
            Color concDark = new Color(0.28f, 0.28f, 0.30f);
            Color concLight = new Color(0.42f, 0.42f, 0.44f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;
                    float v = (float)y / size;

                    float n1 = Mathf.PerlinNoise(u * 12f, v * 12f) * 0.6f;
                    float n2 = Mathf.PerlinNoise(u * 45f, v * 45f) * 0.3f;
                    float speck = (Random.value - 0.5f) * 0.05f;

                    float total = Mathf.Clamp01(n1 + n2 + speck);
                    Color c = Color.Lerp(concDark, concLight, total);

                    albedo.SetPixel(x, y, c);
                    height.SetPixel(x, y, new Color(total, total, total));
                }
            }
            albedo.Apply();
            height.Apply();

            Texture2D normalMap = GenerateNormalMapFromHeight(height, 1.4f);

            mat.mainTexture = albedo;
            mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_BumpMap", normalMap);
            mat.SetFloat("_BumpScale", 0.6f);
            mat.SetFloat("_Metallic", 0.05f);
            mat.SetFloat("_Glossiness", 0.32f);

            return mat;
        }

        /// <summary>
        /// Material PBR de Solo de Estande Tático Outdoor (Cascalho e Terra Compactada com Relevo Normal).
        /// </summary>
        public static Material CreateRealisticShootRangeGroundPBRMaterial()
        {
            Material mat = new Material(GetStandardShader());
            mat.name = "PBR_Tactical_Ground";

            int size = 512;
            Texture2D albedo = new Texture2D(size, size, TextureFormat.RGB24, true);
            Texture2D height = new Texture2D(size, size, TextureFormat.RGB24, false);

            Color groundBase = new Color(0.50f, 0.46f, 0.40f);
            Color groundDark = new Color(0.42f, 0.38f, 0.32f);
            Color groundLight = new Color(0.58f, 0.54f, 0.48f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;
                    float v = (float)y / size;

                    float macro = Mathf.PerlinNoise(u * 8f, v * 8f) * 0.55f;
                    float pebbles = Mathf.PerlinNoise(u * 50f, v * 50f) * 0.35f;
                    float micro = (Random.value - 0.5f) * 0.08f;
                    float total = Mathf.Clamp01(macro + pebbles + micro);

                    Color c = Color.Lerp(groundDark, groundLight, total);
                    albedo.SetPixel(x, y, c);
                    height.SetPixel(x, y, new Color(total, total, total));
                }
            }
            albedo.Apply();
            height.Apply();

            Texture2D normalMap = GenerateNormalMapFromHeight(height, 2.2f);

            mat.mainTexture = albedo;
            mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_BumpMap", normalMap);
            mat.SetFloat("_BumpScale", 1.2f);
            mat.SetFloat("_Metallic", 0.02f);
            mat.SetFloat("_Glossiness", 0.10f);

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
            Texture2D albedo = new Texture2D(size, size, TextureFormat.RGB24, true);
            Texture2D height = new Texture2D(size, size, TextureFormat.RGB24, false);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;
                    float v = (float)y / size;
                    float brush = Mathf.PerlinNoise(u * 2f, v * 40f) * 0.12f;
                    Color c = tint + new Color(brush, brush, brush);
                    albedo.SetPixel(x, y, c);
                    height.SetPixel(x, y, new Color(brush, brush, brush));
                }
            }
            albedo.Apply();
            height.Apply();

            Texture2D normalMap = GenerateNormalMapFromHeight(height, 1.2f);

            mat.mainTexture = albedo;
            mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_BumpMap", normalMap);
            mat.SetFloat("_BumpScale", 0.7f);
            mat.SetFloat("_Metallic", 0.90f);
            mat.SetFloat("_Glossiness", 0.75f);

            return mat;
        }

        /// <summary>
        /// Material PBR de Rochas Avermelhadas de Cânion.
        /// </summary>
        public static Material CreateRedCanyonRockPBRMaterial()
        {
            Material mat = new Material(GetStandardShader());
            mat.name = "PBR_CanyonRock";

            int size = 512;
            Texture2D albedo = new Texture2D(size, size, TextureFormat.RGB24, true);
            Texture2D height = new Texture2D(size, size, TextureFormat.RGB24, false);

            Color rockDark = new Color(0.46f, 0.22f, 0.14f);
            Color rockLight = new Color(0.75f, 0.42f, 0.26f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;
                    float v = (float)y / size;
                    float n1 = Mathf.PerlinNoise(u * 6f, v * 6f) * 0.6f;
                    float n2 = Mathf.PerlinNoise(u * 30f, v * 30f) * 0.35f;
                    float total = Mathf.Clamp01(n1 + n2);

                    Color c = Color.Lerp(rockDark, rockLight, total);
                    albedo.SetPixel(x, y, c);
                    height.SetPixel(x, y, new Color(total, total, total));
                }
            }
            albedo.Apply();
            height.Apply();

            Texture2D normalMap = GenerateNormalMapFromHeight(height, 2.8f);

            mat.mainTexture = albedo;
            mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_BumpMap", normalMap);
            mat.SetFloat("_BumpScale", 1.4f);
            mat.SetFloat("_Metallic", 0.02f);
            mat.SetFloat("_Glossiness", 0.15f);

            return mat;
        }
    }
}
