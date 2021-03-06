﻿using System;
using System.Drawing;

namespace CustomTimelineService.Models
{
    /// <summary>
    /// Taken from https://github.com/manictime/custom-timeline-service-demo.
    /// The readme is not super clear whether this can be reused, but it's annoying if it would not.
    /// </summary>
    public static class HelperFunctions
    {
        public static string GetRandomColor()
        {
            var random = new Random((int)DateTime.Now.Ticks);
            var randomColor = Color.FromArgb(random.Next(0, 255), random.Next(0, 255), random.Next(0, 255));
            return randomColor.R.ToString("X2") + randomColor.G.ToString("X2") + randomColor.B.ToString("X2");
        }

        /// <summary>
        /// Sanitizes the 'from' and 'to' times so that they point to 12:00 AM of that given day.
        ///
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static DateTime TimeToDate(DateTimeOffset input)
        {
            // All that XML Doc for nothing... Still, might be useful as documentation.
            return input.Date;
        }
    }
}
