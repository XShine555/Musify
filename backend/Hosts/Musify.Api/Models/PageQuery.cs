using Microsoft.AspNetCore.Mvc;
using Musify.Application.Shared;

namespace Musify.Api.Models;

/// <summary>Query-string binding for <see cref="PageRequest"/>, keeping the camelCase parameter names of the API.</summary>
public record PageQuery(
    [property: FromQuery(Name = "pageNumber")] int PageNumber = 1,
    [property: FromQuery(Name = "pageSize")] int PageSize = 20);
