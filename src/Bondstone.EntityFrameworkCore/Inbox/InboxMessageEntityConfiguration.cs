using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bondstone.EntityFrameworkCore.Inbox;

public sealed class InboxMessageEntityConfiguration(string? schema = null)
    : IEntityTypeConfiguration<InboxMessageEntity>
{
    public const string TableName = "inbox_messages";
    public const string PrimaryKeyName = "PK_inbox_messages";
    public const int ModuleNameMaxLength = 128;
    public const int HandlerIdentityMaxLength = 512;

    public static class Columns
    {
        public const string MessageId = nameof(InboxMessageEntity.MessageId);
        public const string ModuleName = nameof(InboxMessageEntity.ModuleName);
        public const string HandlerIdentity = nameof(InboxMessageEntity.HandlerIdentity);
        public const string ReceivedAtUtc = nameof(InboxMessageEntity.ReceivedAtUtc);
        public const string ProcessedAtUtc = nameof(InboxMessageEntity.ProcessedAtUtc);
    }

    public void Configure(EntityTypeBuilder<InboxMessageEntity> builder)
    {
        builder.ToTable(TableName, schema);
        builder
            .HasKey(x => new { x.ModuleName, x.MessageId, x.HandlerIdentity })
            .HasName(PrimaryKeyName);

        builder.Property(x => x.MessageId).HasColumnName(Columns.MessageId).IsRequired();
        builder.Property(x => x.ModuleName).HasColumnName(Columns.ModuleName).HasMaxLength(ModuleNameMaxLength).IsRequired();
        builder
            .Property(x => x.HandlerIdentity)
            .HasColumnName(Columns.HandlerIdentity)
            .HasMaxLength(HandlerIdentityMaxLength)
            .IsRequired();
        builder.Property(x => x.ReceivedAtUtc).HasColumnName(Columns.ReceivedAtUtc).IsRequired();
        builder.Property(x => x.ProcessedAtUtc).HasColumnName(Columns.ProcessedAtUtc);

        builder.HasIndex(x => x.ReceivedAtUtc);
    }
}
