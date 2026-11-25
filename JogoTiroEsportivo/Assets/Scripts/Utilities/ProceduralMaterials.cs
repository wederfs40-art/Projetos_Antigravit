using UnityEngine;

namespace IPSCSimulator.Utilities
{
    /// <summary>
    /// Gera texturas PBR e materiais procedurais de alta fidelidade para o estande de tiro IPSC:
    /// cascalho com relevo, madeira com veios e pregos, barris metálicos com reflexos,
    /// faixas de segurança industriais, chapa de aço e fibras óticas luminosas para aparelhos de pontaria.
    /// </summary>
    public static class ProceduralMaterials
    {
        public static Material CreateMaterial(Color color, string name = "ProceduralMat", float smoothness = 0.2f, float metallic = 0.0f)
        {
            Shader standardShader = Shader.Find("Standard");
            if (standardShader == null) standardShader = Shader.Find("Universal Render Pipeline/Lit");
            if (standardShader == null) standardShader = Shader.Find("Diffuse");

            Material mat = new Material(standardShader);
            mat.name = name;
            mat.color = color;
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            return mat;
        }

        public static Material CreateEmissiveMaterial(Color color, string name, float intensity = 1.5f)
        {
            Material mat = CreateMaterial(color, name, 0.9f, 0.0f);
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", color * intensity);
            }
            return mat;
        }

        /// <summary>
        /// Textura de concreto industrial polido para estande indoor e armazém estilo CS.
        /// </summary>
        public static Texture2D CreateConcreteTexture()
        {
            int size = 512;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, true);
            Color[] pixels = new Color[size * size];

