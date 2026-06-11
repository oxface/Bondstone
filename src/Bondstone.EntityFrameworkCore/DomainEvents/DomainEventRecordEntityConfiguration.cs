using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bondstone.EntityFrameworkCore.DomainEvents;

public sealed class DomainEventRecordEntityConfiguration(string? schema = null)
    : IEntityTypeConfiguration<DomainEventRecordEntity>
{
    public const string TableName = "domain_event_records";
    public const string PrimaryKeyName = "PK_domain_event_records";
    public const int DomainEventNameMaxLength = 256;
    public const int ModuleNameMaxLength = 128;
    public const int PayloadTypeNameMaxLength = 1024;
    public const int TraceParentMaxLength = 128;
    public const int TraceStateMaxLength = 512;

    public static class Columns
    {
        public const string DomainEventId = nameof(DomainEventRecordEntity.DomainEventId);
        public const string ModuleName = nameof(DomainEventRecordEntity.ModuleName);
        public const string DomainEventName = nameof(DomainEventRecordEntity.DomainEventName);
        public const string PayloadTypeName = nameof(DomainEventRecordEntity.PayloadTypeName);
        public const string Payload = nameof(DomainEventRecordEntity.Payload);
        public const string PayloadMetadata = nameof(DomainEventRecordEntity.PayloadMetadata);
        public const string OccurredAtUtc = nameof(DomainEventRecordEntity.OccurredAtUtc);
        public const string CapturedAtUtc = nameof(DomainEventRecordEntity.CapturedAtUtc);
        public const string TraceParent = nameof(DomainEventRecordEntity.TraceParent);
        public const string TraceState = nameof(DomainEventRecordEntity.TraceState);
        public const string TraceBaggage = nameof(DomainEventRecordEntity.TraceBaggage);
        public const string CausationId = nameof(DomainEventRecordEntity.CausationId);
    }

    public void Configure(EntityTypeBuilder<DomainEventRecordEntity> builder)
    {
        builder.ToTable(TableName, schema);
        builder
            .HasKey(x => x.DomainEventId)
            .HasName(PrimaryKeyName);

        builder.Property(x => x.DomainEventId).HasColumnName(Columns.DomainEventId).IsRequired();
        builder.Property(x => x.ModuleName).HasColumnName(Columns.ModuleName).HasMaxLength(ModuleNameMaxLength).IsRequired();
        builder.Property(x => x.DomainEventName).HasColumnName(Columns.DomainEventName).HasMaxLength(DomainEventNameMaxLength).IsRequired();
        builder.Property(x => x.PayloadTypeName).HasColumnName(Columns.PayloadTypeName).HasMaxLength(PayloadTypeNameMaxLength).IsRequired();
        builder.Property(x => x.Payload).HasColumnName(Columns.Payload).IsRequired();
        builder.Property(x => x.PayloadMetadata).HasColumnName(Columns.PayloadMetadata);
        builder.Property(x => x.OccurredAtUtc).HasColumnName(Columns.OccurredAtUtc).IsRequired();
        builder.Property(x => x.CapturedAtUtc).HasColumnName(Columns.CapturedAtUtc).IsRequired();
        builder.Property(x => x.TraceParent).HasColumnName(Columns.TraceParent).HasMaxLength(TraceParentMaxLength);
        builder.Property(x => x.TraceState).HasColumnName(Columns.TraceState).HasMaxLength(TraceStateMaxLength);
        builder.Property(x => x.TraceBaggage).HasColumnName(Columns.TraceBaggage);
        builder.Property(x => x.CausationId).HasColumnName(Columns.CausationId);

        builder.HasIndex(x => new { x.ModuleName, x.CapturedAtUtc });
        builder.HasIndex(x => new { x.ModuleName, x.DomainEventName });
    }
}
