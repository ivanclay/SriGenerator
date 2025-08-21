namespace SriGenerator.Models;

public record SriConfiguration(
    HashAlgorithmType HashAlgorithm = HashAlgorithmType.Sha384,
    bool IncludeExternalResources = false,
    bool CreateBackup = true,
    bool OverwriteExisting = false,
    string[] ExcludePatterns = null!
)
{
    public string[] ExcludePatterns { get; init; } = ExcludePatterns ?? Array.Empty<string>();
}
