using Musify.Infrastructure.Observability;
using Scalar.AspNetCore;
using WebApi.Endpoints;
using WebApi.Extensions;

var builder = WebApplication.CreateBuilder();

const string devCorsPolicy = "dev-cors";

builder.Services
    .AddApplicationServices(builder.Configuration)
    .AddObservability(builder.Configuration);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options => options.AddPolicy(devCorsPolicy, policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseCors(devCorsPolicy);
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