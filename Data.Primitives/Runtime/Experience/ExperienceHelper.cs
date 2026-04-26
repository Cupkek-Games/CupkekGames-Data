using System;

namespace CupkekGames.Data.Primitives
{
    public static class ExperienceHelper
    {
        public const int MaxLevel = 90;

        public static int GetLevel(int totalExp, Func<int, int> requiredExperience)
        {
            if (totalExp < 0) return 0;

            int experience = 0;
            for (int i = 1; i < MaxLevel; i++)
            {
                experience += requiredExperience(i);
                if (totalExp < experience) return i;
            }
            return MaxLevel;
        }

        public static int GetTotalRequiredExperience(int level, Func<int, int> requiredExperience)
        {
            int experience = 0;
            for (int i = 1; i < level; i++)
            {
                experience += requiredExperience(i);
            }
            return experience;
        }

        public static int GetCurrentExperience(int totalExp, Func<int, int> requiredExperience)
        {
            int level = GetLevel(totalExp, requiredExperience);
            return GetCurrentExperience(totalExp, level, requiredExperience);
        }

        public static int GetCurrentExperience(int totalExp, int level, Func<int, int> requiredExperience)
        {
            int totalExpForLevel = GetTotalRequiredExperience(level, requiredExperience);
            return totalExp - totalExpForLevel;
        }

        public static int GetCurrentRequiredExperience(int level, Func<int, int> requiredExperience)
        {
            return requiredExperience(level);
        }

        public static int GetRemainingExperience(int totalExp, Func<int, int> requiredExperience)
        {
            int level = GetLevel(totalExp, requiredExperience);
            return GetRemainingExperience(totalExp, level, requiredExperience);
        }

        public static int GetRemainingExperience(int totalExp, int level, Func<int, int> requiredExperience)
        {
            return requiredExperience(level) - GetCurrentExperience(totalExp, level, requiredExperience);
        }

        public static float GetExpPercent(int totalExp, Func<int, int> requiredExperience)
        {
            int level = GetLevel(totalExp, requiredExperience);
            return GetExpPercent(totalExp, level, requiredExperience);
        }

        public static float GetExpPercent(int totalExp, int level, Func<int, int> requiredExperience)
        {
            return (float)GetCurrentExperience(totalExp, level, requiredExperience) / requiredExperience(level);
        }

        public static bool CanLevelUp(int totalExp, int level, Func<int, int> requiredExperience)
        {
            return totalExp >= GetTotalRequiredExperience(level + 1, requiredExperience);
        }
    }
}
