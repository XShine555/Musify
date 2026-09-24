using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Domain.Abstractions;
using Musify.Domain.Entities;
using Musify.Infrastructure.MassTransit.Arguments;

namespace Musify.Infrastructure.MassTransit.Activities.LifeCycle
{
    internal abstract class MarkLifeCycleActivity<TEntity>(IDatabase database, ILogger logger)
        : IExecuteActivity<MarkLifeCycleArguments>
        where TEntity : class, IHasLifeCycle
    {
        public async Task<ExecutionResult> Execute(ExecuteContext<MarkLifeCycleArguments> executeContext)
        {
            var arguments = executeContext.Arguments;
            var entity = await database.Set<TEntity>()
                .SingleOrDefaultAsync(e => e.Id == arguments.Id, executeContext.CancellationToken);

            if (entity == null)
            {
                logger.LogInformation("{Entity} {Id} not found while marking it as {Status}, skipping", typeof(TEntity).Name, arguments.Id, arguments.Status);
                return executeContext.Completed();
            }

            entity.LifeCycleStatus = arguments.Status;
            await database.SaveChangesAsync(executeContext.CancellationToken);

            logger.LogInformation("Marked {Entity} {Id} as {Status}", typeof(TEntity).Name, arguments.Id, arguments.Status);
            return executeContext.Completed();
        }
    }

    internal abstract class DeleteEntityActivity<TEntity>(IDatabase database, ILogger logger)
        : IExecuteActivity<DeleteEntityArguments>
        where TEntity : class, IHasLifeCycle
    {
        public async Task<ExecutionResult> Execute(ExecuteContext<DeleteEntityArguments> executeContext)
        {
            var id = executeContext.Arguments.Id;
            var entity = await database.Set<TEntity>()
                .SingleOrDefaultAsync(e => e.Id == id, executeContext.CancellationToken);

            if (entity == null)
            {
                logger.LogInformation("{Entity} {Id} not found while deleting it, skipping", typeof(TEntity).Name, id);
                return executeContext.Completed();
            }

            database.Set<TEntity>().Remove(entity);
            await database.SaveChangesAsync(executeContext.CancellationToken);

            logger.LogInformation("Deleted {Entity} {Id} from the database", typeof(TEntity).Name, id);
            return executeContext.Completed();
        }
    }

    internal sealed class MarkTrackLifeCycleActivity(IDatabase database, ILogger<MarkTrackLifeCycleActivity> logger)
        : MarkLifeCycleActivity<Track>(database, logger)
    {
        public const string ExecuteEndpointName = "mark-track-life-cycle";
    }

    internal sealed class MarkPlayListLifeCycleActivity(IDatabase database, ILogger<MarkPlayListLifeCycleActivity> logger)
        : MarkLifeCycleActivity<PlayList>(database, logger)
    {
        public const string ExecuteEndpointName = "mark-play-list-life-cycle";
    }

    internal sealed class MarkAlbumLifeCycleActivity(IDatabase database, ILogger<MarkAlbumLifeCycleActivity> logger)
        : MarkLifeCycleActivity<Album>(database, logger)
    {
        public const string ExecuteEndpointName = "mark-album-life-cycle";
    }

    internal sealed class DeleteTrackActivity(IDatabase database, ILogger<DeleteTrackActivity> logger)
        : DeleteEntityActivity<Track>(database, logger)
    {
        public const string ExecuteEndpointName = "delete-track";
    }

    internal sealed class DeletePlayListActivity(IDatabase database, ILogger<DeletePlayListActivity> logger)
        : DeleteEntityActivity<PlayList>(database, logger)
    {
        public const string ExecuteEndpointName = "delete-play-list";
    }

    internal sealed class DeleteAlbumActivity(IDatabase database, ILogger<DeleteAlbumActivity> logger)
        : DeleteEntityActivity<Album>(database, logger)
    {
        public const string ExecuteEndpointName = "delete-album";
    }
}
