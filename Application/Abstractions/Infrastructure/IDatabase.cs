using Microsoft.EntityFrameworkCore;
using Musify.Domain.Entities;

namespace Musify.Application.Abstractions.Infrastructure
{
    public interface IDatabase
    {
        DbSet<User> Users { get; }

        DbSet<PlayList> PlayLists { get; }

        DbSet<Track> Tracks { get; }

        DbSet<UserHasTrack> UserHasTracks { get; }

        DbSet<PlayListHasTrack> PlayListHasTracks { get; }

        DbSet<Upload> Uploads { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}