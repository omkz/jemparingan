using UnityEngine;

// Pure gameplay rules shared by runtime and editor QA checks.
public static class JemparinganScoreRules
{
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
        return shotsFired < 4 ? 0 : shotsFired < 8 ? 1 : 2;
    }

    public static string StageTitle(int shotsFired)
    {
        int stage = StageIndex(shotsFired);
        return stage == 0 ? "ROUND I  •  CALM" :
            stage == 1 ? "ROUND II  •  WIND" : "ROUND III  •  RASA CHALLENGE";
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
