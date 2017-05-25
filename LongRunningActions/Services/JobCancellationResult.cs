namespace LongRunningActions.Services
{
    public class JobCancellationResult
    {
        /// <summary>
        /// The job object which is just cancelled or was completed already and could not be cancelled
        /// </summary>
        public LongRunningJob Job { get; internal set; }

        public string Message { get; internal set; }

        /// <summary>
        /// Id of the job for which cancellation was requested.
        /// </summary>
        public string JobId { get; internal set; }

        public JobCancellationStatus JobCancellationStatus { get; internal set; }
    }
}