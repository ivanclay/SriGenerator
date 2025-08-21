namespace SriGenerator.Models;

public record ProcessingResult(
    int TotalFilesProcessed,
    int SuccessfulUpdates,
    int Failures,
    TimeSpan ProcessingTime,
    IReadOnlyList<SriResult> Results
);
