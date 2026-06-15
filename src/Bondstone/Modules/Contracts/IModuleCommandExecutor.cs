using Bondstone.Messaging;

namespace Bondstone.Modules;

/// <summary>
/// Executes registered module commands through Bondstone's module command pipeline.
/// </summary>
public interface IModuleCommandExecutor
{
    /// <summary>
    /// Executes a command in the named module without durable receive metadata.
    /// </summary>
    /// <typeparam name="TCommand">The command type to execute.</typeparam>
    /// <param name="moduleName">The target module name.</param>
    /// <param name="command">The command instance.</param>
    /// <param name="ct">A cancellation token for command execution.</param>
    /// <returns>Execution metadata for the completed command pipeline.</returns>
    ValueTask<ModuleCommandExecutionResult> ExecuteAsync<TCommand>(
        string moduleName,
        TCommand command,
        CancellationToken ct = default)
        where TCommand : ICommand;

    /// <summary>
    /// Executes a command in the named module with durable receive metadata from an inbox pipeline.
    /// </summary>
    /// <typeparam name="TCommand">The command type to execute.</typeparam>
    /// <param name="moduleName">The target module name.</param>
    /// <param name="command">The command instance.</param>
    /// <param name="receiveContext">Durable receive metadata for inbox and operation-state behavior.</param>
    /// <param name="ct">A cancellation token for command execution.</param>
    /// <returns>Execution metadata for the completed command pipeline.</returns>
    ValueTask<ModuleCommandExecutionResult> ExecuteAsync<TCommand>(
        string moduleName,
        TCommand command,
        ModuleCommandReceiveContext receiveContext,
        CancellationToken ct = default)
        where TCommand : ICommand;

    /// <summary>
    /// Executes a result-returning command in the named module without durable receive metadata.
    /// </summary>
    /// <typeparam name="TResult">The result type produced by the command handler.</typeparam>
    /// <param name="moduleName">The target module name.</param>
    /// <param name="command">The command instance.</param>
    /// <param name="ct">A cancellation token for command execution.</param>
    /// <returns>Execution metadata and the typed result from the completed command pipeline.</returns>
    ValueTask<ModuleCommandExecutionResult<TResult>> ExecuteResultAsync<TResult>(
        string moduleName,
        ICommand<TResult> command,
        CancellationToken ct = default);

    /// <summary>
    /// Executes a result-returning command in the named module with durable receive metadata from an inbox pipeline.
    /// </summary>
    /// <typeparam name="TResult">The result type produced by the command handler.</typeparam>
    /// <param name="moduleName">The target module name.</param>
    /// <param name="command">The command instance.</param>
    /// <param name="receiveContext">Durable receive metadata for inbox and operation-state behavior.</param>
    /// <param name="ct">A cancellation token for command execution.</param>
    /// <returns>Execution metadata and the typed result from the completed command pipeline.</returns>
    ValueTask<ModuleCommandExecutionResult<TResult>> ExecuteResultAsync<TResult>(
        string moduleName,
        ICommand<TResult> command,
        ModuleCommandReceiveContext receiveContext,
        CancellationToken ct = default);

    /// <summary>
    /// Executes a command object in the named module without durable receive metadata.
    /// </summary>
    /// <param name="moduleName">The target module name.</param>
    /// <param name="command">The command object to route and execute.</param>
    /// <param name="ct">A cancellation token for command execution.</param>
    /// <returns>Execution metadata for the completed command pipeline.</returns>
    ValueTask<ModuleCommandExecutionResult> ExecuteAsync(
        string moduleName,
        object command,
        CancellationToken ct = default);

    /// <summary>
    /// Executes a command object in the named module with durable receive metadata from an inbox pipeline.
    /// </summary>
    /// <param name="moduleName">The target module name.</param>
    /// <param name="command">The command object to route and execute.</param>
    /// <param name="receiveContext">Durable receive metadata for inbox and operation-state behavior.</param>
    /// <param name="ct">A cancellation token for command execution.</param>
    /// <returns>Execution metadata for the completed command pipeline.</returns>
    ValueTask<ModuleCommandExecutionResult> ExecuteAsync(
        string moduleName,
        object command,
        ModuleCommandReceiveContext receiveContext,
        CancellationToken ct = default);
}
