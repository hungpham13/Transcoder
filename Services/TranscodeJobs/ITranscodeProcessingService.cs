using Transcoder.Model;

namespace Transcoder.Services.TranscodeJobs;

public interface ITranscodeProcessingService
{
    Task Run(TranscodeProcessingJob transcodeProcessingJob);
}