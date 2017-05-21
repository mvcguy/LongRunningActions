using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using LongRunningActions.Models.Dtos;
using LongRunningActions.Services;

namespace LongRunningActions.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILongProcessService _longProcessService;
        private readonly ILogger<HomeController> _logger;

        private const string PizzaJobKey = "CookPizza";
        private const string BookFlightJobKey = "BookFlight";

        public HomeController(ILoggerFactory loggerFactory, ILongProcessService longProcessService)
        {
            _logger = loggerFactory.CreateLogger<HomeController>();
            _longProcessService = longProcessService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult CookPizza()
        {

            if (HttpContext.Session.Keys.Any(x => x == PizzaJobKey))
            {
                var jobs = GetJobsFromSession(PizzaJobKey);

                var incomplete = jobs.Any(x => !x.IsJobCompleted);

                if (incomplete)
                {
                    return Json(new { Status = Constants.JobInProgress, Message = Messages.JobsInProgress });
                }
                else
                {
                    HttpContext.Session.Remove(PizzaJobKey);
                }
            }

            var job = PizzaJob;

            UpdateJobStatus(PizzaJobKey, new[] { job.JobId });

            _longProcessService.QueueJobs(job);

            return Json(new { Status = Constants.JobQueuedForProcessing, Message = Messages.JobsQueued });
        }

        public IActionResult BookFlight()
        {
            if (HttpContext.Session.Keys.Any(x => x == BookFlightJobKey))
            {
                var jobs = GetJobsFromSession(BookFlightJobKey);

                var incomplete = jobs.Any(x => !x.IsJobCompleted);

                if (incomplete)
                {
                    return Json(new { Status = Constants.JobInProgress, Message = Messages.JobsInProgress });
                }
                else
                {
                    HttpContext.Session.Remove(BookFlightJobKey);
                }
            }

            var job = BookFlightJob;

            UpdateJobStatus(BookFlightJobKey, new[] { job.JobId });

            _longProcessService.QueueJobs(job);

            return Json(new { Status = Constants.JobQueuedForProcessing, Message = Messages.JobsQueued});
        }

        public IActionResult PollJobStatus()
        {
            if (!HttpContext.Session.Keys.Any(x => x == PizzaJobKey || x == BookFlightJobKey))
            {
                return Json(new { Status = Constants.JobsNotFound, Message = Messages.NoJobsFound, result = new object[] { } });
            }

            var jobs = new List<LongRunningJobDto>();

            jobs.AddRange(GetJobsFromSession(PizzaJobKey).ToList());
            jobs.AddRange(GetJobsFromSession(BookFlightJobKey).ToList());

            var status = jobs.All(x => x.IsJobCompleted) ? Constants.AllJobsCompleted : Constants.JobInProgress;

            return Json(new { Status = status, Message = "", result = jobs });
        }

        private void UpdateJobStatus(string key, IEnumerable<string> value)
        {
            HttpContext.Session.Set(key, value);
        }

        private IEnumerable<LongRunningJobDto> GetJobsFromSession(string key)
        {
            var jobs = HttpContext.Session.Get<IEnumerable<string>>(key);

            if (jobs == null) yield break;

            var jobsWithUpdatedStatus = _longProcessService.GetJobsInfo(jobs.ToArray()).ToList();

            foreach (var job in jobsWithUpdatedStatus)
            {
                yield return new LongRunningJobDto
                {
                    JobId = job.JobId,
                    ClientJobId = job.ClientJobId,
                    Name = job.Name,
                    IsJobCompleted = job.IsJobCompleted,
                    IsJobCompletedWithError = job.IsJobCompletedWithError,
                    Error = job.Error?.Message,
                    IsJobStarted = job.IsJobStarted,
                    CreatedOn = job.CreatedOn,
                    CompletedOn = job.CompletedOn,
                    LastUpdatedOn = job.LastUpdatedOn,
                    StartedOn = job.StartedOn
                };
            }
        }
        
        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }

        public LongRunningJob PizzaJob
        {
            get
            {
                return new LongRunningJob()
                {
                    ClientJobId = PizzaJobKey,
                    Name = "Cook Pizza",
                    Execute = (job) =>
                    {
                        _logger.LogInformation("Cooking pizza...");
                        Thread.Sleep(new TimeSpan(0, 0, 10));
                    },
                    Success = (job) =>
                    {
                        _logger.LogInformation($"Pizza is cooked with token Id: {job.JobId}");
                    }
                };
            }
        }

        public LongRunningJob BookFlightJob
        {
            get
            {
                return new LongRunningJob()
                {
                    ClientJobId = BookFlightJobKey,
                    Name = "Book Flight",
                    Execute = (job) =>
                    {
                        _logger.LogInformation("Booking Flight...");
                        Thread.Sleep(new TimeSpan(0, 0, 10));
                    },
                    Success = (job) =>
                    {
                        _logger.LogInformation($"Flight is booked with token Id: {job.JobId}");
                    }
                };
            }
        }
    }

    public static class SessionExtensions
    {
        public static void Set<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonConvert.SerializeObject(value));
        }

        public static T Get<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default(T) :
                JsonConvert.DeserializeObject<T>(value);
        }
    }
}

