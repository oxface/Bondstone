using System.Text.Json;
using Bondstone.Persistence;
using Bondstone.Utility;

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

    public async ValueTask<DurableOperationResult<TResult>> GetResultAsync<TResult>(
        Guid durableOperationId,
        string moduleName,
        CancellationToken ct = default)
    {
        ValidateOperationId(durableOperationId);
        string normalizedModuleName = moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name");

        DurableOperationState? state = await _operationReader.GetStateAsync(
            durableOperationId,
            normalizedModuleName,
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

    public async ValueTask<DurableOperationResult<TResult>> WaitForResultAsync<TResult>(
        Guid durableOperationId,
        string moduleName,
        TimeSpan timeout,
        TimeSpan? pollInterval = null,
        CancellationToken ct = default)
    {
        ValidateOperationId(durableOperationId);
        string normalizedModuleName = moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name");
        ValidatePositive(timeout, nameof(timeout));

        TimeSpan effectivePollInterval = pollInterval ?? DefaultPollInterval;
        ValidatePositive(effectivePollInterval, nameof(pollInterval));

        DateTimeOffset deadlineUtc = _timeProvider.GetUtcNow().Add(timeout);
        DurableOperationResult<TResult> lastResult;

        while (true)
        {
            lastResult = await GetResultAsync<TResult>(
                durableOperationId,
                normalizedModuleName,
                ct);

            if (lastResult.IsTerminal)
            {
                return lastResult;
            }

            TimeSpan remaining = deadlineUtc - _timeProvider.GetUtcNow();
            if (remaining <= TimeSpan.Zero)
            {
                throw new TimeoutException(
                    $"Durable operation '{durableOperationId}' in module '{normalizedModuleName}' did not reach a terminal state before timeout '{timeout}'.");
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
                failureReason: state.FailureReason,
                diagnosticContext: state.DiagnosticContext);
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
            string message = CreateDeserializationFailureMessage(
                state,
                resultTypeName,
                ex);

            return new DurableOperationResult<TResult>(
                state.DurableOperationId,
                state.Status,
                state.UpdatedAtUtc,
                result: default,
                hasResult: false,
                state.FailureReason,
                state.DiagnosticContext,
                new DurableOperationResultDeserializationFailure(
                    state.DurableOperationId,
                    resultTypeName,
                    message,
                    ex.GetType().FullName,
                    state.DiagnosticContext));
        }

        return new DurableOperationResult<TResult>(
            state.DurableOperationId,
            state.Status,
            state.UpdatedAtUtc,
            result,
            hasResult: true,
            state.FailureReason,
            state.DiagnosticContext);
    }

    private static string CreateDeserializationFailureMessage(
        DurableOperationState state,
        string resultTypeName,
        Exception exception)
    {
        string message =
            $"Durable operation '{state.DurableOperationId}' completed with a result payload, but the payload could not be deserialized as '{resultTypeName}'.";

        DurableOperationDiagnosticContext? context = state.DiagnosticContext;
        if (context?.ModuleName is not null)
        {
            message += $" Module '{context.ModuleName}'.";
        }

        if (context?.MessageTypeName is not null)
        {
            message += $" Message type '{context.MessageTypeName}'.";
        }

        if (context?.HandlerIdentity is not null)
        {
            message += $" Handler '{context.HandlerIdentity}'.";
        }

        return $"{message} {exception.GetType().Name}: {exception.Message}";
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
