namespace Alder;

/// <summary>
/// Global lifecycle holder for a shared <see cref="AlderEngine"/> instance.
/// Use <see cref="GetEngine"/> to access the configured engine.
/// </summary>
public static class AlderEval
{
    private static volatile AlderEngine? _engine;
    private static Action<AlderOptions>? _pendingConfigure;
    private static readonly object _lock = new();

    private enum State { Unconfigured, Configured, EngineCreated }
    private static State _state;

    /// <summary>
    /// Configures the global engine options.
    /// This must run before the first engine is created and can only succeed once.
    /// </summary>
    public static void Configure(Action<AlderOptions> configure)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        lock (_lock)
        {
            switch (_state)
            {
                case State.EngineCreated:
                    throw new InvalidOperationException(
                        "Cannot configure AlderEval after the global engine has been created. " +
                        "Call AlderEval.Configure() before the first AlderEval.GetEngine() call.");
                case State.Configured:
                    throw new InvalidOperationException(
                        "AlderEval.Configure() has already been called. " +
                        "Global configuration can only be set once.");
                default:
                    _pendingConfigure = configure;
                    _state = State.Configured;
                    break;
            }
        }
    }

    /// <summary>
    /// Resets the global engine, clearing configuration and cached state.
    /// This is primarily intended for testing and is not safe while evaluations are in flight.
    /// </summary>
    public static void Reset()
    {
        lock (_lock)
        {
            _engine?.Dispose();
            _engine = null;
            _pendingConfigure = null;
            _state = State.Unconfigured;
        }
    }

    /// <summary>
    /// Returns the shared global engine, creating it on first access.
    /// </summary>
    public static AlderEngine GetEngine()
    {
        var engine = _engine;
        if (engine != null)
            return engine;

        lock (_lock)
        {
            if (_engine != null)
                return _engine;

            _engine = _pendingConfigure != null
                ? new AlderEngine(_pendingConfigure)
                : new AlderEngine();

            _state = State.EngineCreated;
            return _engine;
        }
    }
}
