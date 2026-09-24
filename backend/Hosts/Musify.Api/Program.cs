using System.Text.Json.Serialization;
using Musify.Api.Endpoints;
using Musify.Api.Extensions;
using Musify.Infrastructure.Observability;
using Scalar.AspNetCore;

const string DevCorsPolicy = "dev-cors";

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.NumberHandling =
        JsonNumberHandling.AllowReadingFromString |
        JsonNumberHandling.WriteAsString;
} );

builder.Services
    .AddApplicationServices(builder.Configuration)
    .AddObservability(builder.Configuration);

builder.Services.AddProblemDetails();
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options => options.AddPolicy(DevCorsPolicy, policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
}

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseCors(DevCorsPolicy);
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAuthentication();
app.UseAuthorization();

app
    .MapAlbumEndpoints()
    .MapConfigEndpoints()
    .MapGenreEndpoints()
    .MapLikeEndpoints()
    .MapMixEndpoints()
    .MapPlayListEndpoints()
    .MapTrackEndpoints()
    .MapUserEndpoints();

app.Run();

public partial class Program;
