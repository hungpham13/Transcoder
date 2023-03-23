using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transcoder.Model;

namespace Transcoder.Persistence.Configurations;

public class TranscodeJobConfiguration : IEntityTypeConfiguration<TranscodeJob>
{
    public void Configure(EntityTypeBuilder<TranscodeJob> builder)
    {
        builder.HasKey(b => b.Id);
    }
}