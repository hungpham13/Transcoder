
using Microsoft.EntityFrameworkCore;
using Transcoder.Persistence;
using Transcoder.Services.BackgroundQueue;
using Transcoder.Services.TranscodeJobs;
using Transcoder.Services.Storage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

//Databases
builder.Services.AddDbContext<TranscoderDbContext>(
    options => options.UseSqlServer(
            "Data Source=172.16.1.118;Initial Catalog=MediaServer;Persist Security Info=True;User ID=sa;Password=sa@123;MultipleActiveResultSets=True;TrustServerCertificate=True"
            ));

builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddScoped(typeof(IDatabaseService<>), typeof(DatabaseService<>));
builder.Services.AddControllers().AddNewtonsoftJson();

builder.Services.AddScoped<ITranscodeService, TranscodeService>();
builder.Services.AddSingleton<ITranscodeProcessingService, TranscodeProcessingService>();
builder.Services.AddHostedService<QueuedHostedService>();
builder.Services.AddSingleton<IBackgroundTaskQueue>(ctx =>
{
    if (!int.TryParse(builder.Configuration["QueueCapacity"], out var queueCapacity))
        queueCapacity = 100;
    return new BackgroundTaskQueue(queueCapacity);
});
builder.Services.AddControllers();



var app = builder.Build();

{
    app.UseExceptionHandler("/error");
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();

    app.UseCors();

    app.UseAuthorization();
    app.MapControllers();
    app.Run();
}

