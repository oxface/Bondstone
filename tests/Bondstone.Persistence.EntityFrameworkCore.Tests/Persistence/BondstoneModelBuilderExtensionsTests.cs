using Bondstone.Persistence.EntityFrameworkCore.Inbox;
using Bondstone.Persistence.EntityFrameworkCore.Operations;
using Bondstone.Persistence.EntityFrameworkCore.Outbox;
using Bondstone.Persistence.EntityFrameworkCore.Persistence;
using Bondstone.Persistence.EntityFrameworkCore.IncomingInbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Xunit;

namespace Bondstone.Persistence.EntityFrameworkCore.Tests.Persistence;

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
        Assert.Equal(OutboxMessageEntityConfiguration.TableName, entityType.GetTableName());
        IMutableKey primaryKey = entityType.FindPrimaryKey()!;
        Assert.Equal("PK_outbox_messages", primaryKey.GetName());
        Assert.Equal(nameof(OutboxMessageEntity.MessageId), Assert.Single(primaryKey.Properties).Name);
        Assert.Equal(
            OutboxMessageEntityConfiguration.MessageTypeNameMaxLength,
            entityType.FindProperty(nameof(OutboxMessageEntity.MessageTypeName))!.GetMaxLength());
        Assert.Equal(
            OutboxMessageEntityConfiguration.ClaimedByMaxLength,
            entityType.FindProperty(nameof(OutboxMessageEntity.ClaimedBy))!.GetMaxLength());
        Assert.Contains(
            entityType.GetIndexes(),
            index => index.Properties.Select(static property => property.Name).SequenceEqual(
            [
                nameof(OutboxMessageEntity.Status),
                nameof(OutboxMessageEntity.NextAttemptAtUtc),
                nameof(OutboxMessageEntity.StoredAtUtc),
            ]));
        Assert.Contains(
            entityType.GetIndexes(),
            index => index.Properties.Select(static property => property.Name).SequenceEqual(
            [
                nameof(OutboxMessageEntity.Status),
                nameof(OutboxMessageEntity.ClaimedUntilUtc),
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
        IMutableKey primaryKey = entityType.FindPrimaryKey()!;
        Assert.Equal(InboxMessageEntityConfiguration.PrimaryKeyName, primaryKey.GetName());
        Assert.Equal(
            [
                nameof(InboxMessageEntity.ModuleName),
                nameof(InboxMessageEntity.MessageId),
                nameof(InboxMessageEntity.HandlerIdentity),
            ],
            primaryKey.Properties.Select(static property => property.Name).ToArray());
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
        IMutableKey primaryKey = entityType.FindPrimaryKey()!;
        Assert.Equal("PK_operation_states", primaryKey.GetName());
        Assert.Equal(
            nameof(OperationStateEntity.DurableOperationId),
            Assert.Single(primaryKey.Properties).Name);
        Assert.Equal(
            OperationStateEntityConfiguration.StatusMaxLength,
            entityType.FindProperty(nameof(OperationStateEntity.Status))!.GetMaxLength());
        Assert.Equal(
            OperationStateEntityConfiguration.ModuleNameMaxLength,
            entityType.FindProperty(nameof(OperationStateEntity.ModuleName))!.GetMaxLength());
        Assert.Equal(
            OperationStateEntityConfiguration.MessageTypeNameMaxLength,
            entityType.FindProperty(nameof(OperationStateEntity.MessageTypeName))!.GetMaxLength());
        Assert.Equal(
            OperationStateEntityConfiguration.HandlerIdentityMaxLength,
            entityType.FindProperty(nameof(OperationStateEntity.HandlerIdentity))!.GetMaxLength());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ApplyBondstonePersistence_DoesNotConfigureDomainEventEntity()
    {
        IMutableModel model = BuildModel();

        Assert.DoesNotContain(
            model.GetEntityTypes(),
            static entityType => entityType.Name.Contains(
                "DomainEventRecordEntity",
                StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ApplyBondstonePersistence_ConfiguresIncomingInboxEntity()
    {
        IMutableEntityType entityType = BuildModel()
            .FindEntityType(typeof(IncomingInboxMessageEntity))
            ?? throw new InvalidOperationException("Incoming inbox entity type was not configured.");

        Assert.Equal("incoming_inbox_messages", entityType.GetTableName());
        IMutableKey primaryKey = entityType.FindPrimaryKey()!;
        Assert.Equal(IncomingInboxMessageEntityConfiguration.PrimaryKeyName, primaryKey.GetName());
        Assert.Equal(
            [
                nameof(IncomingInboxMessageEntity.ReceiverModule),
                nameof(IncomingInboxMessageEntity.MessageId),
                nameof(IncomingInboxMessageEntity.HandlerIdentity),
            ],
            primaryKey.Properties.Select(static property => property.Name).ToArray());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ApplyBondstoneOutbox_ConfiguresOnlyOutboxEntity()
    {
        IMutableModel model = BuildModel(static modelBuilder =>
            modelBuilder.ApplyBondstoneOutbox("bondstone"));

        IMutableEntityType entityType = model
            .FindEntityType(typeof(OutboxMessageEntity))
            ?? throw new InvalidOperationException("Outbox entity type was not configured.");

        Assert.Equal("bondstone", entityType.GetSchema());
        Assert.Equal(OutboxMessageEntityConfiguration.TableName, entityType.GetTableName());
        Assert.Null(model.FindEntityType(typeof(InboxMessageEntity)));
        Assert.Null(model.FindEntityType(typeof(OperationStateEntity)));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ApplyBondstoneInbox_ConfiguresOnlyInboxEntity()
    {
        IMutableModel model = BuildModel(static modelBuilder =>
            modelBuilder.ApplyBondstoneInbox("bondstone"));

        IMutableEntityType entityType = model
            .FindEntityType(typeof(InboxMessageEntity))
            ?? throw new InvalidOperationException("Inbox entity type was not configured.");

        Assert.Equal("bondstone", entityType.GetSchema());
        Assert.Equal(InboxMessageEntityConfiguration.TableName, entityType.GetTableName());
        Assert.Null(model.FindEntityType(typeof(OutboxMessageEntity)));
        Assert.Null(model.FindEntityType(typeof(OperationStateEntity)));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ApplyBondstoneOperationState_ConfiguresOnlyOperationStateEntity()
    {
        IMutableModel model = BuildModel(static modelBuilder =>
            modelBuilder.ApplyBondstoneOperationState("bondstone"));

        IMutableEntityType entityType = model
            .FindEntityType(typeof(OperationStateEntity))
            ?? throw new InvalidOperationException("Operation state entity type was not configured.");

        Assert.Equal("bondstone", entityType.GetSchema());
        Assert.Equal("operation_states", entityType.GetTableName());
        Assert.Null(model.FindEntityType(typeof(OutboxMessageEntity)));
        Assert.Null(model.FindEntityType(typeof(InboxMessageEntity)));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ApplyBondstoneIncomingInbox_ConfiguresOnlyIncomingInboxEntity()
    {
        IMutableModel model = BuildModel(static modelBuilder =>
            modelBuilder.ApplyBondstoneIncomingInbox("bondstone"));

        IMutableEntityType entityType = model
            .FindEntityType(typeof(IncomingInboxMessageEntity))
            ?? throw new InvalidOperationException("Incoming inbox entity type was not configured.");

        Assert.Equal("bondstone", entityType.GetSchema());
        Assert.Equal(IncomingInboxMessageEntityConfiguration.TableName, entityType.GetTableName());
        Assert.Null(model.FindEntityType(typeof(OutboxMessageEntity)));
        Assert.Null(model.FindEntityType(typeof(InboxMessageEntity)));
        Assert.Null(model.FindEntityType(typeof(OperationStateEntity)));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ApplyBondstoneIncomingInbox_ConfiguresReceiveIdentityAndClaimIndexes()
    {
        IMutableEntityType entityType = BuildModel(static modelBuilder =>
                modelBuilder.ApplyBondstoneIncomingInbox("bondstone"))
            .FindEntityType(typeof(IncomingInboxMessageEntity))
            ?? throw new InvalidOperationException("Incoming inbox entity type was not configured.");

        IMutableKey primaryKey = entityType.FindPrimaryKey()!;
        Assert.Equal(IncomingInboxMessageEntityConfiguration.PrimaryKeyName, primaryKey.GetName());
        Assert.Equal(
            [
                nameof(IncomingInboxMessageEntity.ReceiverModule),
                nameof(IncomingInboxMessageEntity.MessageId),
                nameof(IncomingInboxMessageEntity.HandlerIdentity),
            ],
            primaryKey.Properties.Select(static property => property.Name).ToArray());
        Assert.Equal(
            IncomingInboxMessageEntityConfiguration.MessageTypeNameMaxLength,
            entityType.FindProperty(nameof(IncomingInboxMessageEntity.MessageTypeName))!.GetMaxLength());
        Assert.Equal(
            IncomingInboxMessageEntityConfiguration.ModuleNameMaxLength,
            entityType.FindProperty(nameof(IncomingInboxMessageEntity.ReceiverModule))!.GetMaxLength());
        Assert.Equal(
            IncomingInboxMessageEntityConfiguration.HandlerIdentityMaxLength,
            entityType.FindProperty(nameof(IncomingInboxMessageEntity.HandlerIdentity))!.GetMaxLength());
        Assert.Equal(
            IncomingInboxMessageEntityConfiguration.SourceTransportNameMaxLength,
            entityType.FindProperty(nameof(IncomingInboxMessageEntity.SourceTransportName))!.GetMaxLength());
        Assert.Equal(
            IncomingInboxMessageEntityConfiguration.ClaimedByMaxLength,
            entityType.FindProperty(nameof(IncomingInboxMessageEntity.ClaimedBy))!.GetMaxLength());
        Assert.Contains(
            entityType.GetIndexes(),
            index => index.Properties.Select(static property => property.Name).SequenceEqual(
            [
                nameof(IncomingInboxMessageEntity.Status),
                nameof(IncomingInboxMessageEntity.NextAttemptAtUtc),
                nameof(IncomingInboxMessageEntity.IngestedAtUtc),
            ]));
        Assert.Contains(
            entityType.GetIndexes(),
            index => index.Properties.Select(static property => property.Name).SequenceEqual(
            [
                nameof(IncomingInboxMessageEntity.Status),
                nameof(IncomingInboxMessageEntity.ClaimedUntilUtc),
            ]));
        Assert.Contains(
            entityType.GetIndexes(),
            index => index.Properties.Select(static property => property.Name).SequenceEqual(
            [
                nameof(IncomingInboxMessageEntity.ReceiverModule),
                nameof(IncomingInboxMessageEntity.Status),
                nameof(IncomingInboxMessageEntity.NextAttemptAtUtc),
                nameof(IncomingInboxMessageEntity.IngestedAtUtc),
            ]));
    }

    private static IMutableModel BuildModel()
    {
        return BuildModel(static modelBuilder =>
            modelBuilder.ApplyBondstonePersistence("bondstone"));
    }

    private static IMutableModel BuildModel(Action<ModelBuilder> configure)
    {
        var modelBuilder = new ModelBuilder(new ConventionSet());
        configure(modelBuilder);
        return modelBuilder.Model;
    }
}
