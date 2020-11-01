using System;
using TimesheetProcessor.Core.Dto;

namespace CustomTimelineService.Models
{
    public interface ITimesheetSource
    {
        public Timesheet FindSheet(DateTime day);
    }
}