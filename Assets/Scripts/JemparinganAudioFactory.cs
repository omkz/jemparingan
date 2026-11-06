using UnityEngine;

// Stateless procedural audio generation. Clips are created once by JemparinganGame.
internal static class JemparinganAudioFactory
{
    // Short original one-shot bowstring sounds. Created once on scene
    // startup (22.05 kHz mono); playback does not allocate new clips.
    // Draw = quiet fibrous tension, release = snappy double-frequency twang.
    public static AudioClip GenerateModernStringSound(
        string name, bool release)
    {
        const int sampleRate = 22050;
        float duration = release ? .19f : .15f;
        int count = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[count];
        uint noiseState = release ? 81263u : 92507u;
        float baseHz = release ? 175f : 96f;
        for (int index = 0; index < count; index++)
        {
            float t = (float)index / sampleRate;
            noiseState = noiseState * 1664525u + 1013904223u;
            float noise = (noiseState >> 16) / 32768f - 1f;
            float decay = Mathf.Exp(-t * (release ? 26f : 15f));
            float pitch = baseHz * (1f + .23f * Mathf.Exp(-t * 40f));
            float stringBody = Mathf.Sin(2f * Mathf.PI * pitch * t) +
                .23f * Mathf.Sin(2f * Mathf.PI * pitch * 2.08f * t);
            float snap = noise * Mathf.Exp(-t * (release ? 95f : 35f));
            samples[index] = decay * (release ? .25f : .08f) *
                stringBody + snap * (release ? .19f : .035f);
        }
        AudioClip clip = AudioClip.Create(
            name, count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Short original synthesized wooden hit: harmonic resonance + soft transient.
    // This is sound design, not a claim to be recorded traditional instrumentation.
    public static AudioClip GenerateWoodImpact(string name, bool center)
    {
        const int sampleRate = 22050;
        const float duration = .38f;
        int count = Mathf.CeilToInt(duration * sampleRate);
        var samples = new float[count];
        float baseHz = center ? 590f : 365f;
        uint random = center ? 12345u : 67891u;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / sampleRate;
            float decay = Mathf.Exp(-18f * t);
            random = random * 1664525u + 1013904223u;
            float noise = ((random >> 16) / 32768f - 1f);
            float body = Mathf.Sin(2f * Mathf.PI * baseHz * t) +
                .24f * Mathf.Sin(2f * Mathf.PI * baseHz * 2.12f * t);
            samples[i] = .19f * decay * body +
                .10f * Mathf.Exp(-95f * t) * noise;
        }
        var clip = AudioClip.Create(name, count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Seamless low-level wind texture made once, then looped without streaming or files.
    // No external music/assets, and no allocations in gameplay Update.
    public static AudioClip GenerateWindAmbient(string name, float seconds)
    {
        const int sampleRate = 22050;
        int count = Mathf.CeilToInt(seconds * sampleRate);
        var samples = new float[count];
        float smoothedNoise = 0f;
        uint random = 40621u;
        for (int i = 0; i < count; i++)
        {
            float phase = i / (float)count * Mathf.PI * 2f;
            random = random * 1664525u + 1013904223u;
            float noise = ((random >> 16) / 32768f - 1f);
            smoothedNoise = Mathf.Lerp(smoothedNoise, noise, .0025f);
            float loopEnvelope = Mathf.Sin(phase) * Mathf.Sin(phase);
            float breeze = .45f * Mathf.Sin(phase * 2f) +
                .3f * Mathf.Sin(phase * 5f + .8f);
            samples[i] = .12f * (breeze + smoothedNoise * loopEnvelope);
        }
        var clip = AudioClip.Create(name, count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public static AudioClip GenerateTone(string name, float duration, float frequency, bool twang)
    {
        const int sampleRate = 22050;
        int length = Mathf.CeilToInt(duration * sampleRate);
        var samples = new float[length];
        for (int i = 0; i < length; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Pow(1f - t / duration, twang ? 4f : 2.1f);
            float frequencyNow = twang ? frequency * (1f + .35f * Mathf.Exp(-35f * t)) : frequency;
            samples[i] = .25f * envelope * (Mathf.Sin(2f * Mathf.PI * frequencyNow * t)
                + .22f * Mathf.Sin(2f * Mathf.PI * frequencyNow * 2.1f * t));
        }
        AudioClip clip = AudioClip.Create(name, length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
