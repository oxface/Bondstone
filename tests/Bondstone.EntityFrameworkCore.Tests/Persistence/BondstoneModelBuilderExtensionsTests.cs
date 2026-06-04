using Bondstone.EntityFrameworkCore.Inbox;
using Bondstone.EntityFrameworkCore.Operations;
using Bondstone.EntityFrameworkCore.Outbox;
using Bondstone.EntityFrameworkCore.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Xunit;

namespace Bondstone.EntityFrameworkCore.Tests.Persistence;

public sealed class BondstoneModelBuilderExtensionsTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void ApplyBondstonePersistence_ConfiguresOutboxEntity()
    {
        IMutableEntityType entityType = BuildModel()
            .FindEntityType(typeof(OutboxMessageEntity))
            ?? throw new InvalidOperationException("Outbox entity type was not configured.");

        Assert.Equal("bondstone", entityType.GetSchema());
        Assert.Equal("outbox_messages", entityType.GetTableName());
        Assert.Equal(nameof(OutboxMessageEntity.MessageId), Assert.Single(entityType.FindPrimaryKey()!.Properties).Name);
        Assert.Equal(
            OutboxMessageEntityConfiguration.MessageTypeNameMaxLength,
            entityType.FindProperty(nameof(OutboxMessageEntity.MessageTypeName))!.GetMaxLength());
        Assert.Contains(
            entityType.GetIndexes(),
            index => index.Properties.Select(static property => property.Name).SequenceEqual(
            [
                nameof(OutboxMessageEntity.Status),
                nameof(OutboxMessageEntity.NextAttemptAtUtc),
                nameof(OutboxMessageEntity.StoredAtUtc),
            ]));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ApplyBondstonePersistence_ConfiguresInboxEntity()
    {
        IMutableEntityType entityType = BuildModel()
            .FindEntityType(typeof(InboxMessageEntity))
            ?? throw new InvalidOperationException("Inbox entity type was not configured.");

        Assert.Equal("inbox_messages", entityType.GetTableName());
        Assert.Equal(
            [
                nameof(InboxMessageEntity.ModuleName),
                nameof(InboxMessageEntity.MessageId),
                nameof(InboxMessageEntity.HandlerIdentity),
            ],
            entityType.FindPrimaryKey()!.Properties.Select(static property => property.Name).ToArray());
        Assert.Equal(
            InboxMessageEntityConfiguration.HandlerIdentityMaxLength,
            entityType.FindProperty(nameof(InboxMessageEntity.HandlerIdentity))!.GetMaxLength());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ApplyBondstonePersistence_ConfiguresOperationStateEntity()
    {
        IMutableEntityType entityType = BuildModel()
            .FindEntityType(typeof(OperationStateEntity))
            ?? throw new InvalidOperationException("Operation state entity type was not configured.");

        Assert.Equal("operation_states", entityType.GetTableName());
        Assert.Equal(
            nameof(OperationStateEntity.DurableOperationId),
            Assert.Single(entityType.FindPrimaryKey()!.Properties).Name);
        Assert.Equal(
            OperationStateEntityConfiguration.StatusMaxLength,
            entityType.FindProperty(nameof(OperationStateEntity.Status))!.GetMaxLength());
    }

    private static IMutableModel BuildModel()
    {
        var modelBuilder = new ModelBuilder(new ConventionSet());
        modelBuilder.ApplyBondstonePersistence("bondstone");
        return modelBuilder.Model;
    }
}
