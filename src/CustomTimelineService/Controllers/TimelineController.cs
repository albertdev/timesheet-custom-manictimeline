using System;
using System.Collections.Generic;
using System.Linq;
using CustomTimelineService.Models;
using CustomTimelineService.Models.Dto;
using CustomTimelineService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TimesheetProcessor.Core;
using TimesheetProcessor.Core.Dto;

namespace CustomTimelineService.Controllers
{
    [ApiController]
    [Route("timeline")]
    public class TimelineController : ControllerBase
    {
        private readonly ITimesheetSource _timesheetSource;
        private readonly ILogger<TimelineController> _logger;

        public TimelineController(ITimesheetSource timesheetSource, ILogger<TimelineController> logger)
        {
            _timesheetSource = timesheetSource;
            _logger = logger;
        }

        [HttpGet]
        public TimelineDto GetActivities(DateTimeOffset? fromTime, DateTimeOffset? toTime)
        {
            var timeline = new TimelineDto();
            if (fromTime == null || toTime == null)
            {
                var message = "No time query parameters passed.";
                _logger.LogWarning(message);
                timeline.DisplayName = message;
                return timeline;
            }

            _logger.LogInformation("Retrieving From time [{0}] and To Time [{1}]", fromTime, toTime);

            /* ManicTime normally records days from 12:00 AM to 11:59 PM, but if you never work that early and frequently cross midnight then it's useful
             * to use the "Shift day start by (hours)" setting. However, this means that the timeline service now gets requests "from start day + shifted
             * time, to end day + shifted time". This custom timeline service is day-based so this function simply drops the time component here.
             */
            fromTime = fromTime.Value.Date;
            //  toTime is the day which is no longer included, so we do -1 to get the last included day
            toTime = toTime.Value.Date.AddDays(-1);

            // check that both fromTime and toTime are in same week
            if (fromTime.Value.Date.GetIso8601WeekOfYear() != toTime.Value.Date.GetIso8601WeekOfYear())
            {
                var message = $"From time [{fromTime}] and To Time [{toTime}] are in different weeks";
                _logger.LogWarning(message);
                timeline.DisplayName = message;
                return timeline;
            }

            var timesheet = _timesheetSource.FindSheet(fromTime.Value.Date);

            if (timesheet == null)
            {
                var message = $"From time [{fromTime}] and To Time [{toTime}] did not match a timesheet";
                _logger.LogInformation(message);
                timeline.DisplayName = message;
                return timeline;
            }

            timeline.Color = HelperFunctions.GetRandomColor();
            ConvertTimesheetToActivities(timesheet, timeline, fromTime.Value.Date, toTime.Value.Date);

            return timeline;
        }

        private void ConvertTimesheetToActivities(Timesheet timesheet, TimelineDto timeline, in DateTime fromDay, in DateTime toDay)
        {
            timeline.Groups = ConvertTimesheetTagsToGroups(timesheet);
            var activities = new List<ActivityDto>();
            IDictionary<string, string> tagIdToGroupId = timeline.Groups.ToDictionary(x => x.DisplayName, x => x.GroupId);
            foreach (var dayEntry in timesheet.Days)
            {
                if (dayEntry.Day < fromDay)
                {
                    continue;
                }
                if (dayEntry.Day > toDay)
                {
                    break;
                }

                var planning = _timesheetSource.CheckPlanning(dayEntry.Day);
                var planner = new ActivityPlanner(dayEntry.Day, planning, tagIdToGroupId);
                planner.PlanTimeEntries(dayEntry.Entries);
                activities.AddRange(planner.ConvertedActivities);
            }

            timeline.Activities = activities.ToArray();
        }

        private GroupDto[] ConvertTimesheetTagsToGroups(Timesheet timesheet)
        {
            var result = new List<GroupDto>();
            var counter = 1;
            foreach (var tagEntry in timesheet.Tags)
            {
                result.Add(new GroupDto
                {
                    Color = HelperFunctions.GetRandomColor(),
                    DisplayName = tagEntry.TagId,
                    DisplayKey = tagEntry.TagId.Replace(",", "").Replace(" ", ""),
                    GroupId = counter.ToString()
                });
                counter++;
            }

            return result.ToArray();
        }
    }
}