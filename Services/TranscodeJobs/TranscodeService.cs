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
    private static readonly IDictionary<Guid, CancellationTokenSource> _cancellationSources = new Dictionary<Guid, CancellationTokenSource>();

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
        var t = _cacheService.GetData<TranscodeJob>($"trans_job_{id}");
        if (!t.IsError) return t;
        return _dbService.GetData(id);
    }
    public ErrorOr<List<TranscodeJob>> GetTranscodeJobs()
    {
        throw new System.NotImplementedException();
    }
    
    public async ValueTask StartTranscodeJob(TranscodeJob transcodeJob)
    {
        transcodeJob.setRunning();
        _dbService.UpdateData(transcodeJob);
        _cacheService.SetData($"trans_job_{transcodeJob.Id}", transcodeJob, DateTimeOffset.Now.AddDays(7));
        var cancellationTokenSource = new CancellationTokenSource();
        _cancellationSources.Add(transcodeJob.Id, cancellationTokenSource);
        await _taskQueue.QueueBackgroundWorkItemAsync(new TranscodeProcessingJob(transcodeJob, cancellationTokenSource.Token));
        // await _taskQueue.QueueBackgroundWorkItemAsync(new TranscodeProcessingJob(_logger, transcodeJob, cancellationTokenSource, _dbService, _cacheService));
    }
    
    public ErrorOr<TranscodeJob> StopTranscodeJob(Guid id)
    {
        if (_cancellationSources.ContainsKey(id))
        {
            _cancellationSources[id].Cancel();
            _cancellationSources.Remove(id);
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