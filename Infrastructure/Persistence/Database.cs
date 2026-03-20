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

        public DbSet<ProcessExecution> ProcessExecutions => Set<ProcessExecution>();

        public DbSet<ProcessStepExecution> ProcessStepExecutions => Set<ProcessStepExecution>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProcessExecution>()
                .HasIndex(process => new { process.CorrelationId, process.ProcessName });

            modelBuilder.Entity<ProcessStepExecution>()
                .HasIndex(step => new { step.ProcessExecutionId, step.StepName });

            modelBuilder.Entity<ProcessExecution>()
                .HasMany(process => process.Steps)
                .WithOne(step => step.ProcessExecution)
                .HasForeignKey(step => step.ProcessExecutionId)
                .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);
        }
    }
}