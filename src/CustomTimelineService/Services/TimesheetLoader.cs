using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using CustomTimelineService.Models;
using CustomTimelineService.Models.Dto;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TimesheetProcessor.Core.Dto;
using TimesheetProcessor.Core.Io;

namespace CustomTimelineService.Services
{
    public class TimesheetLoader : ITimesheetSource, IHostedService
    {
        private readonly string _loadPath;
        private readonly ILogger<TimesheetLoader> _logger;
        private readonly SortedDictionary<DateTime, Timesheet> _timesheets;
        private readonly SortedDictionary<DateTime, DayPlanning> _dayPlannings;

        public TimesheetLoader(string loadPath, ILogger<TimesheetLoader> logger)
        {
            _loadPath = loadPath;
            _logger = logger;
            _timesheets = new SortedDictionary<DateTime, Timesheet>();
            _dayPlannings = new SortedDictionary<DateTime, DayPlanning>();
        }

        public Timesheet FindSheet(DateTime day)
        {
            lock (this)
            {
                if (_timesheets.Count <= 0)
                {
                    throw new Exception("No timesheets have been loaded.");
                }

                if (! _timesheets.ContainsKey(day.Date))
                {
                    return null;
                }
                return _timesheets[day.Date];
            }
        }

        public DayPlanning CheckPlanning(DateTime day)
        {
            if (! _dayPlannings.ContainsKey(day))
            {
                return DayPlanning.Default;
            }

            return _dayPlannings[day.Date];
        }

        public void LoadTimesheets(CancellationToken cancellationToken = new CancellationToken())
        {
            lock (this)
            {
                var files = Directory.EnumerateFiles(_loadPath).ToArray();
                if (files.Length == 0)
                {
                    throw new Exception("No input timesheet files found");
                }

                var timesheetReader = new ManicTimeParser(true);

                foreach (var file in files)
                {
                    try
                    {
                        if (Regex.IsMatch(file, "__\\w+\\.tsv$"))
                        {
                            // File which needs to be ignored
                            continue;
                        }
                        else if (file.EndsWith("_planning.tsv"))
                        {
                            ParseDailyPlanning(file);
                        }
                        else if (file.EndsWith(".tsv"))
                        {
                            ParseDiffTimesheet(file, timesheetReader);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"File {file} could not be parsed");
                    }
                }
            }
        }

        /// <summary>
        /// Read '2020-wXX_planning.tsv' file.
        /// This TSV file has no header row, instead it contains 3 columns: day, end of morning quiet hours and start of evening quiet hours.
        /// </summary>
        /// <param name="file"></param>
        private void ParseDailyPlanning(string file)
        {
            CsvConfiguration csvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = "\t",
                HasHeaderRecord = false
            };
            using (var streamReader = new StreamReader(file))
            using (CsvReader csv = new CsvReader(streamReader, csvConfiguration))
            {
                while (csv.Read())
                {
                    try
                    {
                        var day = DateTime.Parse(csv[0]);
                        var endOfMorning = TimeSpan.Parse(csv[1]);
                        var startOfEvening = TimeSpan.Parse(csv[2]);

                        // Quiet hours might start next day in the morning.
                        // When we get 00:00:00 or 03:00:00, add 24 hours to make sure that the timespan goes into the next day
                        if (startOfEvening.Hours < endOfMorning.Hours)
                        {
                            startOfEvening = startOfEvening.Add(TimeSpan.FromHours(24));
                        }

                        _dayPlannings[day] = new DayPlanning
                        {
                            EndOfMorningQuietHours = endOfMorning,
                            StartOfEveningQuietHours = startOfEvening
                        };
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Problem parsing row {csv.Context.Row}", e);
                    }
                }
            }
        }

        private void ParseDiffTimesheet(string file, ManicTimeParser reader)
        {
            using (var streamReader = new StreamReader(file))
            {
                var timesheet = reader.ParseTimesheet(streamReader);
                foreach (var day in timesheet.Days)
                {
                    _timesheets[day.Day] = timesheet;
                }
            }
        }

        Task IHostedService.StartAsync(CancellationToken cancellationToken)
        {
            LoadTimesheets(cancellationToken);
            return Task.CompletedTask;
        }

        Task IHostedService.StopAsync(CancellationToken cancellationToken)
        {
            // No actions on stop
            return Task.CompletedTask;
        }
    }
}