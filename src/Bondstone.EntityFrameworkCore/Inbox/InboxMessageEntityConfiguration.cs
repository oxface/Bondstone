using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bondstone.EntityFrameworkCore.Inbox;

public sealed class InboxMessageEntityConfiguration(string? schema = null)
    : IEntityTypeConfiguration<InboxMessageEntity>
{
    public const int ModuleNameMaxLength = 128;
    public const int HandlerIdentityMaxLength = 512;

    public void Configure(EntityTypeBuilder<InboxMessageEntity> builder)
    {
        builder.ToTable("inbox_messages", schema);
        builder.HasKey(x => new { x.ModuleName, x.MessageId, x.HandlerIdentity });

        builder.Property(x => x.MessageId).IsRequired();
        builder.Property(x => x.ModuleName).HasMaxLength(ModuleNameMaxLength).IsRequired();
        builder.Property(x => x.HandlerIdentity).HasMaxLength(HandlerIdentityMaxLength).IsRequired();
        builder.Property(x => x.ReceivedAtUtc).IsRequired();
        builder.Property(x => x.ProcessedAtUtc);

        builder.HasIndex(x => x.ReceivedAtUtc);
    }
}
