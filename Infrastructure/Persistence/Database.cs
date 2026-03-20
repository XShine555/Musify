using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts.Infrastructure;
using Musify.Domain.Entities;
using Musify.Infrastructure.Configuration;

namespace Musify.Infrastructure.Persistence
{
    public class Database(DatabaseConfiguration configuration)
        : DbContext, IDatabase
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(configuration.ConnectionString);
        }

        public DbSet<User> Users => Set<User>();

        public DbSet<Track> Tracks => Set<Track>();

        public DbSet<PlayList> PlayLists => Set<PlayList>();

        public DbSet<PlayListHasTrack> PlayListHasTracks => Set<PlayListHasTrack>();

        public DbSet<Upload> Uploads => Set<Upload>();

        public DbSet<Job> Jobs => Set<Job>();

        public DbSet<JobExecution> JobExecutions => Set<JobExecution>();
    }
}