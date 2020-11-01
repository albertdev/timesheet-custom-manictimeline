using System;

namespace CustomTimelineService.Models.Dto
{
    /// <summary>
    /// Taken from https://github.com/manictime/custom-timeline-service-demo - the readme specifies that it is allowed to reuse the DTOs.
    /// </summary>
    [Serializable]
    public class GroupDto
    {
        public string GroupId { get; set; }
        public string DisplayName { get; set; }
        public string Color { get; set; }
        public string DisplayKey { get; set; }
    }
}