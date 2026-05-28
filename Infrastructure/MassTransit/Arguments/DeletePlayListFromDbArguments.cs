namespace Musify.Infrastructure.MassTransit.Activities.PlayLists
{
    internal record DeletePlayListFromDbArguments(
        Guid PlayListId);
}
