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
    }
}
