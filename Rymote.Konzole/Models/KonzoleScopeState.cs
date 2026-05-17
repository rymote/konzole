namespace Rymote.Konzole.Models;

public sealed class KonzoleScopeState
{
    private static readonly AsyncLocal<KonzoleScopeState?> CurrentScope = new();

    public KonzoleTag? Tag { get; init; }
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }

    public static KonzoleScopeState? Current => CurrentScope.Value;

    public static IDisposable Push(KonzoleScopeState scopeState)
    {
        KonzoleScopeState? previousScope = CurrentScope.Value;
        CurrentScope.Value = scopeState;
        return new PopOnDispose(previousScope);
    }

    private sealed class PopOnDispose : IDisposable
    {
        private readonly KonzoleScopeState? _previousScope;
        private bool _alreadyDisposed;

        public PopOnDispose(KonzoleScopeState? previousScope)
        {
            _previousScope = previousScope;
        }

        public void Dispose()
        {
            if (_alreadyDisposed) return;
            _alreadyDisposed = true;
            CurrentScope.Value = _previousScope;
        }
    }
}
