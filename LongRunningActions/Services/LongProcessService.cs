using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LongRunningActions.Services
{
    public class LongProcessService : ILongProcessService
    {
        private readonly ConcurrentQueue<LongRunningJob> _jobQueue;

        private readonly ConcurrentDictionary<string, Task> _tasks;

        private readonly ConcurrentDictionary<string, LongRunningJob> _jobsHistory;

        private Task _schedularTask;

        private readonly CancellationTokenSource _cancellationTokenSource;

        private readonly CancellationToken _cancellationToken;

        public bool IsSchedularRunning { get; private set; }

        private readonly ILogger<LongProcessService> _logger;

        private readonly LongRunningServiceOptions _options;

        private const string ServiceName = "Long running jobs schedular";
        
        public LongProcessService(ILoggerFactory loggerFactory, IOptions<LongRunningServiceOptions> options)
        {
            _logger = loggerFactory.CreateLogger<LongProcessService>();
            _jobQueue = new ConcurrentQueue<LongRunningJob>();
            _tasks = new ConcurrentDictionary<string, Task>();
            _jobsHistory = new ConcurrentDictionary<string, LongRunningJob>();
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
            _options = options?.Value ?? new LongRunningServiceOptions { MaxNumberOfTasks = 15 };
            if (_options.MaxNumberOfTasks <= 0)
            {
                _options.MaxNumberOfTasks = 15;
            }
        }

        public void QueueJobs(params LongRunningJob[] longRunningJobs)
        {
            foreach (var job in longRunningJobs)
            {
                if (job == null) continue;

                if (string.IsNullOrWhiteSpace(job.JobId))
                {
                    job.JobId = Guid.NewGuid().ToString();
                }
                _jobQueue.Enqueue(job);
                _jobsHistory.TryAdd(job.JobId, job);
            }

        }

        public Task StartSchedularTask(CancellationToken cancellationToken)
        {
            if (IsSchedularRunning) return _schedularTask;

            _schedularTask = Task.Factory.StartNew(async () =>
              {
                  try
                  {
                      cancellationToken.ThrowIfCancellationRequested();
                      while (true)
                      {
                          if (_jobQueue.Count > 0)
                          {
                              try
                              {
                                  OnJobArrived();
                              }
                              catch (Exception e)
                              {
                                  _logger.LogError(e.Message);
                              }
                          }
                          else
                          {
                              //_logger.LogDebug("No longRunningJobs to process, taking a nap...");
                          }

                          //take a nap between iterations
                          cancellationToken.ThrowIfCancellationRequested();
                          await Task.Delay(5000, cancellationToken);
                      }
                  }
                  catch (Exception exception)
                  {
                      if (exception is TaskCanceledException || exception is OperationCanceledException)
                      {
                          _logger.LogWarning(new EventId(401), exception, $"{ServiceName} is cancelled.");
                      }
                      else
                      {
                          _logger.LogCritical(new EventId(500), exception, $"{ServiceName} is stopped due to unexpected error.");
                      }
                  }
                  // ReSharper disable once FunctionNeverReturns
              }, cancellationToken);

            IsSchedularRunning = true;

            _logger.LogInformation($"{ServiceName} is started");

            return _schedularTask;
        }

        public IEnumerable<LongRunningJob> GetJobsInfo(params string[] jobIds)
        {
            foreach (var jobId in jobIds)
            {

                if (_jobsHistory.TryGetValue(jobId, out LongRunningJob job))
                {
                    yield return job;
                }
            }
        }

        public void StartSchedular()
        {
            _logger.LogInformation($"{ServiceName} is being started...");
            StartSchedularTask(_cancellationToken);
        }

        public void StopSchedular()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _schedularTask?.Wait();
            }
            catch (AggregateException aggregateException)
            {
                foreach (var innerException in aggregateException.InnerExceptions)
                    _logger.LogWarning($"{ServiceName} is stopped: " + aggregateException.Message + " " + innerException.Message);
            }
        }

        private void OnJobArrived()
        {
            if (_tasks.Count >= _options.MaxNumberOfTasks)
            {
                _logger.LogWarning("Max number of tasks reached. Task cannot run now, waiting for its turn to be scheduled");
                return;
            }

            if (!_jobQueue.TryDequeue(out LongRunningJob job)) return;

            var task = new Task<string>(() =>
            {
                try
                {
                    job.IsJobStarted = true;
                    job.StartedOn = DateTime.Now;
                    _logger.LogInformation($"Job '{job.JobId}' is started...");
                    job.Execute?.Invoke(job);

                    //invoke success safely
                    try
                    {
                        _logger.LogInformation($"Job '{job.JobId}' is successfully completed...");
                        job.CompletedOn = DateTime.Now;
                        job.Success?.Invoke(job);
                    }
                    catch {/* ignored*/}
                    finally
                    {
                        job.IsJobCompleted = true;
                        job.IsJobCompletedWithError = false;
                    }
                }
                catch (Exception e)
                {
                    //invoke fail safely
                    try
                    {
                        job.CompletedOn = DateTime.Now;
                        job.Fail?.Invoke(e);
                    }
                    catch {/* ignored*/}
                    finally
                    {
                        job.IsJobCompleted = true;
                        job.IsJobCompletedWithError = true;
                        job.Error = e;
                    }
                }
                finally
                {
                    //invoke Always safely
                    try { job.Always?.Invoke(job); }
                    catch {/*ignored*/ }
                }

                return job.JobId;
            });

            task.ContinueWith(task1 =>
            {
                //when task is compelted remove it from the task list.
                // ReSharper disable once UnusedVariable
                _tasks.TryRemove(task1.Result, out Task rrr);
            });

            _tasks.TryAdd(job.JobId, task);
            task.Start();
        }

        protected virtual void ReleaseResources()
        {
            try
            {
                //we can cancel the schedular task while its running.
                //but this does not mean that tasks which are already scheduled are also aborted.
                //they will continue executing even the schedular is down.
                //if schedular is stopped, any jobs queued afterwards will not be scheduled for execution.
                _cancellationTokenSource?.Cancel();
                _schedularTask?.Wait();
            }
            catch (AggregateException aggregateException)
            {
                foreach (var innerException in aggregateException.InnerExceptions)
                    _logger.LogWarning($"GC: {ServiceName}: " + aggregateException.Message + " " + innerException.Message);
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
            }
        }

        public void Dispose()
        {
            ReleaseResources();
            GC.SuppressFinalize(this);
        }

        ~LongProcessService()
        {
            ReleaseResources();
        }
    }
}
