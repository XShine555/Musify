using Mediator;
using Musify.Api.Authentication;
using Musify.Api.Extensions;
using Musify.Application.Coauthors;

namespace Musify.Api.Endpoints
{
    public static class CoauthorEndpoints
    {
        public static IEndpointRouteBuilder MapCoauthorEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/coauthor-invitations")
                .WithTags("Coauthors")
                .RequireAuthorization();

            group.MapPost("/{id:guid}/accept", AcceptInvitation)
                .WithName("AcceptCoauthorInvitation")
                .WithSummary("Accept a pending coauthor invitation addressed to you")
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status409Conflict);

            group.MapPost("/{id:guid}/reject", RejectInvitation)
                .WithName("RejectCoauthorInvitation")
                .WithSummary("Reject a pending coauthor invitation addressed to you")
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status409Conflict);

            return app;
        }

        private static async Task<IResult> AcceptInvitation(
            IMediator mediator,
            CurrentUser currentUser,
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new RespondToCoauthorInvitationCommand(currentUser.RequiredId, id, true), cancellationToken);
            return result.ToHttpResult();
        }

        private static async Task<IResult> RejectInvitation(
            IMediator mediator,
            CurrentUser currentUser,
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new RespondToCoauthorInvitationCommand(currentUser.RequiredId, id, false), cancellationToken);
            return result.ToHttpResult();
        }
    }
}
