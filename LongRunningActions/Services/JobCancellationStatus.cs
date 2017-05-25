namespace LongRunningActions.Services
{
    public enum JobCancellationStatus
    {
        JobCancelled,
        JobIsAlreadyCompleted,
        JobNotFound
    }
}