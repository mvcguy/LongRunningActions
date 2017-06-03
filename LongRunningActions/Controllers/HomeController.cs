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
            object result;

            var jobs = GetJobsFromSession(PizzaJobKey).ToList();
            var previousJobCompleted = jobs.All(x => x.IsJobCompleted || x.IsJobCancelled);

            if (previousJobCompleted)
            {
                RemoveJobFromSession(PizzaJobKey);
                var job = PizzaJob;
                UpdateJobInSession(PizzaJobKey, new[] { job.JobId });
                _longProcessService.QueueJobs(job);

                result = new
                {
                    Status = Constants.JobQueuedForProcessing,
                    Message = Messages.JobsQueued,
                    Jobs = new object[] { ToLongRunningJobDto(job) }
                };
            }
            else
            {
                result = new
                {
                    Status = Constants.JobInProgress,
                    Message = Messages.JobsInProgress,
                    Jobs = jobs
                };

            }
            return Json(result);
        }

        public IActionResult BookFlight()
        {
            object result;

            var jobs = GetJobsFromSession(BookFlightJobKey).ToList();
            var previousJobCompleted = jobs.All(x => x.IsJobCompleted || x.IsJobCancelled);

            if (previousJobCompleted)
            {
                RemoveJobFromSession(BookFlightJobKey);
                var job = BookFlightJob;
                UpdateJobInSession(BookFlightJobKey, new[] { job.JobId });
                _longProcessService.QueueJobs(job);
                result = new
                {
                    Status = Constants.JobQueuedForProcessing,
                    Message = Messages.JobsQueued,
                    Jobs = new object[] { ToLongRunningJobDto(job) }
                };
            }
            else
            {
                result = new
                {
                    Status = Constants.JobInProgress,
                    Message = Messages.JobsInProgress,
                    Jobs = jobs
                };

            }
            return Json(result);
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

            var status = jobs.All(x => x.IsJobCompleted || x.IsJobCancelled) ? Constants.AllJobsCompleted : Constants.JobInProgress;

            return Json(new { Status = status, Message = "", result = jobs });
        }

        public IActionResult CancelJob(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId) || !Guid.TryParse(jobId, out Guid jobGuid))
            {
                return Json(new { Status = Constants.JobsNotFound, Message = Messages.NoJobsFound, result = new object[] { } });
            }

            var cancellationResult = ToCancellationResultDto(_longProcessService.CancelJob(jobGuid.ToString()));

            var status = cancellationResult.Job != null ? Constants.JobFound : Constants.JobsNotFound;

            return Json(new { Status = status, Message = cancellationResult.Message, result = new object[] { cancellationResult } });
        }

        private void UpdateJobInSession(string clientJobId, IEnumerable<string> value)
        {
            HttpContext.Session.Set(clientJobId, value);
        }

        private void RemoveJobFromSession(string clientJobId)
        {
            HttpContext.Session.Remove(clientJobId);
        }

        private IEnumerable<LongRunningJobDto> GetJobsFromSession(string clientJobId)
        {
            var jobs = HttpContext.Session.Get<IEnumerable<string>>(clientJobId);

            if (jobs == null) yield break;

            var jobsWithUpdatedStatus = _longProcessService.GetJobsInfo(jobs.ToArray()).ToList();

            foreach (var job in jobsWithUpdatedStatus)
            {
                yield return ToLongRunningJobDto(job);
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

        private LongRunningJob PizzaJob
        {
            get
            {
                return new LongRunningJob()
                {
                    ClientJobId = PizzaJobKey,
                    Name = "Cook Pizza",
                    Execute = (job, cancellationToken) =>
                    {
                        _logger.LogInformation("Preparing pizza...");
                        cancellationToken.ThrowIfCancellationRequested();
                        Thread.Sleep(new TimeSpan(0, 0, 10));

                        _logger.LogInformation("Cooking pizza...");
                        cancellationToken.ThrowIfCancellationRequested();
                        Thread.Sleep(new TimeSpan(0, 0, 10));

                    },
                    Success = (job, cancellationToken) =>
                    {
                        _logger.LogInformation($"Pizza is cooked with token Id: {job.JobId}");
                    }
                };
            }
        }

        private LongRunningJob BookFlightJob
        {
            get
            {
                return new LongRunningJob()
                {
                    ClientJobId = BookFlightJobKey,
                    Name = "Book Flight",
                    Execute = (job, cancellationToken) =>
                    {
                        _logger.LogInformation("Connecting to portal...");
                        cancellationToken.ThrowIfCancellationRequested();
                        Thread.Sleep(new TimeSpan(0, 0, 10));

                        _logger.LogInformation("Booking flight...");
                        cancellationToken.ThrowIfCancellationRequested();
                        Thread.Sleep(new TimeSpan(0, 0, 20));
                    },
                    Success = (job, cancellationToken) =>
                    {
                        _logger.LogInformation($"Flight is booked with token Id: {job.JobId}");
                    }
                };
            }
        }

        private LongRunningJobDto ToLongRunningJobDto(LongRunningJob job)
        {
            if (job == null) return null;

            return new LongRunningJobDto
            {
                JobId = job.JobId,
                ClientJobId = job.ClientJobId,
                Name = job.Name,
                IsJobCompleted = job.IsJobCompleted,
                IsJobCompletedWithError = job.IsJobCompletedWithError,
                IsJobCancelled = job.IsJobCancelled,
                CancellationRequested = job.CancellationRequested,
                Error = job.Error?.Message,
                IsJobStarted = job.IsJobStarted,
                CreatedOn = job.CreatedOn,
                CompletedOn = job.CompletedOn,
                LastUpdatedOn = job.LastUpdatedOn,
                StartedOn = job.StartedOn
            };
        }

        private CancellationResultDto ToCancellationResultDto(JobCancellationResult cancellationResult)
        {
            return new CancellationResultDto
            {
                Job = ToLongRunningJobDto(cancellationResult.Job),
                JobCancellationStatus = cancellationResult.JobCancellationStatus.ToString(),
                JobId = cancellationResult.JobId,
                Message = cancellationResult.Message
            };
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

