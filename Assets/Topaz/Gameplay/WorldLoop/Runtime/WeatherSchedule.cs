using System;

namespace Topaz.LoopStudy
{
    /// <summary>World-owned ambient weather derived from stable world identity and saved time.</summary>
    public static class WeatherSchedule
    {
        public const string Clear = "clear";
        public const string Cloudy = "cloudy";
        public const string Rain = "rain";
        const double HoursPerPeriod = 8d;

        public static bool IsValid(string condition) =>
            condition == Clear || condition == Cloudy || condition == Rain;

        public static string Resolve(string ambient, string regionOverride, string eventOverride)
        {
            if (IsValid(eventOverride)) return eventOverride;
            if (IsValid(regionOverride)) return regionOverride;
            return IsValid(ambient) ? ambient : Clear;
        }

        public static string At(string worldId, double worldHours)
        {
            if (string.IsNullOrEmpty(worldId) || double.IsNaN(worldHours) ||
                double.IsInfinity(worldHours) || worldHours < WorldClock.StartingHour)
                throw new ArgumentOutOfRangeException(nameof(worldHours));

            // Every new world opens in clear daylight. Afterwards the world's stable ID
            // offsets its eight-hour weather boundaries so worlds do not change in lockstep.
            if (worldHours < WorldClock.StartingHour + HoursPerPeriod) return Clear;
            uint worldHash = Hash(worldId);
            long period = (long)Math.Floor((worldHours + worldHash % 8u) / HoursPerPeriod);
            uint roll = Mix(worldHash, (ulong)period) % 100u;
            return roll < 47u ? Clear : roll < 80u ? Cloudy : Rain;
        }

        public static double NextBoundary(string worldId, double worldHours)
        {
            if (string.IsNullOrEmpty(worldId) || double.IsNaN(worldHours) ||
                double.IsInfinity(worldHours) || worldHours < WorldClock.StartingHour)
                throw new ArgumentOutOfRangeException(nameof(worldHours));
            if (worldHours < WorldClock.StartingHour + HoursPerPeriod)
                return WorldClock.StartingHour + HoursPerPeriod;
            uint offset = Hash(worldId) % 8u;
            return (Math.Floor((worldHours + offset) / HoursPerPeriod) + 1d) *
                HoursPerPeriod - offset;
        }

        static uint Hash(string value)
        {
            uint hash = 2166136261u;
            foreach (char c in value)
            {
                hash ^= c;
                hash *= 16777619u;
            }
            return hash;
        }

        static uint Mix(uint worldHash, ulong period)
        {
            ulong value = ((ulong)worldHash << 32) | worldHash;
            value ^= period + 0x9e3779b97f4a7c15UL;
            value ^= value >> 30;
            value *= 0xbf58476d1ce4e5b9UL;
            value ^= value >> 27;
            value *= 0x94d049bb133111ebUL;
            value ^= value >> 31;
            return (uint)value;
        }
    }
}
