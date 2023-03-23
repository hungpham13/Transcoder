using FFMpegCore;
using FFMpegCore.Enums;
using Transcoder.Model;
using Transcoder.Persistence;

namespace Transcoder.Services.BackgroundJobs;

public class TranscodeProcessingJob : IBackgroundJob

{
    private readonly TranscodeJob _transcodeJob;
    private readonly ILogger _logger;
    private readonly CancellationToken _cancellationToken;
    private readonly IServiceProvider _serviceProvider;
    
    public TranscodeProcessingJob(ILogger logger, TranscodeJob transcodeJob, CancellationToken cancellationToken, IServiceProvider serviceProvider)
    {
        _transcodeJob = transcodeJob;
        _logger = logger;
        _cancellationToken = cancellationToken;
        _serviceProvider = serviceProvider;
    }
    public async void Run()
    {
        var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TranscoderDbContext>();
        
        _logger.LogInformation("Queued Background Task {Guid} is starting.", _transcodeJob.Id);
        var mediaInfo = FFProbe.Analyse(_transcodeJob.InputPath);
        
        var percentageDone = 0.0;
    
        void OnPercentageProgess(double percentage)
        {
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
        try
        {
            await FFMpegArguments
                .FromFileInput(_transcodeJob.InputPath)
                .OutputToFile(_transcodeJob.OutputPath, true, options => options
                    .WithVideoCodec(VideoCodec.LibX264)
                    .WithDuration(mediaInfo.Duration)
                    // .WithConstantRateFactor(21)
                    .WithAudioCodec(AudioCodec.Aac)
                    // .WithVariableBitrate(4)
                    .WithVideoFilters(filterOptions => filterOptions
                        .Scale(VideoSize.Ld))
                    .WithFastStart())
                .NotifyOnProgress(OnPercentageProgess, mediaInfo.Duration)
                .ProcessAsynchronously();
            
            
            var job = dbContext.TranscodeJobs.Find(_transcodeJob.Id);
            if(job!= null)
            {
                job.setComplete();
                //dbContext.Update(job);
                dbContext.SaveChanges();
            }
            //HandleOnJobComplete(transcodeJob.Id);
        }
        catch (OperationCanceledException)
        {
            // Prevent throwing if the Delay is cancelled
            _logger.LogInformation("Queued Background Task {Guid} was error.", _transcodeJob.Id);
        }
    }
    
}