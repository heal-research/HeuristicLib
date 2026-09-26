using System.Collections;

namespace HEAL.HeuristicLib.Execution;

public sealed class ExecutionStream<T> : IAsyncEnumerable<T>, IEnumerable<T>
{
    private readonly IAsyncEnumerable<T> source;
    private readonly CancellationToken cancellationToken;
    private int consumed;

    internal ExecutionStream(IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
    {
        this.source = source;
        this.cancellationToken = cancellationToken;
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        Claim();
        return source.GetAsyncEnumerator(cancellationToken);
    }

    public IEnumerator<T> GetEnumerator()
    {
        Claim();
        return source.ToBlockingEnumerable(cancellationToken).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void Claim()
    {
        if (Interlocked.Exchange(ref consumed, 1) != 0)
            throw new InvalidOperationException("An execution stream can only be consumed once.");
    }
}
