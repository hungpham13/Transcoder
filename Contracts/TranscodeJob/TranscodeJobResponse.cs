namespace Transcoder.Contracts.TranscodeJob;

public record TranscodeJobResponse
(
    Guid id,
    int status,
    int profile,
    string inputPath,
    string outputPath,
    float percentageDone,
    string note,
    DateTime createdAt
);