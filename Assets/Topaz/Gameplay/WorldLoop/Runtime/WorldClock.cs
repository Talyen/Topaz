using System;

namespace Topaz.LoopStudy
{
    /// <summary>Shared units for the saved world clock and elapsed-time rules.</summary>
    public static class WorldClock
    {
        public const double HoursPerDay = 24d;
        public const double StartingHour = 8d;
        public const double RestHours = 8d;
        public const double ActiveSecondsPerDay = 45d * 60d;
        public const double HoursPerActiveSecond = HoursPerDay / ActiveSecondsPerDay;

        public static double HourOfDay(double worldHours) => worldHours % HoursPerDay;

        public static double Advance(double worldHours, double activeSeconds) =>
            worldHours + Math.Max(0d, activeSeconds) * HoursPerActiveSecond;

        public static double RegrowthHours(int days) => Math.Max(1, days) * HoursPerDay;
    }
}
