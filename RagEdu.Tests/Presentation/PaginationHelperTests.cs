using Assignmet1_Presentation.Models;

namespace RagEdu.Tests.Presentation;

public sealed class PaginationHelperTests
{
    [Fact]
    public void Paginate_ReturnsTheRequestedPage()
    {
        var result = PaginationHelper.Paginate(
            Enumerable.Range(1, 25),
            requestedPage: 2,
            pageSize: 10);

        Assert.Equal(2, result.CurrentPage);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(25, result.TotalItems);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(11, result.FirstItemNumber);
        Assert.Equal(20, result.LastItemNumber);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
        Assert.Equal(Enumerable.Range(11, 10), result.Items);
    }

    [Theory]
    [InlineData(-10, 1, 1)]
    [InlineData(0, 1, 1)]
    [InlineData(99, 3, 21)]
    public void Paginate_ClampsOutOfRangePageNumbers(
        int requestedPage,
        int expectedPage,
        int expectedFirstItem)
    {
        var result = PaginationHelper.Paginate(
            Enumerable.Range(1, 25),
            requestedPage,
            pageSize: 10);

        Assert.Equal(expectedPage, result.CurrentPage);
        Assert.Equal(expectedFirstItem, result.Items[0]);
    }

    [Fact]
    public void Paginate_HandlesAnEmptyCollection()
    {
        var result = PaginationHelper.Paginate<int>(
            [],
            requestedPage: 20,
            pageSize: 10);

        Assert.Empty(result.Items);
        Assert.Equal(1, result.CurrentPage);
        Assert.Equal(0, result.TotalPages);
        Assert.Equal(0, result.TotalItems);
        Assert.Equal(0, result.FirstItemNumber);
        Assert.Equal(0, result.LastItemNumber);
        Assert.False(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public void Paginate_RejectsAnInvalidPageSize()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PaginationHelper.Paginate(
                Enumerable.Range(1, 5),
                requestedPage: 1,
                pageSize: 0));
    }

    [Fact]
    public void ToViewModel_PreservesRouteValuesAndFragment()
    {
        var slice = PaginationHelper.Paginate(
            Enumerable.Range(1, 25),
            requestedPage: 2,
            pageSize: 10);
        var routeValues = new Dictionary<string, object?>
        {
            ["id"] = 42,
            ["search"] = "kiểm thử"
        };

        var model = slice.ToViewModel(
            "/Subjects/Details",
            "pageNumber",
            "tài liệu",
            routeValues,
            "documents");

        Assert.Equal("/Subjects/Details", model.PageName);
        Assert.Equal("pageNumber", model.PageParameterName);
        Assert.Equal("tài liệu", model.ItemLabel);
        Assert.Equal(2, model.CurrentPage);
        Assert.Equal(11, model.FirstItemNumber);
        Assert.Equal(20, model.LastItemNumber);
        Assert.Equal(42, model.RouteValues["id"]);
        Assert.Equal("kiểm thử", model.RouteValues["search"]);
        Assert.Equal("documents", model.Fragment);
    }
}
