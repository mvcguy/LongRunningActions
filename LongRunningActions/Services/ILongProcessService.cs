using System;
using System.Collections.Generic;

namespace LongRunningActions.Services
{
    public interface ILongProcessService : IDisposable
    {
        /// <summary>
        /// Queue the job(s) for execution. Make sure that the job is not queued already for processing. Otherwise you will end up
        /// running the job multiple times.
        /// </summary>
        /// <param name="longRunningJobs"></param>
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