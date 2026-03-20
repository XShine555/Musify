using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Musify.Domain.Entities
{
#pragma warning disable CS8618
    [Table("JobExecution")]
    public class JobOperation
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public required Guid JobId { get; set; }

        [Required]
        public JobState JobState { get; set; } = JobState.Pending;

        [NotMapped]
        public bool IsCompleted => JobState == JobState.Completed;

        [NotMapped]
        public bool IsFailed => JobState == JobState.Failed;

        [NotMapped]
        public bool IsPending => JobState == JobState.Pending;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime FinishedAt { get; set; }

        [ForeignKey(nameof(JobId)) ]
        public Job Job { get; set; }
    }
}