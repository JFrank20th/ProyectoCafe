using System.ComponentModel.DataAnnotations;

namespace ProyectoCafe.Infrastructure.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    [Required]
    public string? ConnectionString { get; set; }
}
