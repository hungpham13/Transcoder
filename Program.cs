
using Microsoft.EntityFrameworkCore;
using Transcoder.Model;
using Transcoder.Persistence;
using Transcoder.Services.BackgroundQueue;
using Transcoder.Services.TranscodeJobs;
using Transcoder.Services.Storage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Databases
builder.Services.AddDbContext<TranscoderDbContext>(
    options => options.UseSqlServer(
            "Data Source=172.16.1.118;Initial Catalog=MediaServer;Persist Security Info=True;User ID=sa;Password=sa@123;MultipleActiveResultSets=True;TrustServerCertificate=True"
            ));

builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped(typeof(IDatabaseService<>), typeof(DatabaseService<>));
builder.Services.AddControllers().AddNewtonsoftJson();

builder.Services.AddScoped<ITranscodeService, TranscodeService>();
builder.Services.AddHostedService<QueuedHostedService>();
builder.Services.AddSingleton<IBackgroundTaskQueue>(ctx =>
{
    if (!int.TryParse(builder.Configuration["QueueCapacity"], out var queueCapacity))
        queueCapacity = 100;
    return new BackgroundTaskQueue(queueCapacity);
});
builder.Services.AddControllers();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
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

