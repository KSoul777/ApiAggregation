using Aggregator.Aggregation.Application.Feed.GetFeed;
using AwesomeAssertions;
using FluentValidation.Results;

namespace Aggregator.Aggregation.Tests.Feed;

public class GetFeedQueryValidatorTests
{
    private readonly GetFeedQueryValidator _validator = new();

    [Fact]
    public void DefaultQuery_IsValid()
    {
        ValidationResult result = _validator.Validate(new GetFeedQuery());

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Page_BelowOne_Fails(int page)
    {
        ValidationResult result = _validator.Validate(new GetFeedQuery { Page = page });

        result.Errors.Should().Contain(e => e.ErrorCode == "Feed.Page.OutOfRange");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void PageSize_OutOfRange_Fails(int pageSize)
    {
        ValidationResult result = _validator.Validate(new GetFeedQuery { PageSize = pageSize });

        result.Errors.Should().Contain(e => e.ErrorCode == "Feed.PageSize.OutOfRange");
    }

    [Fact]
    public void DateRange_FromAfterTo_Fails()
    {
        var query = new GetFeedQuery
        {
            From = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero),
            To = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
        };

        ValidationResult result = _validator.Validate(query);

        result.Errors.Should().Contain(e => e.ErrorCode == "Feed.DateRange.Invalid");
    }
}
