namespace Bondstone.Modules;

internal sealed class ModuleExecutionContextAccessor : IModuleExecutionContextAccessor
{
    private readonly AsyncLocal<Scope?> _current = new();

    public ModuleExecutionContext? Current => _current.Value?.Context;

    public IDisposable Push(ModuleExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scope = new Scope(this, context, _current.Value);
        _current.Value = scope;
        return scope;
    }

    internal IDisposable PushNoContext()
    {
        var scope = new Scope(this, context: null, _current.Value);
        _current.Value = scope;
        return scope;
    }

    private sealed class Scope(
        ModuleExecutionContextAccessor accessor,
        ModuleExecutionContext? context,
        Scope? previous)
        : IDisposable
    {
        private bool _disposed;

        public ModuleExecutionContext? Context { get; } = context;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            accessor._current.Value = previous;
            _disposed = true;
        }
    }
}
