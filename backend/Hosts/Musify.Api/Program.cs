using System.Text.Json.Serialization;
using Musify.Infrastructure.Observability;
using Scalar.AspNetCore;
using Musify.Api.Endpoints;
using Musify.Api.Extensions;

var builder = WebApplication.CreateBuilder();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

const string devCorsPolicy = "dev-cors";

builder.Services
    .AddApplicationServices(builder.Configuration)
    .AddObservability(builder.Configuration);

builder.Services.AddProblemDetails();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options => options.AddPolicy(devCorsPolicy, policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
}

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseCors(devCorsPolicy);
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAuthentication();
app.UseAuthorization();

app
    .MapAlbumEndpoints()
    .MapConfigEndpoints()
    .MapMixEndpoints()
    .MapPlayListEndpoints()
    .MapTrackEndpoints()
    .MapUserEndpoints();

app.Run();

// Makes the top-level-statements Program class public so
// WebApplicationFactory<Program> (Musify.Api.Tests) can see it.
public partial class Program;