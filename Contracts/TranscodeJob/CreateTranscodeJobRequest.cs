namespace Transcoder.Contracts.TranscodeJob;

public record CreateTranscodeJobRequest
(
   string InputPath,
   int Profile,
   bool autoStart
);