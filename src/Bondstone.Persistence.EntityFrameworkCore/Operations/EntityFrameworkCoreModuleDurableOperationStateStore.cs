using Bondstone.Messaging;
using Bondstone.Persistence;
using Bondstone.Utility;
using Microsoft.EntityFrameworkCore;

namespace Bondstone.Persistence.EntityFrameworkCore.Operations;

public sealed class EntityFrameworkCoreModuleDurableOperationStateStore<TDbContext>(
    string moduleName,
    TDbContext context)
    : IDurableOperationStateStore,
        IDurableOperationExpirationStore
    where TDbContext : DbContext
{
    private readonly EntityFrameworkCoreDurableOperationStateStore<TDbContext> _store =
        new(context);

    public string ModuleName { get; } = moduleName.NormalizeRequired(
        nameof(moduleName),
        "Module name");

    public async ValueTask<DurableOperationState?> GetStateAsync(
        Guid durableOperationId,
        CancellationToken ct = default)
    {
        return await _store.GetStateAsync(durableOperationId, ct);
    }

    public async ValueTask SaveAsync(
        DurableOperationState state,
        CancellationToken ct = default)
    {
        await _store.SaveAsync(state, ct);
    }

    public async ValueTask<IReadOnlyList<DurableOperationState>> FindExpirationCandidatesAsync(
        DateTimeOffset expiresBeforeUtc,
        int maxCount,
        CancellationToken ct = default)
    {
        return await _store.FindExpirationCandidatesAsync(
            expiresBeforeUtc,
            maxCount,
            ct);
    }
}
