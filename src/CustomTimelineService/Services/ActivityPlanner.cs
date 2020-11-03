using System;
using System.Collections.Generic;
using System.Linq;
using CustomTimelineService.Models.Dto;
using TimesheetProcessor.Core.Dto;

namespace CustomTimelineService.Services
{
    /// <summary>
    /// This class takes a time entry from a timesheet and creates a single activity or set of activities during a range of "quiet" hours in the day.
    /// A new instance should be created per day.
    /// </summary>
    public class ActivityPlanner
    {
        private readonly DateTime _day;
        private readonly IDictionary<string, string> _tagIdToGroupId;
        private readonly List<ActivityDto> _activities = new List<ActivityDto>();

        public ActivityPlanner(DateTime day, IDictionary<string,string> tagIdToGroupId)
        {
            _day = day;
            _tagIdToGroupId = tagIdToGroupId;
        }

        public IEnumerable<ActivityDto> ConvertedActivities => _activities;

        public void PlanTimeEntries(IEnumerable<TimeEntry> entries)
        {
            // Sort time entries from largest to smallest.
            var timeEntries = entries.ToList();
            timeEntries.Sort((x, y) => y.TimeSpent.CompareTo(x.TimeSpent));

            // Fit in 'activities' from 07:00 to 10:00. If that doesn't fit everything in, try from 03:00 to 07:00
            var startHour = _day.Date.Add(new TimeSpan(7,0,0));
            foreach (var timeEntry in timeEntries)
            {
                if (timeEntry.TimeSpent.Ticks < TimeSpan.TicksPerSecond)
                {
                    continue;
                }
                var endHour = startHour.Add(timeEntry.TimeSpent);
                if (endHour.TimeOfDay > new TimeSpan(10, 0, 0))
                {
                    startHour = _day.Date.Add(new TimeSpan(27, 0, 0));
                    endHour = startHour.Add(timeEntry.TimeSpent);
                }

                var activity = new ActivityDto
                {
                    DisplayName = timeEntry.Tag.Notes,
                    StartTime = startHour,
                    EndTime = endHour,
                    GroupId = _tagIdToGroupId[timeEntry.Tag.TagId]
                };
                _activities.Add(activity);
                startHour = endHour;
            }
        }
    }
}