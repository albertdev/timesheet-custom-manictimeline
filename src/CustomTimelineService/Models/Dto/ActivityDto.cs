using System;
using System.Xml;
using System.Xml.Serialization;

namespace CustomTimelineService.Models.Dto
{
    /// <summary>
    /// Taken from https://github.com/manictime/custom-timeline-service-demo - the readme specifies that it is allowed to reuse the DTOs.
    /// </summary>
    [Serializable]
    public class ActivityDto
    {
        public string DisplayName { get; set; }
        public string GroupId { get; set; }
        
        [XmlIgnore]
        public DateTimeOffset StartTime { get; set; }

        [XmlElement("StartTime")]
        public string StartTimeTextValue
        {
            get => XmlConvert.ToString(StartTime);
            set => StartTime = XmlConvert.ToDateTimeOffset(value);
        }

        [XmlIgnore]
        public DateTimeOffset EndTime { get; set; }

        [XmlElement("EndTime")]
        public string EndTimeTextValue
        {
            get => XmlConvert.ToString(EndTime);
            set => EndTime = XmlConvert.ToDateTimeOffset(value);
        }
    }
}
