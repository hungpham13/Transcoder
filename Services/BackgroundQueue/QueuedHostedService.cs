using Transcoder.Model;
using Transcoder.Services.TranscodeJobs;

namespace Transcoder.Services.BackgroundQueue;

public class QueuedHostedService : BackgroundService
{
    private readonly ILogger<QueuedHostedService> _logger;
    private readonly ITranscodeProcessingService _transcodeProcessingService;

    public QueuedHostedService(
        IBackgroundTaskQueue taskQueue, 
        ILogger<QueuedHostedService> logger,
        ITranscodeProcessingService processingService
        )
    {
        TaskQueue = taskQueue;
        _logger = logger;
        _transcodeProcessingService = processingService;
    }

    public IBackgroundTaskQueue TaskQueue { get; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            $"Queued Hosted Service is running.{Environment.NewLine}");

        await BackgroundProcessing(stoppingToken);
    }

    private async Task BackgroundProcessing(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var workItem = 
                await TaskQueue.DequeueAsync(stoppingToken);

            try
            {
                if (workItem is TranscodeProcessingJob transcodeProcessingJob)
                {
                    await _transcodeProcessingService.Run(transcodeProcessingJob);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error occurred executing {WorkItem}.", nameof(workItem));
            }
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Queued Hosted Service is stopping.");

        await base.StopAsync(stoppingToken);
    }
}
