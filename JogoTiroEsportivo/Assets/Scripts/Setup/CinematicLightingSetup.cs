using UnityEngine;
using UnityEngine.Rendering;

namespace IPSCSimulator.Setup
{
    /// <summary>
    /// Configura a iluminação cinematográfica fotorrealista da cena:
    /// 1. Luz solar direcional com sombras suaves de alta definição (Soft Shadows)
    /// 2. Iluminação ambiente por gradiente de 3 tons (Céu, Horizonte e Chão)
    /// 3. Sonda de reflexão (Reflection Probe) para reflexos metálicos PBR na arma e estojos
    /// </summary>
    public static class CinematicLightingSetup
    {
        public static void ApplyCinematicLighting(Transform sceneRoot, Vector3 sunRotation, float sunIntensity = 1.15f)
        {
            // 1. Configura Luz Direcional (Sol)
            Light sunLight = null;
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    sunLight = l;
                    break;
                }
            }

            if (sunLight == null)
            {
                GameObject sunObj = new GameObject("Cinematic_Sun_Light");
                sunObj.transform.SetParent(sceneRoot);
                sunLight = sunObj.AddComponent<Light>();
                sunLight.type = LightType.Directional;
            }

            sunLight.transform.rotation = Quaternion.Euler(sunRotation);
            sunLight.color = new Color(1.0f, 0.96f, 0.90f); // Luz solar natural levemente dourada
            sunLight.intensity = sunIntensity;
            sunLight.shadows = LightShadows.Soft;
            sunLight.shadowStrength = 0.88f;
            sunLight.shadowBias = 0.025f;
            sunLight.shadowNormalBias = 0.35f;

            // 2. Iluminação Ambiente por Gradiente Tridimensional PBR
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.48f, 0.58f, 0.72f);      // Azul suave do céu
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.52f, 0.46f);  // Tom neutro do horizonte
            RenderSettings.ambientGroundColor = new Color(0.24f, 0.20f, 0.16f);   // Reflexo quente da terra/chão

            // 3. Sonda de Reflexão em Tempo Real (Reflection Probe)
            GameObject probeObj = GameObject.Find("Cinematic_Reflection_Probe");
            if (probeObj == null)
            {
                probeObj = new GameObject("Cinematic_Reflection_Probe");
                probeObj.transform.SetParent(sceneRoot);
                probeObj.transform.position = new Vector3(0, 1.8f, 8f);

                ReflectionProbe probe = probeObj.AddComponent<ReflectionProbe>();
                probe.mode = ReflectionProbeMode.Realtime;
                probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
                probe.size = new Vector3(80f, 35f, 80f);
                probe.intensity = 0.95f;
            }
        }
    }
}
