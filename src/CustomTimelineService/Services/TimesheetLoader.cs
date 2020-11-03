using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CustomTimelineService.Models;
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

        public TimesheetLoader(string loadPath, ILogger<TimesheetLoader> logger)
        {
            _loadPath = loadPath;
            _logger = logger;
            _timesheets = new SortedDictionary<DateTime, Timesheet>();
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

        public void LoadTimesheets(CancellationToken cancellationToken = new CancellationToken())
        {
            lock (this)
            {
                var files = Directory.EnumerateFiles(_loadPath).ToArray();
                if (files.Length == 0)
                {
                    throw new Exception("No input timesheet files found");
                }

                var reader = new ManicTimeParser(true);

                foreach (var file in files)
                {
                    try
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
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"File {file} could not be parsed");
                    }
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