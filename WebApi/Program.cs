using Musify.Infrastructure.Observability;
using Scalar.AspNetCore;
using WebApi.Endpoints;
using WebApi.Extensions;

var builder = WebApplication.CreateBuilder();

builder.Services
    .AddApplicationServices(builder.Configuration)
    .AddObservability(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAuthentication();
app.UseAuthorization();

app
    .MapPlayListEndpoints()
    .MapTrackEndpoints()
    .MapUserEndpoints();

app.Run();

public partial class Program { }
