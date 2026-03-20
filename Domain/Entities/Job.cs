using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Musify.Domain.Entities
{
    [Table("Job")]
    public class Job
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public required JobType JobType { get; set; }

        [Required]
        public JobState JobState { get; set; } = JobState.Pending;

        [NotMapped]
        public bool IsCompleted => JobState == JobState.Completed;

        [NotMapped]
        public bool IsFailed => JobState == JobState.Failed;

        [NotMapped]
        public bool IsPending => JobState == JobState.Pending;

        [Required]
        public required string Payload { get; set; }

        [Required]
        public DateTime CreateAt { get; set; } = DateTime.UtcNow;

        [Required]
        public int RetryCount { get; set; } = 0;

        public DateTime CompletedAt { get; set; }
    }
}