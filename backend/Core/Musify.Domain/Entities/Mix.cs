using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Musify.Domain.Abstractions;
using Musify.Domain.ValueObjects;

namespace Musify.Domain.Entities;

[Table("Mixes")]
public class Mix : IAuditable
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public required long UserId { get; set; }

    public MixKind Kind { get; set; }

    public int Position { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public ICollection<MixItem> Items { get; set; } = new List<MixItem>();
}
