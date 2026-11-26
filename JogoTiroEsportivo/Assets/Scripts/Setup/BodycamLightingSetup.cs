using UnityEngine;
using UnityEngine.Rendering;

namespace IPSCSimulator.Setup
{
    /// <summary>
    /// Configuração profissional de iluminação e sombras para simulação fotorrealista de Bodycam:
    /// 1. Sol direcional suave com sombras em alta definição (Soft Shadows, Bias calibrado sem artefatos)
    /// 2. Iluminação de preenchimento ambiente Trilight (Céu, Horizonte, Chão)
    /// 3. Sonda de reflexão em tempo real (Reflection Probe) para brilhos especulares no metal das armas
    /// 4. Lanterna/LED tático de peito (Chest Bodycam Light) simulando o iluminador das câmeras corporais
    /// </summary>
    public static class BodycamLightingSetup
    {
        public static void SetupBodycamLighting(Transform cameraTransform, Transform sceneRoot = null)
        {
            // 1. Sol Direcional Suave (Natural Daylight)
            Light sun = null;
            foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type == LightType.Directional)
                {
                    sun = l;
                    break;
                }
            }

            if (sun == null)
            {
                GameObject sunObj = new GameObject("Bodycam_Directional_Sun");
                if (sceneRoot != null) sunObj.transform.SetParent(sceneRoot);
                sun = sunObj.AddComponent<Light>();
                sun.type = LightType.Directional;
            }

            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            sun.color = new Color(1.0f, 0.97f, 0.92f); // Luz solar natural de dia
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.85f;
            sun.shadowBias = 0.03f;
            sun.shadowNormalBias = 0.35f;

            // 2. Iluminação Ambiente Trilight (Evita sombras pretas sem detalhes)
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.52f, 0.60f, 0.70f);      // Céu azul suave
            RenderSettings.ambientEquatorColor = new Color(0.50f, 0.48f, 0.44f);  // Linha do horizonte
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.20f, 0.18f);   // Terra e piso de concreto

            // 3. Sonda de Reflexão (Reflection Probe)
            GameObject probeObj = GameObject.Find("Bodycam_Reflection_Probe");
            if (probeObj == null)
            {
                probeObj = new GameObject("Bodycam_Reflection_Probe");
                if (sceneRoot != null) probeObj.transform.SetParent(sceneRoot);
                probeObj.transform.position = new Vector3(0, 1.8f, 5f);

                ReflectionProbe probe = probeObj.AddComponent<ReflectionProbe>();
                probe.mode = ReflectionProbeMode.Realtime;
                probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
                probe.size = new Vector3(80f, 30f, 80f);
                probe.intensity = 1.0f;
            }

            // 4. Luz Tática da Câmera Corporal (Chest-Mounted Bodycam LED Light)
            if (cameraTransform != null)
            {
                GameObject chestLightObj = GameObject.Find("Bodycam_Chest_LED");
                if (chestLightObj == null)
                {
                    chestLightObj = new GameObject("Bodycam_Chest_LED");
                    chestLightObj.transform.SetParent(cameraTransform);
                    chestLightObj.transform.localPosition = new Vector3(0f, -0.20f, 0.05f); // No peito, logo abaixo da lente
                    chestLightObj.transform.localRotation = Quaternion.identity;

                    Light chestLight = chestLightObj.AddComponent<Light>();
                    chestLight.type = LightType.Spot;
                    chestLight.color = new Color(0.92f, 0.96f, 1.0f); // LED branco frio de alta potência
                    chestLight.intensity = 0.85f;
                    chestLight.range = 18f;
                    chestLight.spotAngle = 75f;
                    chestLight.innerSpotAngle = 45f;
                    chestLight.shadows = LightShadows.Soft;
                    chestLight.shadowStrength = 0.5f;
                }
            }
        }
    }
}
