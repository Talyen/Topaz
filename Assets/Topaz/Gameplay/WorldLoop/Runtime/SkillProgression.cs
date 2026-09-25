using System;
using System.Collections.Generic;
using UnityEngine;

namespace Topaz.LoopStudy
{
    public static class SkillIds
    {
        public const string Swords = "swords";
        public const string Axes = "axes";
        public const string Shield = "shield";
        public const string Logging = "logging";
        public const string Mining = "mining";
        public const string Staff = "staff";
        public const string Crossbows = "crossbows";
        public static readonly string[] All =
            { Swords, Axes, Shield, Logging, Mining, Staff, Crossbows };
    }

    [Serializable]
    public sealed class SkillProgressRecord
    {
        public string skillId;
        public int experienceCenti;
        public List<string> learnedTalentIds = new List<string>();
        public List<string> activeTalentIds = new List<string>();

        public int Level => SkillProgression.Level(experienceCenti);
        public int AvailableChoices => Mathf.Max(0,
            (Level >= 2 ? 1 : 0) + (Level >= 5 ? 1 : 0) + (Level >= 8 ? 1 : 0)
            - learnedTalentIds.Count);
    }

    [Serializable]
    public sealed class TalentDefinition
    {
        public string id;
        public string label;
        [TextArea(2, 4)] public string description;
        public float amount;
        public float durationSeconds;
        public float intervalSeconds;
    }

    public static class SkillProgression
    {
        // Hundredths retain a 0.25 XP reward without per-event rounding loss.
        public static readonly int[] Thresholds =
            { 0, 1000, 2200, 3600, 5200, 7000, 9000, 11200, 13600, 16200 };

        public static int Level(int centiXp)
        {
            for (int i = Thresholds.Length - 1; i > 0; i--)
                if (centiXp >= Thresholds[i]) return i + 1;
            return 1;
        }

        public static int Award(int baseCentiXp, int skillLevel, int sourceLevel)
        {
            if (baseCentiXp <= 0 || skillLevel >= 10 || sourceLevel < 1) return 0;
            int difference = sourceLevel - skillLevel;
            int percent = difference >= 0
                ? Mathf.Min(125, 100 + difference * 5)
                : Mathf.Max(0, 100 + difference * 25);
            return Mathf.RoundToInt(baseCentiXp * percent / 100f);
        }

        public static int FromLegacy(int oldExperience)
        {
            int oldLevel = 1 + oldExperience / 10;
            if (oldLevel >= 10)
                return Thresholds[9] + Mathf.Max(0, oldExperience - 90) * 100;
            int lower = Thresholds[oldLevel - 1];
            int upper = Thresholds[oldLevel];
            return lower + (upper - lower) * (oldExperience % 10) / 10;
        }

    }
}
