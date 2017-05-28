namespace LongRunningActions.Models.Dtos
{
    public class Constants
    {
        public const string JobsNotFound = "404";

        public const string AllJobsCompleted = "201";

        public const string JobFound = "200";

        public const string JobInProgress = "100";

        public const string JobQueuedForProcessing = "101";
    }

    public class Messages
    {
        public const string JobsInProgress = "Jobs are still being processed. Please wait...";

        public const string JobsQueued = "Jobs queued for processing";

        public const string NoJobsFound = "No jobs found";
    }
}