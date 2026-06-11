using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bondstone.Persistence.EntityFrameworkCore.Operations;

public sealed class OperationStateEntityConfiguration(string? schema = null)
    : IEntityTypeConfiguration<OperationStateEntity>
{
    public const int StatusMaxLength = 32;

    public void Configure(EntityTypeBuilder<OperationStateEntity> builder)
    {
        builder.ToTable("operation_states", schema);
        builder
            .HasKey(x => x.DurableOperationId)
            .HasName("PK_operation_states");

        builder.Property(x => x.DurableOperationId).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(StatusMaxLength).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.ResultPayload);
        builder.Property(x => x.FailureReason);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.UpdatedAtUtc);
    }
}
