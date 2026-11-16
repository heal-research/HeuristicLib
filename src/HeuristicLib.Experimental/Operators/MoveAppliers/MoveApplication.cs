using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies applying a move to a candidate when declaring an operator slot.</summary>
public sealed class MoveApplication<TMove> : IOperation<MoveApplication<TMove>>
{
    static bool IOperation<MoveApplication<TMove>>.IsExecutor<TCandidate, TSearchState>(IExecutor executor) =>
        executor is IMoveApplicationExecutor<TCandidate, TMove>;

    static IExecutor IOperation<MoveApplication<TMove>>.Wrap<TCandidate, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper) =>
        new WrappedExecutor<TCandidate, TWrapper>(executor, wrapper);

    private readonly struct Invocation<TCandidate>(IMoveApplicationExecutor<TCandidate, TMove> executor,
        TCandidate candidate, TMove move, IRandomNumberGenerator random) : IInvocation<TCandidate>
    {
        private readonly IMoveApplicationExecutor<TCandidate, TMove> executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;

        public TCandidate Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((IMoveApplicationExecutor<TCandidate, TMove>?)executor ?? this.executor).Apply(candidate, move, random ?? this.random);
    }

    private sealed class WrappedExecutor<TCandidate, TWrapper>(IExecutor executor, TWrapper wrapper)
        : WrappingExecutor<IMoveApplicationExecutor<TCandidate, TMove>, TWrapper>(executor, wrapper), IMoveApplicationExecutor<TCandidate, TMove>
        where TWrapper : struct, IInvocationWrapper
    {
        public TCandidate Apply(TCandidate candidate, TMove move, IRandomNumberGenerator random) =>
            Wrapper.Invoke<Invocation<TCandidate>, TCandidate>(new(Executor, candidate, move, random));
    }
}
