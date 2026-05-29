namespace WebApi.Scalar;

public static class ScalarExtensions
{
    public static IServiceCollection AddScalarConfiguration(this IServiceCollection services)
    {
        services.ConfigureOptions<ScalarOptionsSetup>();

        return services;
    }
}
