using Microsoft.AspNetCore.Mvc;
using Transcoder.Contracts.TranscodeJob;
using Transcoder.Model;
using ErrorOr;
using Transcoder.Services.TranscodeJobs;

namespace Transcoder.Controllers;

public class TranscodeJobsController : ApiController
{
    private readonly ITranscodeService _transcodeService;
    private readonly ILogger<TranscodeJobsController> _logger;

    public TranscodeJobsController(
        ILogger<TranscodeJobsController> logger, 
        ITranscodeService transcodeService)
    {
        _logger = logger;
        _transcodeService = transcodeService;
    }

    
    [HttpGet("{id:guid}")]
    public IActionResult GetJob(Guid id)
    {
        ErrorOr<TranscodeJob> transcodeJobResult = _transcodeService.GetTranscodeJob(id);
        return transcodeJobResult.Match(
            transcodeJob => Ok(MapTranscodeResponse(transcodeJob)),
            errors => Problem(errors)
        );
    }
    
    [HttpPost]
    public IActionResult CreateJob(CreateTranscodeJobRequest request)
    {          
        ErrorOr<TranscodeJob> requestToFormat = TranscodeJob.From(request);
        if (requestToFormat.IsError) return Problem(requestToFormat.Errors);
        var transcodeJob = requestToFormat.Value;
        var createTranscodeJob = _transcodeService.CreateTranscodeJob(transcodeJob, request.autoStart);

        return createTranscodeJob.Result.Match(created => CreatedAtAction(
            actionName: nameof(GetJob),
            routeValues: new {id= transcodeJob.Id},
            value: MapTranscodeResponse(transcodeJob)),
            errors => Problem(errors)
        );
    }
    
    [HttpPut("{id:guid}/Start")]
    public async Task<IActionResult> StartJob(Guid id)
    {
        ErrorOr<TranscodeJob> transcodeJobResult = _transcodeService.GetTranscodeJob(id);
        if (transcodeJobResult.IsError) return Problem(transcodeJobResult.Errors);
        await _transcodeService.StartTranscodeJob(transcodeJobResult.Value);
        return Ok();
    }
    
    [HttpPut("{id:guid}/Stop")]
    public IActionResult StopJob(Guid id)
    {
        ErrorOr<TranscodeJob> result = _transcodeService.StopTranscodeJob(id);
        return result.Match(
            job => Ok(MapTranscodeResponse(job)),
            errors => Problem(errors)
            );
    }
    
    [HttpGet("")]
    public IActionResult GetJobs()
    {
        _logger.LogInformation("MonitorAsync Loop is starting.");
        return Ok();
    }
    
    public static TranscodeJobResponse MapTranscodeResponse(TranscodeJob transcodeJob) => new TranscodeJobResponse
    (
        transcodeJob.Id,
        transcodeJob.Status,
        transcodeJob.Profile,
        transcodeJob.InputPath,
        transcodeJob.OutputPath,
        transcodeJob.PercentageDone,
        transcodeJob.Note ?? "",
        transcodeJob.CreatedAt
    );
}
