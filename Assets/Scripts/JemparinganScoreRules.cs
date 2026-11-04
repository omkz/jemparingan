using UnityEngine;

// Pure gameplay rules shared by runtime and editor QA checks.
public static class JemparinganScoreRules
{
    public const int ArrowsPerStage = 4;
    public const int OutdoorRoundCount = 4;
    public const int OutdoorArrowCount = ArrowsPerStage * OutdoorRoundCount;

    public static int PointsForHit(bool bullseye, bool stableRelease, int consecutiveHits)
    {
        return (bullseye ? 10 : 5) + (stableRelease ? 3 : 0) +
            (consecutiveHits >= 3 ? 2 : 0);
    }

    public static int AccuracyPercent(int hits, int shots)
    {
        if (shots <= 0) return 0;
        return Mathf.Clamp(Mathf.RoundToInt(hits * 100f / shots), 0, 100);
    }

    public static int StageIndex(int shotsFired)
    {
        return Mathf.Clamp(Mathf.Max(0, shotsFired) / ArrowsPerStage,
            0, OutdoorRoundCount - 1);
    }

    public static string StageTitle(int shotsFired)
    {
        switch (StageIndex(shotsFired))
        {
            case 0: return "ROUND I  •  CALM";
            case 1: return "ROUND II  •  WIND";
            case 2: return "ROUND III  •  RASA CHALLENGE";
            default: return "ROUND IV  •  EXTREME SWING";
        }
    }

    // First three stages keep their existing swing settings. The final stage
    // has 1.8x the Round III travel and ~1.35x its angular speed. Sine-based
    // motion naturally eases at both turning points; there is no randomness.
    public static float SwayAmountForStage(int stage)
    {
        return stage >= 3 ? 0.27f : 0.06f + Mathf.Clamp(stage, 0, 2) * 0.045f;
    }

    public static float SwaySpeedForStage(int stage)
    {
        return stage >= 3 ? 2.35f : 0.7f + Mathf.Clamp(stage, 0, 2) * 0.52f;
    }

    public static string ResultTitle(int hits, int stableHits)
    {
        if (hits >= 8 && stableHits >= 4) return "RASA IN BALANCE";
        if (hits >= 8) return "TRUE AIM";
        if (stableHits >= 3) return "FINDING CALM";
        return "FIRST STEPS";
    }

    public static int PoolCapacity(int arrowsPerRound, int baseCapacity)
    {
        return Mathf.Max(1, Mathf.Max(arrowsPerRound, baseCapacity));
    }
}
