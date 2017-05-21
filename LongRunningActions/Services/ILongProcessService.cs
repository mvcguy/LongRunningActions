using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LongRunningActions.Services
{
    public interface ILongProcessService : IDisposable
    {
        void QueueJobs(params LongRunningJob[] longRunningJobs);

        Task StartSchedularTask(CancellationToken cancellationToken);

        IEnumerable<LongRunningJob> GetJobsInfo(params string[] jobIds);
    }
}