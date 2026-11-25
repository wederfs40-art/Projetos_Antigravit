using UnityEngine;

namespace IPSCSimulator.Utilities
{
    /// <summary>
    /// Gera texturas e materiais procedurais para alvos IPSC, furos de bala e superfícies do estande.
    /// Garante que a cena funcione visualmente mesmo sem pacotes de arte externos.
    /// </summary>
    public static class ProceduralMaterials
    {
        public static Material CreateMaterial(Color color, string name = "ProceduralMat", float smoothness = 0.2f)
        {
            Shader standardShader = Shader.Find("Standard");
            if (standardShader == null) standardShader = Shader.Find("Universal Render Pipeline/Lit");
            if (standardShader == null) standardShader = Shader.Find("Diffuse");

            Material mat = new Material(standardShader);
            mat.name = name;
            mat.color = color;
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            return mat;
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

                    // Bordas exteriores das zonas (Charlie e Alpha)
                    bool isAlphaBox = (nx >= 0.35f && nx <= 0.65f && ny >= 0.25f && ny <= 0.70f);
                    bool isCharlieBox = (nx >= 0.20f && nx <= 0.80f && ny >= 0.12f && ny <= 0.85f);

                    // Desenha linhas de demarcação sutil
                    if ((Mathf.Abs(nx - 0.35f) < 0.01f || Mathf.Abs(nx - 0.65f) < 0.01f) && (ny >= 0.25f && ny <= 0.70f)) pixel = lineScoreColor;
                    if ((Mathf.Abs(ny - 0.25f) < 0.005f || Mathf.Abs(ny - 0.70f) < 0.005f) && (nx >= 0.35f && nx <= 0.65f)) pixel = lineScoreColor;

                    if ((Mathf.Abs(nx - 0.20f) < 0.01f || Mathf.Abs(nx - 0.80f) < 0.01f) && (ny >= 0.12f && ny <= 0.85f)) pixel = lineScoreColor;
                    if ((Mathf.Abs(ny - 0.12f) < 0.005f || Mathf.Abs(ny - 0.85f) < 0.005f) && (nx >= 0.20f && nx <= 0.80f)) pixel = lineScoreColor;

                    // Marca de "X" vermelho se for No-Shoot
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
