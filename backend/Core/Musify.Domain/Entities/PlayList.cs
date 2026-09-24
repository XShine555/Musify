using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Musify.Domain.Abstractions;
using Musify.Domain.ValueObjects;

namespace Musify.Domain.Entities
{
    [Table("PlayLists")]
    public class PlayList : IAuditable, IHasLifeCycle, IOwnedEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("UserId")]
        public required long OwnerUserId { get; set; }

        [MaxLength(50)]
        public required string Name { get; set; }

        [MaxLength(50)]
        public required string NormalizedName { get; set; }

        [MaxLength(256)]
        public string? Description { get; set; }

        public EntityPictures? Pictures { get; set; }

        public LifeCycleStatus LifeCycleStatus { get; set; } = LifeCycleStatus.Active;

        public PlayListVisibility Visibility { get; set; } = PlayListVisibility.Private;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(OwnerUserId))]
        public User Owner { get; set; } = null!;

        public ICollection<PlayListHasTrack> PlayListTracks { get; set; } = new List<PlayListHasTrack>();
    }
}
