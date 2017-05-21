using System;

namespace LongRunningActions.Models.Dtos
{
    public class LongRunningJobDto
    {
        public string JobId { get; set; }

        public string ClientJobId { get; set; }

        public string Name { get; set; }

        public bool IsJobCompleted { get; set; }

        public bool IsJobStarted { get; set; }

        public bool IsJobCompletedWithError { get; set; }

        public string Error { get; set; }

        public DateTime? CreatedOn { get; set; }

        public DateTime? StartedOn { get; set; }

        public DateTime? CompletedOn { get; set; }

        public DateTime? LastUpdatedOn { get; set; }
    }
}