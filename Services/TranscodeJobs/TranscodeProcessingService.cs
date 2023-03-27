using FFMpegCore;
using FFMpegCore.Enums;
using Transcoder.Model;
using Transcoder.Persistence;
using Transcoder.Services.BackgroundQueue;
using Transcoder.Services.Storage;

namespace Transcoder.Services.TranscodeJobs;

public class TranscodeProcessingService : ITranscodeProcessingService
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly CancellationToken _cancellationToken;
    private readonly ICacheService _cacheService;
    // private readonly IDatabaseService<TranscodeJob> _dbService;
    
    public TranscodeProcessingService(
        ILogger<TranscodeProcessingService> logger, 
        ICacheService cacheService,
        IHostApplicationLifetime appLifetime,
        // IDatabaseService<TranscodeJob> dbService
        IServiceProvider serviceProvider
        )
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        // _dbService = dbService;
        _cancellationToken = appLifetime.ApplicationStopping;
        _cacheService = cacheService;
    }
    
    public async Task Run(TranscodeProcessingJob transcodeProcessingJob)
    {
        var transcodeJob = transcodeProcessingJob.JobInfo;
        var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TranscoderDbContext>();
        var job = dbContext.TranscodeJobs.Find(transcodeJob.Id);
        
        // var result = _dbService.GetData(transcodeJob.Id);
        
        _logger.LogInformation("Queued Background Task {Guid} is starting.", transcodeJob.Id);
        var mediaInfo = FFProbe.Analyse(transcodeJob.InputPath);
        
        var percentageDone = 0.0;
    
        void OnPercentageProgess(double percentage)
        {
            transcodeJob.setPercentage((float)percentage);
            _cacheService.SetData($"trans_job_{transcodeJob.Id}", transcodeJob, DateTimeOffset.Now.AddDays(7));
            _logger.LogInformation(percentage + "%");
            if (percentage < 100)
            {
                percentageDone = percentage;
            }
        }
        
        // var snap_interval = 5;
        // for (int i=0;i<mediaInfo.Duration.TotalSeconds/snap_interval;i++)
        // {
        //     FFMpeg.Snapshot(inputPath, $"output/image_{i}.png", new Size(900, 400), TimeSpan.FromSeconds(snap_interval*i));
        // }
        if (job == null) return;
        // if (result.IsError) return;
        // var job = result.Value;
        try
        {
            await FFMpegArguments
                .FromFileInput(transcodeJob.InputPath)
                .OutputToFile(transcodeJob.OutputPath, true, options => options
                    .WithVideoCodec(VideoCodec.LibX264)
                    .WithDuration(mediaInfo.Duration)
                    // .WithConstantRateFactor(21)
                    .WithAudioCodec(AudioCodec.Aac)
                    // .WithVariableBitrate(4)
                    .WithVideoFilters(filterOptions => filterOptions
                        .Scale(VideoSize.Ld))
                    .WithFastStart())
                .CancellableThrough(transcodeProcessingJob.CancellationToken)
                .CancellableThrough(_cancellationToken)
                .NotifyOnProgress(OnPercentageProgess, mediaInfo.Duration)
                .ProcessAsynchronously();
                
            job.setComplete();
            //dbContext.Update(job);
        }
        catch (OperationCanceledException e)
        {
            // Prevent throwing if the Delay is cancelled
            _logger.LogInformation("Queued Background Task {Guid} was error.", transcodeJob.Id);
            job.setError(e.Message);
        }
        dbContext.SaveChanges();
        // _dbService.UpdateData(job);
        _cacheService.SetData($"trans_job_{job.Id}", job, DateTimeOffset.Now.AddDays(7));
    }
}