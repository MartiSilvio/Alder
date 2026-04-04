using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using Alder.Interpretation;

namespace Alder.Runtime;

internal sealed class IteratorEnumerable<T> : IEnumerable<T>, IEnumerable
{
    private readonly LambdaValue _lambda;
    private readonly object?[] _args;
    private readonly AlderContext _context;

    public IteratorEnumerable(LambdaValue lambda, object?[] args, AlderContext context)
    {
        _lambda = lambda;
        _args = args;
        _context = context;
    }

    public IEnumerator<T> GetEnumerator() => new IteratorEnumerator(_lambda, _args, _context);
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class IteratorEnumerator : IEnumerator<T>
    {
        private readonly SemaphoreSlim _producerGate = new(0, 1);
        private readonly SemaphoreSlim _consumerGate = new(0, 1);
        private readonly Thread _producerThread;

        private T _current = default!;
        private volatile bool _completed;
        private Exception? _exception;

        public IteratorEnumerator(LambdaValue lambda, object?[] args, AlderContext context)
        {
            _producerThread = new Thread(() => RunBody(lambda, args, context))
            {
                IsBackground = true,
                Name = "Alder-Iterator"
            };
            _producerThread.Start();
        }

        private void RunBody(LambdaValue lambda, object?[] args, AlderContext context)
        {
            _producerGate.Wait();
            try
            {
                var childContext = lambda.Closure.CreateChild();
                for (var i = 0; i < lambda.Parameters.Count && i < args.Length; i++)
                    childContext.Define(lambda.Parameters[i], args[i]);

                var bound = lambda.GetOrBindBody(childContext);
                var evaluator = new BoundEvaluator(childContext);

                evaluator.YieldCallback = value =>
                {
                    _current = (T)value!;
                    _consumerGate.Release();
                    _producerGate.Wait();
                    return !_completed;
                };

                evaluator.Evaluate(bound);
            }
            catch (Exception ex)
            {
                _exception = ex;
            }
            finally
            {
                _completed = true;
                try { _consumerGate.Release(); } catch (ObjectDisposedException) { }
            }
        }

        public T Current => _current;
        object? IEnumerator.Current => _current;

        public bool MoveNext()
        {
            if (_completed) return false;
            _producerGate.Release();
            _consumerGate.Wait();
            if (_exception != null)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(_exception).Throw();
            return !_completed;
        }

        public void Reset() => throw new NotSupportedException();

        public void Dispose()
        {
            if (!_completed)
            {
                _completed = true;
                try { _producerGate.Release(); } catch (SemaphoreFullException) { }
            }
            _producerGate.Dispose();
            _consumerGate.Dispose();
        }
    }
}

internal static class IteratorEnumerable
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> GenericMethodCache = new();

    private static readonly MethodInfo CreateMethod =
        typeof(IteratorEnumerable).GetMethod(nameof(CreateCore), BindingFlags.NonPublic | BindingFlags.Static)!;

    internal static object Create(LambdaValue lambda, object?[] args, AlderContext context)
    {
        var elementType = lambda.IteratorElementType ?? typeof(object);
        var method = GenericMethodCache.GetOrAdd(elementType, static t => CreateMethod.MakeGenericMethod(t));
        return method.Invoke(null, [lambda, args, context])!;
    }

    private static IteratorEnumerable<T> CreateCore<T>(LambdaValue lambda, object?[] args, AlderContext context)
        => new(lambda, args, context);
}
