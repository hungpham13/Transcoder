using Microsoft.EntityFrameworkCore;
using Transcoder.Model;

namespace Transcoder.Persistence;

public class TranscoderDbContext : DbContext
{
    public TranscoderDbContext(DbContextOptions<TranscoderDbContext> options) : base(options)
    {
    }

    public DbSet<TranscodeJob> TranscodeJobs { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TranscoderDbContext).Assembly);
    }
}