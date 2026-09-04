using FluentValidation;

namespace Aggregator.Aggregation.Application.Feed.GetFeed;

internal sealed class GetFeedQueryValidator : AbstractValidator<GetFeedQuery>
{
    public GetFeedQueryValidator()
    {
        RuleFor(q => q.Page)
            .GreaterThanOrEqualTo(1)
            .WithErrorCode("Feed.Page.OutOfRange")
            .WithMessage("Page must be 1 or greater.");

        RuleFor(q => q.PageSize)
            .InclusiveBetween(1, 100)
            .WithErrorCode("Feed.PageSize.OutOfRange")
            .WithMessage("PageSize must be between 1 and 100.");

        RuleFor(q => q.MaxItemsPerSource)
            .InclusiveBetween(1, 100)
            .WithErrorCode("Feed.MaxItemsPerSource.OutOfRange")
            .WithMessage("MaxItemsPerSource must be between 1 and 100.");

        RuleFor(q => q)
            .Must(q => q.From <= q.To)
            .When(q => q.From is not null && q.To is not null)
            .WithErrorCode("Feed.DateRange.Invalid")
            .WithMessage("'From' must be earlier than or equal to 'To'.");
    }
}
