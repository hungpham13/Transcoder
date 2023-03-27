using ErrorOr;
using Transcoder.Model;
using Transcoder.Services.BackgroundQueue;
using Transcoder.Services.Storage;

namespace Transcoder.Services.TranscodeJobs;

public class TranscodeService : ITranscodeService
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly ILogger _logger;
    private readonly IDatabaseService<TranscodeJob> _dbService;
    private readonly ICacheService _cacheService;
    private static readonly IDictionary<Guid, CancellationTokenSource> CancellationSources = new Dictionary<Guid, CancellationTokenSource>();

    public TranscodeService(
        IBackgroundTaskQueue taskQueue,
        ILogger<TranscodeService> logger,
        IDatabaseService<TranscodeJob> dbService,
        ICacheService cacheService
        )
    {
        _taskQueue = taskQueue;
        _cacheService = cacheService;
        _logger = logger;
        _dbService = dbService;
    }
    
    public async ValueTask<ErrorOr<Created>> CreateTranscodeJob(TranscodeJob transcodeJob, bool autoStart = false)
    {
        var result = _dbService.CreateData(transcodeJob);
        if (result.IsError) return result;
        
        _cacheService.SetData($"trans_job_{transcodeJob.Id}", transcodeJob, DateTimeOffset.Now.AddDays(7));

        if (autoStart)
            await StartTranscodeJob(transcodeJob);
        return Result.Created;
    }
    public ErrorOr<TranscodeJob> GetTranscodeJob(Guid id)
    {
        var t = _dbService.GetData(id);
        if (t.IsError) return t;
        var newerT = _cacheService.GetData<TranscodeJob>($"trans_job_{id}");
        return newerT.IsError ? t : newerT;
    }
    public ErrorOr<List<TranscodeJob>> GetTranscodeJobs(int status = -1)
    {
        List<TranscodeJob> jobs = new();
        var all = _dbService.GetQueryable();
        foreach (var job in all)
        {
            if (job.Status != status && status != -1) continue;
            var result = GetTranscodeJob(job.Id);
            if (!result.IsError) jobs.Add(result.Value);
        }
        return jobs;
    }
    
    public async ValueTask StartTranscodeJob(TranscodeJob transcodeJob)
    {
        transcodeJob.setRunning();
        _dbService.UpdateData(transcodeJob);
        _cacheService.SetData($"trans_job_{transcodeJob.Id}", transcodeJob, DateTimeOffset.Now.AddDays(7));
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        if (!CancellationSources.ContainsKey(transcodeJob.Id))
        {
            CancellationSources.Add(transcodeJob.Id, cancellationTokenSource);
        }
        else
        {
            CancellationSources[transcodeJob.Id] = cancellationTokenSource;
        }
        await _taskQueue.QueueBackgroundWorkItemAsync(new TranscodeProcessingJob(transcodeJob, cancellationTokenSource.Token));
    }
    
    public ErrorOr<TranscodeJob> StopTranscodeJob(Guid id)
    {
        if (CancellationSources.ContainsKey(id))
        {
            CancellationSources[id].Cancel();
            CancellationSources.Remove(id);
            _logger.LogInformation("Stop triggered.");
        }

        var result = GetTranscodeJob(id);
        if (result.IsError) return Errors.Errors.TranscodeJob.NotFound;
        var transcodeJob = result.Value;
        transcodeJob.setCanceled();
        _dbService.UpdateData(transcodeJob);
        _cacheService.SetData($"trans_job_{transcodeJob.Id}", transcodeJob, DateTimeOffset.Now.AddDays(7));
        return transcodeJob;
    }
}