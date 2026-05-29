using FluentAssertions;
using Musify.Application.Pagination;
using X.PagedList;
using Xunit;

namespace Musify.Application.Tests.Pagination;

public sealed class PaginatedResponseTests
{
    [Fact]
    public void FromPagedList_maps_all_fields_for_a_middle_page()
    {
        var source = Enumerable.Range(1, 25).ToList();
        var paged = new PagedList<int>(source, pageNumber: 2, pageSize: 10);

        var response = PaginatedResponse<int>.FromPagedList(paged);

        response.PageNumber.Should().Be(2);
        response.PageSize.Should().Be(10);
        response.PageCount.Should().Be(3);
        response.TotalItemCount.Should().Be(25);
        response.HasNextPage.Should().BeTrue();
        response.HasPreviousPage.Should().BeTrue();
        response.Items.Should().Equal(11, 12, 13, 14, 15, 16, 17, 18, 19, 20);
    }

    [Fact]
    public void FromPagedList_first_page_has_no_previous_page()
    {
        var paged = new PagedList<int>(Enumerable.Range(1, 5).ToList(), pageNumber: 1, pageSize: 10);

        var response = PaginatedResponse<int>.FromPagedList(paged);

        response.HasPreviousPage.Should().BeFalse();
        response.HasNextPage.Should().BeFalse();
        response.TotalItemCount.Should().Be(5);
    }
}
