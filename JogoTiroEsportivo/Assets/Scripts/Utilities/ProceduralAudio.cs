using UnityEngine;

namespace IPSCSimulator.Utilities
{
    /// <summary>
    /// Gera áudios realistas em tempo de execução via síntese matemática de ondas.
    /// Permite que o jogo tenha som de tiro, clique de ferrolho e o bipe oficial do Shot Timer
    /// sem precisar de arquivos externos .WAV ou .MP3.
    /// </summary>
    public static class ProceduralAudio
    {
        /// <summary>
        /// Gera o bipe estridente regulamentar de Shot Timer IPSC (onda senoidal pura de ~2.75 kHz).
        /// </summary>
        public static AudioClip CreateTimerBeepClip()
        {
            int sampleRate = 44100;
            float duration = 0.35f;
            int sampleCount = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];
            float frequency = 2750f; // Frequência padrão de buzzers de tiro

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                // Envoltória com fade out rápido nos últimos 20ms para evitar 'pop' acústico
                float envelope = 1f;
                if (i > sampleCount - (sampleRate * 0.02f))
                {
                    envelope = (float)(sampleCount - i) / (sampleRate * 0.02f);
                }
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * 0.8f * envelope;
            }

            AudioClip clip = AudioClip.Create("IPSC_Timer_Beep", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Gera o estampido sonoro de disparo de arma de fogo 9mm (impulso de pressão + ruído balístico).
        /// </summary>
        public static AudioClip CreateGunshotClip()
        {
            int sampleRate = 44100;
            float duration = 0.55f;
            int sampleCount = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                // Decaimento exponencial rápido do estampido
                float envelope = Mathf.Exp(-t * 22f);
                // Ruído branco do escape de gases
                float noise = (Random.value * 2f - 1f) * 0.85f;
                // Pressão grave de impacto mecânico inicial (130 Hz)
                float punch = Mathf.Sin(2 * Mathf.PI * 130f * t) * Mathf.Exp(-t * 35f);

                samples[i] = Mathf.Clamp((noise + punch) * envelope, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("Gunshot_9mm", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Gera o clique seco do percussor batendo na câmara vazia (Dry Fire).
        /// </summary>
        public static AudioClip CreateDryFireClip()
        {
            int sampleRate = 44100;
            float duration = 0.08f;
            int sampleCount = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 70f);
                float click = Mathf.Sin(2 * Mathf.PI * 1800f * t) * envelope;
                samples[i] = click * 0.5f;
            }

            AudioClip clip = AudioClip.Create("DryFire_Click", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Gera o som metálico de trava aberta ou fechamento de ferrolho.
        /// </summary>
        public static AudioClip CreateSlideLockClip()
        {
            int sampleRate = 44100;
            float duration = 0.15f;
            int sampleCount = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 40f);
                float metalRing = (Mathf.Sin(2 * Mathf.PI * 950f * t) + Mathf.Sin(2 * Mathf.PI * 1420f * t)) * 0.4f * envelope;
                samples[i] = metalRing;
            }

            AudioClip clip = AudioClip.Create("SlideLock_Metal", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Gera o estalido metálico estridente de bala 9mm batendo em chapa de aço IPSC (Steel Popper / Plate).
        /// </summary>
        public static AudioClip CreateSteelPingClip()
        {
            int sampleRate = 44100;
            float duration = 0.85f;
            int sampleCount = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                // Ataque ultra rápido de impacto seguido de ressonância metálica duradoura
                float envelope = Mathf.Exp(-t * 6.5f);
                float ring1 = Mathf.Sin(2 * Mathf.PI * 1850f * t);
                float ring2 = Mathf.Sin(2 * Mathf.PI * 3420f * t) * 0.5f;
                float ring3 = Mathf.Sin(2 * Mathf.PI * 920f * t) * 0.3f;
                float initialCrack = (Random.value * 2f - 1f) * Mathf.Exp(-t * 80f) * 0.6f;

                samples[i] = Mathf.Clamp((ring1 + ring2 + ring3) * envelope * 0.6f + initialCrack, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("SteelTarget_Ping", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Som mecânico de carregador (magazine) destravando e saindo da arma.
        /// </summary>
        public static AudioClip CreateMagOutClip()
        {
            int sampleRate = 44100;
            float duration = 0.22f;
            int sampleCount = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float click1 = Mathf.Sin(2 * Mathf.PI * 1200f * t) * Mathf.Exp(-t * 60f);
                float scrape = (Random.value * 2f - 1f) * 0.4f * Mathf.Exp(-t * 25f);
                samples[i] = Mathf.Clamp(click1 * 0.6f + scrape, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("Mag_Out_Click", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Som de carregador novo sendo inserido e travado no poço da empunhadura (slap).
        /// </summary>
        public static AudioClip CreateMagInClip()
        {
            int sampleRate = 44100;
            float duration = 0.28f;
            int sampleCount = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float thud = Mathf.Sin(2 * Mathf.PI * 180f * t) * Mathf.Exp(-t * 30f) * 0.8f;
                float click = Mathf.Sin(2 * Mathf.PI * 1650f * t) * Mathf.Exp(-t * 75f) * 0.7f;
                float scrape = (Random.value * 2f - 1f) * 0.35f * Mathf.Exp(-t * 40f);
                samples[i] = Mathf.Clamp(thud + click + scrape, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("Mag_In_Slap", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Som de ferrolho sendo puxado e solto batendo na câmara (slide rack).
        /// </summary>
        public static AudioClip CreateSlideRackClip()
        {
            int sampleRate = 44100;
            float duration = 0.32f;
            int sampleCount = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float rackBack = Mathf.Sin(2 * Mathf.PI * 980f * t) * Mathf.Exp(-t * 25f);
                float slam = (t > 0.12f) ? Mathf.Sin(2 * Mathf.PI * 1350f * (t - 0.12f)) * Mathf.Exp(-(t - 0.12f) * 50f) * 0.85f : 0f;
                float metal = (Random.value * 2f - 1f) * 0.3f * Mathf.Exp(-t * 20f);
                samples[i] = Mathf.Clamp(rackBack * 0.5f + slam + metal, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("Slide_Rack", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Som agudo de estojo de latão ejetado quicando no chão.
        /// </summary>
        public static AudioClip CreateCasingBounceClip()
        {
            int sampleRate = 44100;
            float duration = 0.20f;
            int sampleCount = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float clink = (Mathf.Sin(2 * Mathf.PI * 3800f * t) + Mathf.Sin(2 * Mathf.PI * 5200f * t) * 0.5f) * Mathf.Exp(-t * 45f);
                samples[i] = clink * 0.35f;
            }

            AudioClip clip = AudioClip.Create("Casing_Bounce", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Som de perfuração abafada da bala rasgando o papelão IPSC.
        /// </summary>
        public static AudioClip CreateCardboardImpactClip()
        {
            int sampleRate = 44100;
            float duration = 0.15f;
            int sampleCount = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float thud = Mathf.Sin(2 * Mathf.PI * 220f * t) * Mathf.Exp(-t * 50f) * 0.7f;
                float rip = (Random.value * 2f - 1f) * 0.6f * Mathf.Exp(-t * 35f);
                samples[i] = Mathf.Clamp(thud + rip, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("Cardboard_Rip", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
