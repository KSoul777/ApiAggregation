namespace Aggregator.Aggregation.Application.Feed.GetFeed;

public static class FeedQueryProcessor
{
    public static IReadOnlyList<AggregatedItem> Filter(
        IEnumerable<AggregatedItem> items,
        GetFeedQuery query)
    {
        IEnumerable<AggregatedItem> filtered = items;

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            filtered = filtered.Where(i =>
                string.Equals(i.Category, query.Category, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Source))
        {
            filtered = filtered.Where(i =>
                string.Equals(i.Source, query.Source, StringComparison.OrdinalIgnoreCase));
        }

        if (query.From is not null)
        {
            filtered = filtered.Where(i => i.Timestamp is not null && i.Timestamp >= query.From);
        }

        if (query.To is not null)
        {
            filtered = filtered.Where(i => i.Timestamp is not null && i.Timestamp <= query.To);
        }

        return filtered.ToList();
    }

    public static IReadOnlyList<AggregatedItem> Sort(
        IReadOnlyList<AggregatedItem> items,
        FeedSortField sortBy,
        SortDirection direction) =>
        sortBy switch
        {
            FeedSortField.Timestamp => OrderByNullable(items, i => i.Timestamp, direction),
            FeedSortField.Relevance => OrderByNullable(items, i => i.Relevance, direction),
            FeedSortField.Source => OrderByText(items, i => i.Source, direction),
            FeedSortField.Category => OrderByText(items, i => i.Category, direction),
            FeedSortField.Title => OrderByText(items, i => i.Title, direction),
            _ => items
        };

    public static (IReadOnlyList<AggregatedItem> Items, int TotalCount) Apply(
        IEnumerable<AggregatedItem> items,
        GetFeedQuery query)
    {
        IReadOnlyList<AggregatedItem> filtered = Filter(items, query);
        IReadOnlyList<AggregatedItem> sorted = Sort(filtered, query.SortBy, query.Order);

        int skip = (query.Page - 1) * query.PageSize;
        IReadOnlyList<AggregatedItem> page = sorted.Skip(skip).Take(query.PageSize).ToList();

        return (page, filtered.Count);
    }

    private static List<AggregatedItem> OrderByNullable<TKey>(
        IEnumerable<AggregatedItem> items,
        Func<AggregatedItem, TKey?> keySelector,
        SortDirection direction)
        where TKey : struct, IComparable<TKey>
    {
        IOrderedEnumerable<AggregatedItem> ordered = items.OrderByDescending(i => keySelector(i).HasValue);

        ordered = direction == SortDirection.Ascending
            ? ordered.ThenBy(i => keySelector(i) ?? default)
            : ordered.ThenByDescending(i => keySelector(i) ?? default);

        return ordered.ToList();
    }

    private static List<AggregatedItem> OrderByText(
        IEnumerable<AggregatedItem> items,
        Func<AggregatedItem, string> keySelector,
        SortDirection direction) =>
        (direction == SortDirection.Ascending
            ? items.OrderBy(keySelector, StringComparer.OrdinalIgnoreCase)
            : items.OrderByDescending(keySelector, StringComparer.OrdinalIgnoreCase))
        .ToList();
}
