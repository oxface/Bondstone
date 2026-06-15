using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bondstone.Persistence.EntityFrameworkCore.Operations;

public sealed class OperationStateEntityConfiguration(string? schema = null)
    : IEntityTypeConfiguration<OperationStateEntity>
{
    public const int StatusMaxLength = 32;
    public const int ModuleNameMaxLength = 128;
    public const int MessageTypeNameMaxLength = 256;
    public const int HandlerIdentityMaxLength = 512;

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
        builder.Property(x => x.ModuleName).HasMaxLength(ModuleNameMaxLength);
        builder.Property(x => x.MessageTypeName).HasMaxLength(MessageTypeNameMaxLength);
        builder.Property(x => x.HandlerIdentity).HasMaxLength(HandlerIdentityMaxLength);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.UpdatedAtUtc);
    }
}
