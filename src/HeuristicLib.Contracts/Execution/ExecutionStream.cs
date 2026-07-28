using System.Collections;

namespace HEAL.HeuristicLib.Execution;

public sealed class ExecutionStream<T> : IAsyncEnumerable<T>, IEnumerable<T>
{
    private readonly IAsyncEnumerable<T> source;
    private readonly CancellationToken cancellationToken;

    internal ExecutionStream(IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
    {
        this.source = source;
        this.cancellationToken = cancellationToken;
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => source.GetAsyncEnumerator(cancellationToken);

    public IEnumerator<T> GetEnumerator() => source.ToBlockingEnumerable(cancellationToken).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
