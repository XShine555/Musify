using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Musify.Domain.Entities
{
#pragma warning disable CS8618
    [Table("ProcessExecution")]
    public class ProcessExecution
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? CorrelationId { get; set; }

        public Guid? ConversationId { get; set; }

        public Guid? MessageId { get; set; }

        [Required]
        [MaxLength(256)]
        public required string ProcessName { get; set; }

        [Required]
        public ProcessExecutionStatus Status { get; set; } = ProcessExecutionStatus.Running;

        [Required]
        public DateTime StartedDateTime { get; set; } = DateTime.UtcNow;

        public DateTime? FinishedDateTime { get; set; }

        [MaxLength(2048)]
        public string? ErrorMessage { get; set; }

        public ICollection<ProcessStepExecution> Steps { get; set; } = new List<ProcessStepExecution>();
    }
}
