using System;

namespace LongRunningActions.Services
{
    public class LongRunningJob
    {
        private bool _isJobCompleted;
        private bool _isJobStarted;
        private bool _isJobCompletedWithError;


        public LongRunningJob()
        {
            JobId = Guid.NewGuid().ToString();
            CreatedOn = DateTime.Now;
        }

        public string JobId { get; internal set; }

        public string ClientJobId { get; set; }

        public string Name { get; set; }

        public Action<LongRunningJob> Execute { get; set; }

        public Action<LongRunningJob> Success { get; set; }

        public Action<LongRunningJob> Always { get; set; }

        public Action<Exception> Fail { get; set; }

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

        public Exception Error { get; internal set; }

        public DateTime? CreatedOn { get; internal set; }

        public DateTime? StartedOn { get; internal set; }

        public DateTime? CompletedOn { get; internal set; }

        public DateTime? LastUpdatedOn { get; internal set; }

    }
}