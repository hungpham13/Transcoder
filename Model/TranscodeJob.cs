using ErrorOr;
using Transcoder.Contracts.TranscodeJob;
using Transcoder.Services.Errors;

namespace Transcoder.Model;

public class TranscodeJob : BaseEntity
{
    public int Status { get; set; }
    public int Profile { get;  set; }
    public string InputPath { get;  set; }
    public string OutputPath { get;  set; }
    public float PercentageDone { get;  set; }
    public DateTime CreatedAt { get;  set; }
    public string? Note { get; set; }

    TranscodeJob()
    {
    }

    private TranscodeJob(
        Guid id,
        int status,
        int profile,
        string inputPath,
        string outputPath,
        float percentageDone,
        DateTime createdAt,
        string? note
    )
    {
        Id = id;
        Status = status;
        Profile = profile;
        InputPath = inputPath;
        OutputPath = outputPath;
        PercentageDone = percentageDone;
        CreatedAt = createdAt;
        Note = note;
    }
    public void setPercentage(float percentage)
    {
        PercentageDone = percentage;
    }
    public void setComplete()
    {
        Status = 0;
        PercentageDone = 100;
        Note = $"Complete at {DateTime.Now}";
    }
    public void setRunning()
    {
        Status = 1;
        Note = $"Running";
    }
    public void setError(string error)
    {
        Status = 2;
        Note = $"Error: {error}";
    }
    public void setCanceled()
    {
        Status = 4;
        Note = $"Canceled at {DateTime.Now}";
    }

    private static ErrorOr<TranscodeJob> Create(
        int status,
        int profile,
        string inputPath,
        string outputPath,
        float percentageDone,
        DateTime createdAt,
        string? note,
        Guid? id = null
    )
    {
        List<Error> errors = new();
        if (!File.Exists(inputPath))
            errors.Add(Errors.TranscodeJob.InvalidInputPath);
        if (errors.Count > 0)
            return errors;
        
        return new TranscodeJob(
            id ?? new Guid(),
            status,
            profile,
            inputPath,
            outputPath,
            percentageDone,
            createdAt,
            note
        );
    }

    public static ErrorOr<TranscodeJob> From(CreateTranscodeJobRequest request)
    {
        var inName = Path.GetFileNameWithoutExtension(request.InputPath);
        var inExt = Path.GetExtension(request.InputPath);
        var outName = $"{inName}_out_{request.Profile}{inExt}";
        var outPath = Path.Combine(Path.GetDirectoryName(request.InputPath)!, outName);
        
        Console.WriteLine("Output file path: " + outPath);
        
        return Create(
            3,
            request.Profile,
            request.InputPath,
            outPath,
            0,
            DateTime.Now,
            "Initialized"
        );
    }
}
