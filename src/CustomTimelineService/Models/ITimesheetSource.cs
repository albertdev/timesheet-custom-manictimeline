using System;
using CustomTimelineService.Models.Dto;
using TimesheetProcessor.Core.Dto;

namespace CustomTimelineService.Models
{
    public interface ITimesheetSource
    {
        public Timesheet FindSheet(DateTime day);
        public DayPlanning CheckPlanning(DateTime day);
    }
}