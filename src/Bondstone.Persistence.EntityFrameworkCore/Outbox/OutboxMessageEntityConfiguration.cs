using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bondstone.Persistence.EntityFrameworkCore.Outbox;

public sealed class OutboxMessageEntityConfiguration(string? schema = null)
    : IEntityTypeConfiguration<OutboxMessageEntity>
{
    public const string TableName = "outbox_messages";
    public const string PrimaryKeyName = "PK_outbox_messages";
    public const int MessageTypeNameMaxLength = 256;
    public const int ModuleNameMaxLength = 128;
    public const int TraceParentMaxLength = 128;
    public const int TraceStateMaxLength = 512;
    public const int PartitionKeyMaxLength = 512;
    public const int StatusMaxLength = 32;
    public const int FailureReasonMaxLength = 2048;
    public const int ClaimedByMaxLength = 256;

    public static class Columns
    {
        public const string MessageId = nameof(OutboxMessageEntity.MessageId);
        public const string MessageKind = nameof(OutboxMessageEntity.MessageKind);
        public const string MessageTypeName = nameof(OutboxMessageEntity.MessageTypeName);
        public const string SourceModule = nameof(OutboxMessageEntity.SourceModule);
        public const string TargetModule = nameof(OutboxMessageEntity.TargetModule);
        public const string DurableOperationId = nameof(OutboxMessageEntity.DurableOperationId);
        public const string TraceParent = nameof(OutboxMessageEntity.TraceParent);
        public const string TraceState = nameof(OutboxMessageEntity.TraceState);
        public const string TraceBaggage = nameof(OutboxMessageEntity.TraceBaggage);
        public const string CausationId = nameof(OutboxMessageEntity.CausationId);
        public const string PartitionKey = nameof(OutboxMessageEntity.PartitionKey);
        public const string Payload = nameof(OutboxMessageEntity.Payload);
        public const string Metadata = nameof(OutboxMessageEntity.Metadata);
        public const string CreatedAtUtc = nameof(OutboxMessageEntity.CreatedAtUtc);
        public const string StoredAtUtc = nameof(OutboxMessageEntity.StoredAtUtc);
        public const string Status = nameof(OutboxMessageEntity.Status);
        public const string AttemptCount = nameof(OutboxMessageEntity.AttemptCount);
        public const string NextAttemptAtUtc = nameof(OutboxMessageEntity.NextAttemptAtUtc);
        public const string DispatchedAtUtc = nameof(OutboxMessageEntity.DispatchedAtUtc);
        public const string FailedAtUtc = nameof(OutboxMessageEntity.FailedAtUtc);
        public const string FailureReason = nameof(OutboxMessageEntity.FailureReason);
        public const string ClaimedBy = nameof(OutboxMessageEntity.ClaimedBy);
        public const string ClaimedUntilUtc = nameof(OutboxMessageEntity.ClaimedUntilUtc);
    }

    public void Configure(EntityTypeBuilder<OutboxMessageEntity> builder)
    {
        builder.ToTable(TableName, schema);
        builder
            .HasKey(x => x.MessageId)
            .HasName(PrimaryKeyName);

        builder.Property(x => x.MessageId).HasColumnName(Columns.MessageId).IsRequired();
        builder
            .Property(x => x.MessageKind)
            .HasColumnName(Columns.MessageKind)
            .HasConversion<string>()
            .HasMaxLength(StatusMaxLength)
            .IsRequired();
        builder
            .Property(x => x.MessageTypeName)
            .HasColumnName(Columns.MessageTypeName)
            .HasMaxLength(MessageTypeNameMaxLength)
            .IsRequired();
        builder.Property(x => x.SourceModule).HasColumnName(Columns.SourceModule).HasMaxLength(ModuleNameMaxLength).IsRequired();
        builder.Property(x => x.TargetModule).HasColumnName(Columns.TargetModule).HasMaxLength(ModuleNameMaxLength);
        builder.Property(x => x.DurableOperationId).HasColumnName(Columns.DurableOperationId);
        builder.Property(x => x.TraceParent).HasColumnName(Columns.TraceParent).HasMaxLength(TraceParentMaxLength);
        builder.Property(x => x.TraceState).HasColumnName(Columns.TraceState).HasMaxLength(TraceStateMaxLength);
        builder.Property(x => x.TraceBaggage).HasColumnName(Columns.TraceBaggage);
        builder.Property(x => x.CausationId).HasColumnName(Columns.CausationId);
        builder.Property(x => x.PartitionKey).HasColumnName(Columns.PartitionKey).HasMaxLength(PartitionKeyMaxLength);
        builder.Property(x => x.Payload).HasColumnName(Columns.Payload).IsRequired();
        builder.Property(x => x.Metadata).HasColumnName(Columns.Metadata);
        builder.Property(x => x.CreatedAtUtc).HasColumnName(Columns.CreatedAtUtc).IsRequired();
        builder.Property(x => x.StoredAtUtc).HasColumnName(Columns.StoredAtUtc).IsRequired();
        builder
            .Property(x => x.Status)
            .HasColumnName(Columns.Status)
            .HasConversion<string>()
            .HasMaxLength(StatusMaxLength)
            .IsRequired();
        builder.Property(x => x.AttemptCount).HasColumnName(Columns.AttemptCount).IsRequired();
        builder.Property(x => x.NextAttemptAtUtc).HasColumnName(Columns.NextAttemptAtUtc);
        builder.Property(x => x.DispatchedAtUtc).HasColumnName(Columns.DispatchedAtUtc);
        builder.Property(x => x.FailedAtUtc).HasColumnName(Columns.FailedAtUtc);
        builder.Property(x => x.FailureReason).HasColumnName(Columns.FailureReason).HasMaxLength(FailureReasonMaxLength);
        builder.Property(x => x.ClaimedBy).HasColumnName(Columns.ClaimedBy).HasMaxLength(ClaimedByMaxLength);
        builder.Property(x => x.ClaimedUntilUtc).HasColumnName(Columns.ClaimedUntilUtc);

        builder.HasIndex(x => new { x.Status, x.NextAttemptAtUtc, x.StoredAtUtc });
        builder.HasIndex(x => new { x.Status, x.ClaimedUntilUtc });
        builder.HasIndex(x => x.MessageTypeName);
        builder.HasIndex(x => x.DurableOperationId);
    }
}
