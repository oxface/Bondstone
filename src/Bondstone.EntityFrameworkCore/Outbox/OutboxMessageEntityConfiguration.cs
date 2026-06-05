using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bondstone.EntityFrameworkCore.Outbox;

public sealed class OutboxMessageEntityConfiguration(string? schema = null)
    : IEntityTypeConfiguration<OutboxMessageEntity>
{
    public const int MessageTypeNameMaxLength = 256;
    public const int ModuleNameMaxLength = 128;
    public const int TraceParentMaxLength = 128;
    public const int TraceStateMaxLength = 512;
    public const int PartitionKeyMaxLength = 512;
    public const int StatusMaxLength = 32;
    public const int FailureReasonMaxLength = 2048;
    public const int ClaimedByMaxLength = 256;

    public void Configure(EntityTypeBuilder<OutboxMessageEntity> builder)
    {
        builder.ToTable("outbox_messages", schema);
        builder
            .HasKey(x => x.MessageId)
            .HasName("PK_outbox_messages");

        builder.Property(x => x.MessageId).IsRequired();
        builder.Property(x => x.MessageKind).HasConversion<string>().HasMaxLength(StatusMaxLength).IsRequired();
        builder.Property(x => x.MessageTypeName).HasMaxLength(MessageTypeNameMaxLength).IsRequired();
        builder.Property(x => x.SourceModule).HasMaxLength(ModuleNameMaxLength).IsRequired();
        builder.Property(x => x.TargetModule).HasMaxLength(ModuleNameMaxLength);
        builder.Property(x => x.DurableOperationId);
        builder.Property(x => x.TraceParent).HasMaxLength(TraceParentMaxLength);
        builder.Property(x => x.TraceState).HasMaxLength(TraceStateMaxLength);
        builder.Property(x => x.TraceBaggage);
        builder.Property(x => x.CausationId);
        builder.Property(x => x.PartitionKey).HasMaxLength(PartitionKeyMaxLength);
        builder.Property(x => x.Payload).IsRequired();
        builder.Property(x => x.Metadata);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.StoredAtUtc).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(StatusMaxLength).IsRequired();
        builder.Property(x => x.AttemptCount).IsRequired();
        builder.Property(x => x.NextAttemptAtUtc);
        builder.Property(x => x.DispatchedAtUtc);
        builder.Property(x => x.FailedAtUtc);
        builder.Property(x => x.FailureReason).HasMaxLength(FailureReasonMaxLength);
        builder.Property(x => x.ClaimedBy).HasMaxLength(ClaimedByMaxLength);
        builder.Property(x => x.ClaimedUntilUtc);

        builder.HasIndex(x => new { x.Status, x.NextAttemptAtUtc, x.StoredAtUtc });
        builder.HasIndex(x => new { x.Status, x.ClaimedUntilUtc });
        builder.HasIndex(x => x.MessageTypeName);
        builder.HasIndex(x => x.DurableOperationId);
    }
}
