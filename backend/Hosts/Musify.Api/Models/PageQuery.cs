using Microsoft.AspNetCore.Mvc;

namespace Musify.Api.Models
{
    public record PageQuery(
        [property: FromQuery(Name = "pageNumber")] int PageNumber = 1,
        [property: FromQuery(Name = "pageSize")] int PageSize = 20);
}
