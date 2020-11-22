using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly TimeSpan _morningStart;
        private readonly TimeSpan _eveningStart;
        private readonly DateTime _day;
        private readonly IDictionary<string, string> _tagIdToGroupId;
        private readonly List<ActivityDto> _activities = new List<ActivityDto>();
        private readonly LinkedList<ActivityDto> _morningActivities = new LinkedList<ActivityDto>();
        private readonly LinkedList<ActivityDto> _eveningActivities = new LinkedList<ActivityDto>();
        private TimeSpan _morningTimeFree;
        private TimeSpan _eveningTimeFree;

        public ActivityPlanner(DateTime day, DayPlanning planning, IDictionary<string,string> tagIdToGroupId)
        {
            _day = day;
            _tagIdToGroupId = tagIdToGroupId;

            _morningStart = planning.StartOfMorningQuietHours;
            var morningEnd = planning.EndOfMorningQuietHours;
            _eveningStart = planning.StartOfEveningQuietHours;
            var eveningEnd = planning.EndOfEveningQuietHours;

            _eveningTimeFree = eveningEnd - _eveningStart;
            _morningTimeFree = morningEnd - _morningStart;
        }

        public IEnumerable<ActivityDto> ConvertedActivities => _morningActivities.Concat(_eveningActivities).ToList();

        public void PlanTimeEntries(IEnumerable<TimeEntry> entries)
        {
            // Sort time entries from largest to smallest.
            var timeEntries = entries.ToList();
            timeEntries.Sort((x, y) => y.TimeSpent.CompareTo(x.TimeSpent));

            // Fit in 'activities' from 07:00 to 10:00. If that doesn't fit everything in, try from 03:00 to 07:00
            foreach (var timeEntry in timeEntries)
            {
                var duration = timeEntry.TimeSpent;
                // Ignore negative (?), zero or 1-second time entries
                if (duration.Ticks <= TimeSpan.TicksPerSecond)
                {
                    continue;
                }

                if (duration < _morningTimeFree)
                {
                    ScheduleInMorning(timeEntry, duration);
                } else if (duration < _eveningTimeFree)
                {
                    ScheduleInEvening(timeEntry, duration);
                }
                else if (duration < (_morningTimeFree + _eveningTimeFree))
                {
                    // Split the activity into two bits
                    var firstHalf = _morningTimeFree;
                    ScheduleInMorning(timeEntry, firstHalf);
                    ScheduleInEvening(timeEntry, duration - firstHalf);
                }
                else if (_eveningTimeFree.Ticks > 0)
                {
                    // Evening timeslot is going to be full. Split the time into two bits: one that fits, and one to be scheduled wherever
                    var firstHalf = _eveningTimeFree;
                    ScheduleInEvening(timeEntry, firstHalf);
                    ScheduleInEvening(timeEntry, duration - firstHalf);
                }
                else
                {
                    // Both timeslots are now filled... Just keep adding stuff to the evening bit, it should move into time before 3 AM
                    ScheduleInEvening(timeEntry, duration);
                }
            }
        }

        private void ScheduleInMorning(TimeEntry timeEntry, TimeSpan duration)
        {
            // Use previous activity's end time, if any
            var startHour = _morningActivities.Last?.Value?.EndTime ?? _day.Add(_morningStart);
            var endHour = startHour.Add(duration);

            var activity = new ActivityDto
            {
                DisplayName = timeEntry.Tag.Notes,
                StartTime = startHour,
                EndTime = endHour,
                GroupId = _tagIdToGroupId[timeEntry.Tag.TagId]
            };
            _morningActivities.AddLast(activity);

            _morningTimeFree -= duration;
        }

        private void ScheduleInEvening(TimeEntry timeEntry, TimeSpan duration)
        {
            // Use previous activity's end time, if any
            var startHour = _eveningActivities.Last?.Value?.EndTime ?? _day.Add(_eveningStart);
            var endHour = startHour.Add(duration);

            // Evening time slot is filled. Move stuff in front of it, but now logic works in reverse
            if (_eveningTimeFree.Ticks <= 0)
            {
                Debug.Assert(_eveningActivities.First != null, "_eveningActivities.First != null"); // Only should get here if activities list is filled
                endHour = _eveningActivities.First.Value.StartTime;
                startHour = endHour.Subtract(duration);
            }

            var activity = new ActivityDto
            {
                DisplayName = timeEntry.Tag.Notes,
                StartTime = startHour,
                EndTime = endHour,
                GroupId = _tagIdToGroupId[timeEntry.Tag.TagId]
            };
            // Keep linked list in chronological order by inserting in front
            if (_eveningTimeFree.Ticks <= 0)
            {
                _eveningActivities.AddFirst(activity);
            }
            else
            {
                _eveningActivities.AddLast(activity);
            }
            _eveningTimeFree -= duration;
        }
    }
}