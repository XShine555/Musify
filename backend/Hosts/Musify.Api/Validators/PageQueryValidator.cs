using FluentValidation;
using Musify.Api.Models;
using Musify.Application.Shared;

namespace Musify.Api.Validators;

public sealed class PageQueryValidator : AbstractValidator<PageQuery>
{
    public PageQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, PageRequest.MaxPageSize);
    }
}
