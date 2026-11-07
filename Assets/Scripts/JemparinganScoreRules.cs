using UnityEngine;

// Pure scoring and capacity helpers for Traditional and Modern gameplay.
public static class JemparinganScoreRules
{
    // Mataram-inspired red/yellow/white bandulan scoring.
    public static int TraditionalPointsForZone(JemparinganTarget.TraditionalZone zone)
    {
        switch (zone)
        {
            case JemparinganTarget.TraditionalZone.Molo: return 3;
            case JemparinganTarget.TraditionalZone.Jangga: return 2;
            case JemparinganTarget.TraditionalZone.Awak: return 1;
            default: return 0;
        }
    }

    public static int AccuracyPercent(int hits, int shots)
    {
        if (shots <= 0) return 0;
        return Mathf.Clamp(Mathf.RoundToInt(hits * 100f / shots), 0, 100);
    }

    public static int PoolCapacity(int arrowsPerRound, int baseCapacity)
    {
        return Mathf.Max(1, Mathf.Max(arrowsPerRound, baseCapacity));
    }
}
