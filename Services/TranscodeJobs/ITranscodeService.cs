using ErrorOr;
using Transcoder.Model;

namespace Transcoder.Services.TranscodeJobs;

public interface ITranscodeService
{
    public ErrorOr<Created> CreateTranscodeJob(TranscodeJob transcodeJob, bool autoStart = false);
    public ErrorOr<TranscodeJob> GetTranscodeJob(Guid id);
    public ErrorOr<List<TranscodeJob>> GetTranscodeJobs();
    public void StartTranscodeJob(TranscodeJob transcodeJob);
    public ErrorOr<Updated> StopTranscodeJob(Guid id);

}