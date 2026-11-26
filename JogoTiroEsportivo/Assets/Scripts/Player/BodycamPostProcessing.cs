using UnityEngine;

namespace IPSCSimulator.Player
{
    /// <summary>
    /// Efeito de Pós-Processamento de Câmera Corporal (Bodycam / Unrecord Style Post-Processing):
    /// 1. Distorção óptica esférica de lente grande angular (Fisheye / Barrel Distortion)
    /// 2. Aberração cromática radial nas bordas da lente
    /// 3. Tonemapping ACES para alto alcance dinâmico fotorrealista
    /// 4. Gradação de cor de câmera policial (alto contraste, leve dessaturação)
    /// 5. Vinheta e granulado de sensor CMOS de alta sensibilidade
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class BodycamPostProcessing : MonoBehaviour
    {
        [Header("Shader de Lente")]
        [SerializeField] private Shader lensShader;

        [Header("Distorção de Lente Fisheye")]
        [Range(0f, 0.45f)] public float distortion = 0.16f;
        [Range(0f, 0.25f)] public float cubicDistortion = 0.08f;
        [Range(0f, 0.03f)] public float chromaticAberration = 0.0055f;

        [Header("Gradação e Exposição ACES")]
        [Range(0.5f, 2.0f)] public float exposure = 1.15f;
        [Range(0.5f, 2.0f)] public float contrast = 1.18f;
        [Range(0.0f, 1.5f)] public float saturation = 0.90f;

        [Header("Sensor e Vinheta")]
        [Range(0.5f, 2.0f)] public float vignetteRoundness = 1.12f;
        [Range(0.5f, 3.5f)] public float vignettePower = 1.8f;
        [Range(0f, 0.06f)] public float filmGrain = 0.020f;

        private Material _lensMaterial;

        private void OnEnable()
        {
            EnsureMaterial();
        }

        private void EnsureMaterial()
        {
            if (lensShader == null)
            {
                lensShader = Shader.Find("Custom/BodycamLensEffect");
            }

            if (lensShader != null && _lensMaterial == null)
            {
                _lensMaterial = new Material(lensShader);
                _lensMaterial.hideFlags = HideFlags.DontSave;
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            EnsureMaterial();

            if (_lensMaterial != null && _lensMaterial.shader.isSupported)
            {
                _lensMaterial.SetFloat("_Distortion", distortion);
                _lensMaterial.SetFloat("_CubicDistortion", cubicDistortion);
                _lensMaterial.SetFloat("_ChromAberration", chromaticAberration);
                _lensMaterial.SetFloat("_Exposure", exposure);
                _lensMaterial.SetFloat("_Contrast", contrast);
                _lensMaterial.SetFloat("_Saturation", saturation);
                _lensMaterial.SetFloat("_VignetteRoundness", vignetteRoundness);
                _lensMaterial.SetFloat("_VignettePower", vignettePower);
                _lensMaterial.SetFloat("_FilmGrain", filmGrain);

                Graphics.Blit(source, destination, _lensMaterial);
            }
            else
            {
                Graphics.Blit(source, destination);
            }
        }

        private void OnDisable()
        {
            if (_lensMaterial != null)
            {
                DestroyImmediate(_lensMaterial);
            }
        }
    }
}
