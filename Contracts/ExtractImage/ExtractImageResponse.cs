namespace Transcoder.Contracts.ExtractImage;

public record ExtractImageResponse(
    string imagePath,
    TimeOnly atTime
);