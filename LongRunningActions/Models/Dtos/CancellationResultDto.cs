
namespace LongRunningActions.Models.Dtos
{
    public class CancellationResultDto
    {
        /// <summary>
        /// The job object which is just cancelled or was completed already and could not be cancelled
        /// </summary>
        public LongRunningJobDto Job { get; set; }

        public string Message { get; set; }

        /// <summary>
        /// Id of the job for which cancellation was requested.
        /// </summary>
        public string JobId { get; set; }

        public string JobCancellationStatus { get; set; }
    }
}
