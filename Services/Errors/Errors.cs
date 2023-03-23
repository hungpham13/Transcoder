using ErrorOr;

namespace Transcoder.Services.Errors;

public static class Errors
{
    public static class TranscodeJob
    {
        public static Error NotFound => Error.NotFound(
            "TranscodeJob.NotFound", 
            "Transcode job not found"
        );

        public static Error InvalidInputPath => Error.Validation(
            "TranscodeJob.InvalidPath",
            "Invalid input path, file does not exist"
        );
    }
}