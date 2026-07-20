namespace Assignmet1_Presentation.Models;

public sealed class PaginationSlice<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int CurrentPage { get; init; } = 1;
    public int TotalPages { get; init; }
    public int TotalItems { get; init; }
    public int PageSize { get; init; }
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public int FirstItemNumber => TotalItems == 0
        ? 0
        : (CurrentPage - 1) * PageSize + 1;
    public int LastItemNumber => TotalItems == 0
        ? 0
        : FirstItemNumber + Items.Count - 1;

    public PaginationViewModel ToViewModel(
        string pageName,
        string pageParameterName,
        string itemLabel,
        IReadOnlyDictionary<string, object?>? routeValues = null,
        string? fragment = null)
    {
        return new PaginationViewModel
        {
            PageName = pageName,
            PageParameterName = pageParameterName,
            ItemLabel = itemLabel,
            CurrentPage = CurrentPage,
            TotalPages = TotalPages,
            TotalItems = TotalItems,
            PageSize = PageSize,
            ItemCount = Items.Count,
            RouteValues = routeValues ?? new Dictionary<string, object?>(),
            Fragment = fragment
        };
    }
}

public sealed class PaginationViewModel
{
    public string PageName { get; init; } = string.Empty;
    public string PageParameterName { get; init; } = "pageNumber";
    public string ItemLabel { get; init; } = "mục";
    public int CurrentPage { get; init; } = 1;
    public int TotalPages { get; init; }
    public int TotalItems { get; init; }
    public int PageSize { get; init; }
    public int ItemCount { get; init; }
    public IReadOnlyDictionary<string, object?> RouteValues { get; init; } =
        new Dictionary<string, object?>();
    public string? Fragment { get; init; }
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public int FirstItemNumber => TotalItems == 0
        ? 0
        : (CurrentPage - 1) * PageSize + 1;
    public int LastItemNumber => TotalItems == 0
        ? 0
        : FirstItemNumber + ItemCount - 1;
}

public static class PaginationHelper
{
    public static PaginationSlice<T> Paginate<T>(
        IEnumerable<T> source,
        int requestedPage,
        int pageSize)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (pageSize <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                "Kích thước trang phải lớn hơn 0.");

        var items = source as IReadOnlyList<T> ?? source.ToList();
        var totalItems = items.Count;
        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)pageSize);
        var currentPage = Math.Clamp(requestedPage, 1, Math.Max(1, totalPages));
        var pageItems = items
            .Skip((currentPage - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginationSlice<T>
        {
            Items = pageItems,
            CurrentPage = currentPage,
            TotalPages = totalPages,
            TotalItems = totalItems,
            PageSize = pageSize
        };
    }
}