            Color darkGray = new Color(0.28f, 0.28f, 0.30f);
            Color lightGray = new Color(0.42f, 0.42f, 0.44f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;
                    float v = (float)y / size;
                    float n = Mathf.PerlinNoise(u * 8f, v * 8f) * 0.7f + Mathf.PerlinNoise(u * 30f, v * 30f) * 0.3f;
                    Color c = Color.Lerp(darkGray, lightGray, n);
                    float noise = Random.Range(-0.03f, 0.03f);
                    c.r = Mathf.Clamp01(c.r + noise);
                    c.g = Mathf.Clamp01(c.g + noise);
                    c.b = Mathf.Clamp01(c.b + noise);
                    pixels[y * size + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Textura de caixa de suprimento militar estilo CS (Madeira verde militar com reforços).
        /// </summary>
        public static Texture2D CreateMilitaryCrateTexture()
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, true);
            Color[] pixels = new Color[size * size];

            Color woodColor = new Color(0.38f, 0.42f, 0.28f);
            Color frameColor = new Color(0.18f, 0.20f, 0.15f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isBorder = (x < 16 || x > size - 16 || y < 16 || y > size - 16);
                    bool isDiagonal = (Mathf.Abs(x - y) < 8 || Mathf.Abs((size - x) - y) < 8);

                    Color c = (isBorder || isDiagonal) ? frameColor : woodColor;
                    float noise = Random.Range(-0.02f, 0.02f);
                    c.r = Mathf.Clamp01(c.r + noise);
                    c.g = Mathf.Clamp01(c.g + noise);
                    c.b = Mathf.Clamp01(c.b + noise);
                    pixels[y * size + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Textura realista de chão de estande de tiro (areia, cascalho moído e pequenas pedras).
        /// </summary>
        public static Texture2D CreateGravelTexture()
        {
            int size = 512;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, true);
            Color[] pixels = new Color[size * size];

            Color baseTan = new Color(0.48f, 0.43f, 0.36f);
            Color darkStone = new Color(0.28f, 0.25f, 0.22f);
            Color lightPebble = new Color(0.62f, 0.58f, 0.50f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;
                    float v = (float)y / size;

                    // Ruído Perlin multicamadas (macro e micro textura de solo)
                    float n1 = Mathf.PerlinNoise(u * 12f, v * 12f);
                    float n2 = Mathf.PerlinNoise(u * 45f, v * 45f) * 0.5f;
                    float n3 = Mathf.PerlinNoise(u * 120f, v * 120f) * 0.25f;
                    float val = Mathf.Clamp01(n1 * 0.6f + n2 + n3);

                    Color c = Color.Lerp(darkStone, baseTan, val);

                    // Granulação fina aleatória (grãos de areia e pedriscos)
                    float speckle = Random.Range(-0.06f, 0.06f);
                    if (Random.value < 0.04f) c = lightPebble; // pedrinha clara isolada
                    else if (Random.value < 0.03f) c = darkStone; // pedrinha escura

                    c.r = Mathf.Clamp01(c.r + speckle);
                    c.g = Mathf.Clamp01(c.g + speckle);
                    c.b = Mathf.Clamp01(c.b + speckle);

                    pixels[y * size + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Textura de tábuas de madeira de pinus com ranhuras de encaixe, veios e nós.
        /// </summary>
        public static Texture2D CreateWoodPlankTexture()
        {
            int size = 512;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, true);
            Color[] pixels = new Color[size * size];

            Color woodLight = new Color(0.68f, 0.52f, 0.35f);
            Color woodDark = new Color(0.45f, 0.32f, 0.20f);
            Color grooveColor = new Color(0.18f, 0.12f, 0.08f);

            int numPlanks = 4;
            int plankWidth = size / numPlanks;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int plankIndex = x / plankWidth;
                    int xInPlank = x % plankWidth;

                    // Sulco entre as tábuas
                    if (xInPlank < 4 || xInPlank > plankWidth - 4)
                    {
                        pixels[y * size + x] = grooveColor;
                        continue;
                    }

                    // Veios longitudinais da madeira
                    float grain = Mathf.PerlinNoise(x * 0.08f, y * 0.015f);
                    float ring = Mathf.Sin((x + grain * 30f) * 0.25f) * 0.5f + 0.5f;

                    Color c = Color.Lerp(woodDark, woodLight, ring);

                    // Variação de tonalidade entre tábuas vizinhas
                    float plankTint = (plankIndex % 2 == 0) ? 0.05f : -0.05f;
                    c.r = Mathf.Clamp01(c.r + plankTint);
                    c.g = Mathf.Clamp01(c.g + plankTint);
                    c.b = Mathf.Clamp01(c.b + plankTint);

                    pixels[y * size + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Faixas diagonais regulamentares amarelas e pretas (Hazard / Fault Line).
        /// </summary>
        public static Texture2D CreateWarningStripeTexture()
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, true);
            Color yellow = new Color(0.95f, 0.78f, 0.10f);
            Color black = new Color(0.12f, 0.12f, 0.12f);

            Color[] pixels = new Color[size * size];
            int stripeWidth = 32;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int stripeCoord = (x + y) / stripeWidth;
                    pixels[y * size + x] = (stripeCoord % 2 == 0) ? yellow : black;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Textura metálica para barris industriais de 200L (55 galões) com anéis e reflexos.
        /// </summary>
        public static Texture2D CreateBarrelTexture(Color baseColor)
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, true);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                float ny = (float)y / size;
                // Anéis de reforço na parte superior e inferior
                bool isRib = (Mathf.Abs(ny - 0.33f) < 0.035f || Mathf.Abs(ny - 0.66f) < 0.035f);

                for (int x = 0; x < size; x++)
                {
                    Color c = baseColor;
                    if (isRib)
                    {
                        c = Color.Lerp(baseColor, Color.black, 0.35f);
                    }
                    // Leve gradiente vertical de metal escovado
                    float noise = Random.Range(-0.03f, 0.03f);
                    c.r = Mathf.Clamp01(c.r + noise);
                    c.g = Mathf.Clamp01(c.g + noise);
                    c.b = Mathf.Clamp01(c.b + noise);

                    pixels[y * size + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Gera a textura de papelão clássico IPSC com zonas Alpha, Charlie e Delta demarcadas.
        /// </summary>
        public static Texture2D CreateIPSCTargetTexture(bool isNoShoot = false)
        {
            int width = 256;
            int height = 512;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            Color cardboardColor = isNoShoot ? new Color(0.92f, 0.92f, 0.92f) : new Color(0.76f, 0.58f, 0.38f);
            Color lineScoreColor = isNoShoot ? new Color(0.8f, 0.1f, 0.1f) : new Color(0.45f, 0.32f, 0.18f);

            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                float ny = (float)y / height;
                for (int x = 0; x < width; x++)
                {
                    float nx = (float)x / width;
                    Color pixel = cardboardColor;

                    // Granulação natural do papelão kraft
                    float kraftNoise = Random.Range(-0.025f, 0.025f);
                    pixel.r = Mathf.Clamp01(pixel.r + kraftNoise);
                    pixel.g = Mathf.Clamp01(pixel.g + kraftNoise);
                    pixel.b = Mathf.Clamp01(pixel.b + kraftNoise);

                    // Bordas exteriores das zonas (Charlie e Alpha)
                    if ((Mathf.Abs(nx - 0.35f) < 0.012f || Mathf.Abs(nx - 0.65f) < 0.012f) && (ny >= 0.25f && ny <= 0.70f)) pixel = lineScoreColor;
                    if ((Mathf.Abs(ny - 0.25f) < 0.006f || Mathf.Abs(ny - 0.70f) < 0.006f) && (nx >= 0.35f && nx <= 0.65f)) pixel = lineScoreColor;

                    if ((Mathf.Abs(nx - 0.20f) < 0.012f || Mathf.Abs(nx - 0.80f) < 0.012f) && (ny >= 0.12f && ny <= 0.85f)) pixel = lineScoreColor;
                    if ((Mathf.Abs(ny - 0.12f) < 0.006f || Mathf.Abs(ny - 0.85f) < 0.006f) && (nx >= 0.20f && nx <= 0.80f)) pixel = lineScoreColor;

                    // Letras de identificação no centro da zona Alpha
                    if (isNoShoot)
                    {
                        if (Mathf.Abs(nx - ny) < 0.02f || Mathf.Abs((1f - nx) - ny) < 0.02f)
                        {
                            pixel = new Color(0.85f, 0.15f, 0.15f);
                        }
                    }

                    pixels[y * width + x] = pixel;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Gera a textura do furo de bala com bordas queimadas/escuras e transparência.
        /// </summary>
        public static Texture2D CreateBulletHoleTexture()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.45f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist < radius * 0.45f)
                    {
                        // Centro escuro do orifício
                        tex.SetPixel(x, y, new Color(0.05f, 0.05f, 0.05f, 1f));
                    }
                    else if (dist < radius)
                    {
                        // Anel de fuligem e rasgo de papelão
                        float alpha = Mathf.InverseLerp(radius, radius * 0.45f, dist);
                        tex.SetPixel(x, y, new Color(0.15f, 0.12f, 0.1f, alpha));
                    }
                    else
                    {
                        // Transparente
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            return tex;
        }
    }
}
