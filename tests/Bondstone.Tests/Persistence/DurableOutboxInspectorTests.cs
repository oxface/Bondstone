using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Tests.Persistence;

public sealed class DurableOutboxInspectorTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task FindTerminalFailedAsync_WhenStoreExists_ReturnsModuleStoreRows()
    {
        var salesStore = new CapturingOutboxInspectionStore(
            "sales",
            CreateRecord(Guid.Parse("11111111-1111-1111-1111-111111111111"), "sales"));
        var billingStore = new CapturingOutboxInspectionStore(
            "billing",
            CreateRecord(Guid.Parse("22222222-2222-2222-2222-222222222222"), "billing"));
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            salesStore,
            billingStore);

        IReadOnlyList<DurableOutboxRecord> records = await serviceProvider
            .GetRequiredService<IDurableOutboxInspector>()
            .FindTerminalFailedAsync(
                " sales ",
                maxCount: 25,
                failedAtOrBeforeUtc: DateTimeOffset.Parse("2026-06-16T12:00:00+00:00"));

        DurableOutboxRecord record = Assert.Single(records);
        Assert.Equal("sales", record.Envelope.SourceModule);
        Assert.Equal(25, salesStore.MaxCount);
        Assert.Equal("sales", salesStore.SourceModuleName);
        Assert.Equal(DateTimeOffset.Parse("2026-06-16T12:00:00+00:00"), salesStore.FailedAtOrBeforeUtc);
        Assert.Equal(0, billingStore.CallCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task FindTerminalFailedAsync_WhenStoreIsMissing_ThrowsClearError()
    {
        await using ServiceProvider serviceProvider = CreateServiceProvider();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await serviceProvider
                .GetRequiredService<IDurableOutboxInspector>()
                .FindTerminalFailedAsync("fulfillment"));

        Assert.Contains("durable module outbox inspection store", exception.Message, StringComparison.Ordinal);
        Assert.Contains("fulfillment", exception.Message, StringComparison.Ordinal);
    }

    private static ServiceProvider CreateServiceProvider(
        params CapturingOutboxInspectionStore[] stores)
    {
        var services = new ServiceCollection();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", _ => { });
            bondstone.Module("billing", _ => { });
            bondstone.Module("fulfillment", _ => { });

            foreach (CapturingOutboxInspectionStore store in stores)
            {
                bondstone.Services.GetOrAddDurableModulePersistenceRegistrationRegistry()
                    .AddOutboxInspectionStore(new DurableModuleOutboxInspectionStoreRegistration(
                        store.ModuleName,
                        _ => store));
            }
        });

        return services.BuildServiceProvider();
    }

    private static DurableOutboxRecord CreateRecord(Guid messageId, string sourceModule)
    {
        var envelope = new DurableMessageEnvelope(
            messageId,
            MessageKind.Command,
            $"{sourceModule}.test.v1",
            sourceModule,
            "target",
            "{}",
            DateTimeOffset.Parse("2026-06-16T11:00:00+00:00"));
        var dispatchState = new DurableOutboxDispatchState(
            DurableOutboxStatus.TerminalFailed,
            attemptCount: 3,
            failedAtUtc: DateTimeOffset.Parse("2026-06-16T11:30:00+00:00"),
            failureReason: "dispatch failed");

        return new DurableOutboxRecord(
            envelope,
            DateTimeOffset.Parse("2026-06-16T11:00:01+00:00"),
            dispatchState);
    }

    private sealed class CapturingOutboxInspectionStore(
        string moduleName,
        params DurableOutboxRecord[] records)
        : IDurableOutboxInspectionStore
    {
        public string ModuleName { get; } = moduleName;

        public int CallCount { get; private set; }

        public int? MaxCount { get; private set; }

        public DateTimeOffset? FailedAtOrBeforeUtc { get; private set; }

        public string? SourceModuleName { get; private set; }

        public ValueTask<IReadOnlyList<DurableOutboxRecord>> FindTerminalFailedAsync(
            int maxCount = 100,
            DateTimeOffset? failedAtOrBeforeUtc = null,
            string? sourceModuleName = null,
            CancellationToken ct = default)
        {
            CallCount++;
            MaxCount = maxCount;
            FailedAtOrBeforeUtc = failedAtOrBeforeUtc;
            SourceModuleName = sourceModuleName;
            return ValueTask.FromResult<IReadOnlyList<DurableOutboxRecord>>(records);
        }
    }
}
