using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts.Infrastructure;
using Musify.Domain.Entities;

namespace Musify.Infrastructure.Persistence
{
    public class Database(DbContextOptions<Database> dbContextOptions)
        : DbContext(dbContextOptions), IDatabase
    {
        public DbSet<User> Users => Set<User>();

        public DbSet<Track> Tracks => Set<Track>();

        public DbSet<PlayList> PlayLists => Set<PlayList>();

        public DbSet<PlayListHasTrack> PlayListHasTracks => Set<PlayListHasTrack>();

        public DbSet<Upload> Uploads => Set<Upload>();
    }
}