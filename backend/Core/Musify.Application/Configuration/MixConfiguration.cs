using System.ComponentModel.DataAnnotations;

namespace Musify.Application.Configuration;

public class MixConfiguration : IConfigurationOptions
{
    public static string SectionName => "Mix";

    [Range(4, 100)]
    public int ItemsPerMix { get; set; } = 12;
}
