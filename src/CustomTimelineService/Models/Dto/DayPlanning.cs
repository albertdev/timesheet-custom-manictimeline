using System;

namespace CustomTimelineService.Models.Dto
{
    public class DayPlanning
    {
        public TimeSpan StartOfMorningQuietHours => TimeSpan.FromHours(7);
        public TimeSpan EndOfMorningQuietHours { get; set; }
        public TimeSpan StartOfEveningQuietHours { get; set; }
        // 7 AM next morning
        public TimeSpan EndOfEveningQuietHours => TimeSpan.FromHours(31);

        public static DayPlanning Default =>
            new DayPlanning
            {
                EndOfMorningQuietHours = TimeSpan.FromHours(10),
                // 3 AM next morning
                StartOfEveningQuietHours = TimeSpan.FromHours(27),
            };
    }
}