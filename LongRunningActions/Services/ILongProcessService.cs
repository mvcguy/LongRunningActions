using System;
using System.Collections.Generic;

namespace LongRunningActions.Services
{
    public interface ILongProcessService : IDisposable
    {
        void QueueJobs(params LongRunningJob[] longRunningJobs);

        IEnumerable<LongRunningJob> GetJobsInfo(params string[] jobIds);

        void StartSchedular();


        /// <summary>
        /// This will stop the schedular task, any jobs queued afterwards will not be scheduled for execution.
        /// but this does not mean that tasks which are already scheduled are also aborted.
        /// they will continue executing even the schedular is down.
        /// </summary>
        void StopSchedular();

        JobCancellationResult CancelJob(string jobGuid);
    }
}