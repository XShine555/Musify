using Microsoft.Extensions.Options;
using Musify.StreamingGateway.Authentication;
using Musify.StreamingGateway.Configuration;
using Musify.StreamingGateway.Middleware;

const string CorsPolicy = "media-clients";

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<StreamTicketValidationConfiguration>()
    .Bind(builder.Configuration.GetRequiredSection(StreamTicketValidationConfiguration.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton(serviceProvider =>
    serviceProvider.GetRequiredService<IOptions<StreamTicketValidationConfiguration>>().Value);

builder.Services.AddSingleton<TicketValidator>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy(CorsPolicy, policy =>
{
    if (builder.Environment.IsDevelopment())
        policy.AllowAnyOrigin();
    else
        policy.WithOrigins(allowedOrigins);

    policy.AllowAnyHeader()
        .WithMethods("GET", "HEAD")
        .WithExposedHeaders("Content-Range", "Accept-Ranges", "Content-Length");
}));

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapGet("/health", () => Results.Ok("healthy"))
    .WithSummary("Health check endpoint");

app.UseCors(CorsPolicy);
app.UseMiddleware<TicketValidationMiddleware>();
app.MapReverseProxy();

app.Run();
