using FluentValidation;
using Musify.Api.DataTransferObjects.Tracks;

namespace Musify.Api.Validators.Tracks
{
    public sealed class InviteCoauthorsRequestValidator : AbstractValidator<InviteCoauthorsRequest>
    {
        public const int MaxUsersPerRequest = 20;

        public InviteCoauthorsRequestValidator()
        {
            RuleFor(x => x.UserIds)
                .NotEmpty()
                .Must(ids => ids.Count <= MaxUsersPerRequest)
                .WithMessage($"At most {MaxUsersPerRequest} users can be invited at once.");

            RuleForEach(x => x.UserIds)
                .GreaterThan(0);
        }
    }
}
