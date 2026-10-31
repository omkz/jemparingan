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
        return stage == 0 ? "BABAK I  •  TENANG" :
            stage == 1 ? "BABAK II  •  ANGIN" : "BABAK III  •  UJIAN RASA";
    }

    public static string ResultTitle(int hits, int stableHits)
    {
        if (hits >= 8 && stableHits >= 4) return "R A S A   T E R J A G A";
        if (hits >= 8) return "P A N A H   J I T U";
        if (stableHits >= 3) return "T E N A N G   B E R T U M B U H";
        return "L A N G K A H   P E R T A M A";
    }

    public static int PoolCapacity(int arrowsPerRound, int baseCapacity)
    {
        return Mathf.Max(1, Mathf.Max(arrowsPerRound, baseCapacity));
    }
}
