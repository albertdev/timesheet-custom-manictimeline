using System;
using System.Xml.Serialization;

namespace CustomTimelineService.Models.Dto
{
    /// <summary>
    /// Taken from https://github.com/manictime/custom-timeline-service-demo - the readme specifies that it is allowed to reuse the DTOs.
    /// </summary>
    [Serializable]
    [XmlRoot("Timeline")]
    public class TimelineDto
    {
        public string Color { get; set; }

        public string DisplayName { get; set; }

        [XmlArrayItem("Activity")]
        public ActivityDto[] Activities { get; set; }

        [XmlArrayItem("Group")]
        public GroupDto[] Groups { get; set; }
    }
}