using Musify.Infrastructure.Observability;
using Scalar.AspNetCore;
using WebApi.Endpoints;
using WebApi.Extensions;

var builder = WebApplication.CreateBuilder();

builder.Services
    .AddApplicationServices(builder.Configuration)
    .AddInfrastructureOpenTelemetry(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app
    .MapPlayListEndpoints()
    .MapTrackEndpoints()
    .MapUserEndpoints();

app.Run();
