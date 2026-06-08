using Bondstone.Messaging;
using Bondstone.Persistence;
using Bondstone.Utility;
using Microsoft.EntityFrameworkCore;

namespace Bondstone.EntityFrameworkCore.Outbox;

public sealed class EntityFrameworkCoreModuleDurableOutboxWriter<TDbContext>(
    string moduleName,
    TDbContext context,
    TimeProvider? timeProvider = null)
    : IDurableModuleOutboxWriter
    where TDbContext : DbContext
{
    private readonly EntityFrameworkCoreDurableOutboxWriter<TDbContext> _writer =
        new(context, timeProvider);

    public string ModuleName { get; } = moduleName.NormalizeRequired(
        nameof(moduleName),
        "Module name");

    public async ValueTask WriteAsync(
        DurableMessageEnvelope envelope,
        CancellationToken ct = default)
    {
        await _writer.WriteAsync(envelope, ct);
    }
}
