using Bondstone.Configuration;
using Bondstone.Modules;
using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Tests.Persistence;

public sealed class DurableInboxInspectorTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task FindUnprocessedAsync_WhenStoreExists_ReturnsModuleStoreRows()
    {
        var salesStore = new CapturingInboxInspectionStore(
            "sales",
            CreateRecord(Guid.Parse("11111111-1111-1111-1111-111111111111"), "sales"));
        var billingStore = new CapturingInboxInspectionStore(
            "billing",
            CreateRecord(Guid.Parse("22222222-2222-2222-2222-222222222222"), "billing"));
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            salesStore,
            billingStore);

        IReadOnlyList<DurableInboxRecord> records = await serviceProvider
            .GetRequiredService<IDurableInboxInspector>()
            .FindUnprocessedAsync(
                " sales ",
                maxCount: 25,
                receivedAtOrBeforeUtc: DateTimeOffset.Parse("2026-06-16T12:00:00+00:00"));

        DurableInboxRecord record = Assert.Single(records);
        Assert.Equal("sales", record.Key.ModuleName);
        Assert.Equal(25, salesStore.MaxCount);
        Assert.Equal("sales", salesStore.ModuleNameFilter);
        Assert.Equal(DateTimeOffset.Parse("2026-06-16T12:00:00+00:00"), salesStore.ReceivedAtOrBeforeUtc);
        Assert.Equal(0, billingStore.CallCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task FindUnprocessedAsync_WhenStoreIsMissing_ThrowsClearError()
    {
        await using ServiceProvider serviceProvider = CreateServiceProvider();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await serviceProvider
                .GetRequiredService<IDurableInboxInspector>()
                .FindUnprocessedAsync("fulfillment"));

        Assert.Contains("durable module inbox inspection store", exception.Message, StringComparison.Ordinal);
        Assert.Contains("fulfillment", exception.Message, StringComparison.Ordinal);
    }

    private static ServiceProvider CreateServiceProvider(
        params CapturingInboxInspectionStore[] stores)
    {
        var services = new ServiceCollection();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", _ => { });
            bondstone.Module("billing", _ => { });
            bondstone.Module("fulfillment", _ => { });

            foreach (CapturingInboxInspectionStore store in stores)
            {
                bondstone.Services.GetOrAddDurableModulePersistenceRegistrationRegistry()
                    .AddInboxInspectionStore(new DurableModuleInboxInspectionStoreRegistration(
                        store.ModuleName,
                        _ => store));
            }
        });

        return services.BuildServiceProvider();
    }

    private static DurableInboxRecord CreateRecord(Guid messageId, string moduleName)
    {
        return new DurableInboxRecord(
            new DurableInboxMessageKey(
                messageId,
                moduleName,
                $"{moduleName}.handler.v1"),
            DateTimeOffset.Parse("2026-06-16T11:00:00+00:00"));
    }

    private sealed class CapturingInboxInspectionStore(
        string moduleName,
        params DurableInboxRecord[] records)
        : IDurableInboxInspectionStore
    {
        public string ModuleName { get; } = moduleName;

        public int CallCount { get; private set; }

        public int? MaxCount { get; private set; }

        public DateTimeOffset? ReceivedAtOrBeforeUtc { get; private set; }

        public string? ModuleNameFilter { get; private set; }

        public ValueTask<IReadOnlyList<DurableInboxRecord>> FindUnprocessedAsync(
            int maxCount = 100,
            DateTimeOffset? receivedAtOrBeforeUtc = null,
            string? moduleName = null,
            CancellationToken ct = default)
        {
            CallCount++;
            MaxCount = maxCount;
            ReceivedAtOrBeforeUtc = receivedAtOrBeforeUtc;
            ModuleNameFilter = moduleName;
            return ValueTask.FromResult<IReadOnlyList<DurableInboxRecord>>(records);
        }
    }
}
