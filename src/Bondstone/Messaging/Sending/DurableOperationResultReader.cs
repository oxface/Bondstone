using System.Text.Json;
using Bondstone.Persistence;

namespace Bondstone.Messaging;

internal sealed class DurableOperationResultReader(
    IDurableOperationReader operationReader,
    DurableOperationResultPayloadSerializer payloadSerializer,
    TimeProvider? timeProvider = null)
    : IDurableOperationResultReader
{
    private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMilliseconds(250);
    private readonly IDurableOperationReader _operationReader =
        operationReader ?? throw new ArgumentNullException(nameof(operationReader));
    private readonly DurableOperationResultPayloadSerializer _payloadSerializer =
        payloadSerializer ?? throw new ArgumentNullException(nameof(payloadSerializer));
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async ValueTask<DurableOperationResult<TResult>> GetResultAsync<TResult>(
        Guid durableOperationId,
        CancellationToken ct = default)
    {
        ValidateOperationId(durableOperationId);

        DurableOperationState? state = await _operationReader.GetStateAsync(
            durableOperationId,
            ct);

        return CreateResult<TResult>(
            durableOperationId,
            state);
    }

    public async ValueTask<DurableOperationResult<TResult>> WaitForResultAsync<TResult>(
        Guid durableOperationId,
        TimeSpan timeout,
        TimeSpan? pollInterval = null,
        CancellationToken ct = default)
    {
        ValidateOperationId(durableOperationId);
        ValidatePositive(timeout, nameof(timeout));

        TimeSpan effectivePollInterval = pollInterval ?? DefaultPollInterval;
        ValidatePositive(effectivePollInterval, nameof(pollInterval));

        DateTimeOffset deadlineUtc = _timeProvider.GetUtcNow().Add(timeout);
        DurableOperationResult<TResult> lastResult;

        while (true)
        {
            lastResult = await GetResultAsync<TResult>(
                durableOperationId,
                ct);

            if (lastResult.IsTerminal)
            {
                return lastResult;
            }

            TimeSpan remaining = deadlineUtc - _timeProvider.GetUtcNow();
            if (remaining <= TimeSpan.Zero)
            {
                throw new TimeoutException(
                    $"Durable operation '{durableOperationId}' did not reach a terminal state before timeout '{timeout}'.");
            }

            TimeSpan delay = remaining < effectivePollInterval
                ? remaining
                : effectivePollInterval;
            await Task.Delay(
                delay,
                _timeProvider,
                ct);
        }
    }

    private DurableOperationResult<TResult> CreateResult<TResult>(
        Guid durableOperationId,
        DurableOperationState? state)
    {
        if (state is null)
        {
            return new DurableOperationResult<TResult>(
                durableOperationId,
                status: null,
                updatedAtUtc: null);
        }

        if (state.Status != DurableOperationStatus.Completed
            || state.ResultPayload is null)
        {
            return new DurableOperationResult<TResult>(
                state.DurableOperationId,
                state.Status,
                state.UpdatedAtUtc,
                failureReason: state.FailureReason);
        }

        TResult? result;
        try
        {
            result = _payloadSerializer.Deserialize<TResult>(
                state.ResultPayload);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            Type resultType = typeof(TResult);
            string resultTypeName = resultType.FullName ?? resultType.Name;
            string message =
                $"Durable operation '{state.DurableOperationId}' completed with a result payload, but the payload could not be deserialized as '{resultTypeName}'. {ex.GetType().Name}: {ex.Message}";

            return new DurableOperationResult<TResult>(
                state.DurableOperationId,
                state.Status,
                state.UpdatedAtUtc,
                result: default,
                hasResult: false,
                state.FailureReason,
                new DurableOperationResultDeserializationFailure(
                    state.DurableOperationId,
                    resultTypeName,
                    message,
                    ex.GetType().FullName));
        }

        return new DurableOperationResult<TResult>(
            state.DurableOperationId,
            state.Status,
            state.UpdatedAtUtc,
            result,
            hasResult: true,
            state.FailureReason);
    }

    private static void ValidateOperationId(Guid durableOperationId)
    {
        if (durableOperationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Durable operation id must not be empty.",
                nameof(durableOperationId));
        }
    }

    private static void ValidatePositive(
        TimeSpan value,
        string parameterName)
    {
        if (value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "The duration must be greater than zero.");
        }
    }
}
