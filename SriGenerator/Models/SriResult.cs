namespace SriGenerator.Models;

public record SriResult(
    string FilePath,
    string ResourceUrl,
    string IntegrityHash,
    ResourceType Type,
    bool IsLocal,
    SriStatus Status,
    string? ErrorMessage = null
);
