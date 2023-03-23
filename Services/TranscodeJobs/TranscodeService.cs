using ErrorOr;
using Transcoder.Model;
using Transcoder.Persistence;
using Transcoder.Services.BackgroundJobs;
using Transcoder.Services.BackgroundQueue;
using Transcoder.Services.Storage;

namespace Transcoder.Services.TranscodeJobs;

public class TranscodeService : ITranscodeService
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly ILogger _logger;
    private readonly CancellationToken _cancellationToken;
    private readonly IDatabaseService<TranscodeJob> _dbService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IDictionary<Guid, CancellationTokenSource> _cancellationSources = new Dictionary<Guid, CancellationTokenSource>();

    public TranscodeService(
        IBackgroundTaskQueue taskQueue,
        IServiceProvider serviceProvider,
        ILogger<TranscodeService> logger,
        IHostApplicationLifetime appLifetime,
        IDatabaseService<TranscodeJob> dbService
        )
    {
        _taskQueue = taskQueue;
        _logger = logger;
        _cancellationToken = appLifetime.ApplicationStopping;
        _dbService = dbService;
        _serviceProvider = serviceProvider;
    }
    
    public ErrorOr<Created> CreateTranscodeJob(TranscodeJob transcodeJob, bool autoStart = false)
    {
        var result = _dbService.CreateData(transcodeJob);
        if (result.IsError) return result;

        if (autoStart)
            StartTranscodeJob(transcodeJob);
        return Result.Created;
    }
    public ErrorOr<TranscodeJob> GetTranscodeJob(Guid id)
    {
        throw new System.NotImplementedException();
    }
    public ErrorOr<List<TranscodeJob>> GetTranscodeJobs()
    {
        throw new System.NotImplementedException();
    }
    
    public async void StartTranscodeJob(TranscodeJob transcodeJob)
    {
        
        var cancellationTokenSource = new CancellationTokenSource();
        _cancellationSources.Add(transcodeJob.Id, cancellationTokenSource);
        await _taskQueue.QueueBackgroundWorkItemAsync(new TranscodeProcessingJob(_logger, transcodeJob, cancellationTokenSource.Token, _serviceProvider));
    }
    
    public ErrorOr<Updated> StopTranscodeJob(Guid id)
    {
        if (_cancellationSources.ContainsKey(id))
        {
            _cancellationSources[id].Cancel();
            _cancellationSources.Remove(id);
            return Result.Updated;
        }
        return Errors.Errors.TranscodeJob.NotFound;
    }
}