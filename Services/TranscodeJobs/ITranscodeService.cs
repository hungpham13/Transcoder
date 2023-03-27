using ErrorOr;
using Transcoder.Model;

namespace Transcoder.Services.TranscodeJobs;

public interface ITranscodeService
{
    public ValueTask<ErrorOr<Created>> CreateTranscodeJob(TranscodeJob transcodeJob, bool autoStart = false);
    public ErrorOr<TranscodeJob> GetTranscodeJob(Guid id);
    public ErrorOr<List<TranscodeJob>> GetTranscodeJobs(int status = -1);
    public ValueTask StartTranscodeJob(TranscodeJob transcodeJob);
    public ErrorOr<TranscodeJob> StopTranscodeJob(Guid id);
}