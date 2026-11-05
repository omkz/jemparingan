using System;
using UnityEngine;

// Shared rules, configuration, and medal evaluation for Jemparingan Challenge Mode.
public static class JemparinganChallengeRules
{
    public enum MissionId
    {
        Precision,
        MovingTarget
    }

    public enum Medal
    {
        None = 0,
        Bronze = 1,
        Silver = 2,
        Gold = 3
    }

    [System.Serializable]
    public struct MissionConfig
    {
        public MissionId Id;
        public string Name;
        public string Subtitle;
        public string Description;
        public int ArrowCount;
        public int BronzeThreshold;
        public int SilverThreshold;
        public int GoldThreshold;
        public float TargetSway;
        public float TargetSpeed;
        public string ObjectiveLabel;
        public string ObjectiveUnit;

        public Medal Evaluate(int objectiveHits)
        {
            if (objectiveHits >= GoldThreshold) return Medal.Gold;
            if (objectiveHits >= SilverThreshold) return Medal.Silver;
            if (objectiveHits >= BronzeThreshold) return Medal.Bronze;
            return Medal.None;
        }

        public string GetThresholdsText()
        {
            return "Bronze: " + BronzeThreshold + " " + ObjectiveUnit +
                   "  |  Silver: " + SilverThreshold + " " + ObjectiveUnit +
                   "  |  Gold: " + GoldThreshold + " " + ObjectiveUnit;
        }

        public string GetProgressHint(int objectiveHits)
        {
            if (objectiveHits >= GoldThreshold) return "GOLD REACHED!";
            if (objectiveHits >= SilverThreshold) return (GoldThreshold - objectiveHits) + " to Gold";
            if (objectiveHits >= BronzeThreshold) return (SilverThreshold - objectiveHits) + " to Silver";
            return (BronzeThreshold - objectiveHits) + " to Bronze";
        }
    }

    public static MissionConfig GetConfig(MissionId id, float movingSway = 0.08f, float movingSpeed = 0.9f)
    {
        switch (id)
        {
            case MissionId.Precision:
                return new MissionConfig
                {
                    Id = MissionId.Precision,
                    Name = "Precision",
                    Subtitle = "Stationary Bandulan • Molo Focus",
                    Description = "Hit the small red molo head on the stationary bandulan.",
                    ArrowCount = 8,
                    BronzeThreshold = 1,
                    SilverThreshold = 2,
                    GoldThreshold = 3,
                    TargetSway = 0f,
                    TargetSpeed = 0f,
                    ObjectiveLabel = "Molo Hits",
                    ObjectiveUnit = "Molo"
                };

            case MissionId.MovingTarget:
            default:
                return new MissionConfig
                {
                    Id = MissionId.MovingTarget,
                    Name = "Moving Target",
                    Subtitle = "Swinging Bandulan • Target Timing",
                    Description = "Strike the swaying bandulan in horizontal pendulum motion.",
                    ArrowCount = 10,
                    BronzeThreshold = 2,
                    SilverThreshold = 4,
                    GoldThreshold = 6,
                    TargetSway = Mathf.Clamp(movingSway, 0.02f, 0.25f),
                    TargetSpeed = Mathf.Clamp(movingSpeed, 0.2f, 3.0f),
                    ObjectiveLabel = "Bandulan Hits",
                    ObjectiveUnit = "Hits"
                };
        }
    }

    public static string MedalTitle(Medal medal)
    {
        switch (medal)
        {
            case Medal.Gold: return "GOLD MEDAL";
            case Medal.Silver: return "SILVER MEDAL";
            case Medal.Bronze: return "BRONZE MEDAL";
            default: return "NO MEDAL";
        }
    }

    public static string MedalSymbol(Medal medal)
    {
        switch (medal)
        {
            case Medal.Gold: return "★ GOLD";
            case Medal.Silver: return "◈ SILVER";
            case Medal.Bronze: return "◆ BRONZE";
            default: return "NONE";
        }
    }

    public static Color MedalColor(Medal medal)
    {
        switch (medal)
        {
            case Medal.Gold: return new Color(1.0f, 0.84f, 0.25f);
            case Medal.Silver: return new Color(0.85f, 0.88f, 0.92f);
            case Medal.Bronze: return new Color(0.85f, 0.55f, 0.35f);
            default: return new Color(0.7f, 0.7f, 0.7f);
        }
    }

    public static string PrefsBestMedalKey(MissionId id)
    {
        return "JemparinganChallenge_" + id + "_BestMedal";
    }

    public static Medal LoadBestMedal(MissionId id)
    {
        return (Medal)PlayerPrefs.GetInt(PrefsBestMedalKey(id), (int)Medal.None);
    }

    public static bool SaveBestMedalIfHigher(MissionId id, Medal medal)
    {
        Medal current = LoadBestMedal(id);
        if (medal > current)
        {
            PlayerPrefs.SetInt(PrefsBestMedalKey(id), (int)medal);
            PlayerPrefs.Save();
            return true;
        }
        return false;
    }
}
