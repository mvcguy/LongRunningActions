using System;
using System.Threading;

namespace LongRunningActions.Services
{
    public class LongRunningJob
    {
        private bool _isJobCompleted;
        private bool _isJobStarted;
        private bool _isJobCompletedWithError;
        private bool _isJobCancelled;


        public LongRunningJob()
        {
            JobId = Guid.NewGuid().ToString();
            CancellationTokenSource = new CancellationTokenSource();
            CreatedOn = DateTime.Now;
        }

        public string JobId { get; internal set; }
        
        public string ClientJobId { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// The main logic of the job should be go here. Use the cancellation token
        /// if the job might need to be cancelled.
        /// </summary>
        public Action<LongRunningJob, CancellationToken> Execute { get; set; }

        /// <summary>
        /// This action will be called when the <see cref="Execute"/> is completed successfully
        /// </summary>
        public Action<LongRunningJob, CancellationToken> Success { get; set; }

        /// <summary>
        /// This action will called be called always after a job is executed
        /// </summary>
        public Action<LongRunningJob, CancellationToken> Always { get; set; }

        /// <summary>
        /// This action will be called when the job has encountered exception during execution.
        /// </summary>
        public Action<Exception, CancellationToken> Fail { get; set; }

        public bool IsJobCompleted
        {
            get => _isJobCompleted;
            internal set
            {
                _isJobCompleted = value;
                LastUpdatedOn = DateTime.Now;
            }
        }

        public bool IsJobStarted
        {
            get => _isJobStarted;
            internal set
            {
                _isJobStarted = value;
                LastUpdatedOn = DateTime.Now;
            }
        }

        public bool IsJobCompletedWithError
        {
            get => _isJobCompletedWithError;
            internal set
            {
                _isJobCompletedWithError = value;
                LastUpdatedOn = DateTime.Now;
            }
        }

        public bool IsJobCancelled
        {
            get => _isJobCancelled;
            internal set
            {
                _isJobCancelled = value;
                LastUpdatedOn = DateTime.Now;
            }
        }

        public bool CancellationRequested { get; set; }

        public Exception Error { get; internal set; }

        public DateTime? CreatedOn { get; internal set; }

        public DateTime? StartedOn { get; internal set; }

        public DateTime? CompletedOn { get; internal set; }

        public DateTime? LastUpdatedOn { get; internal set; }

        public CancellationTokenSource CancellationTokenSource { get; internal set; }

    }
}