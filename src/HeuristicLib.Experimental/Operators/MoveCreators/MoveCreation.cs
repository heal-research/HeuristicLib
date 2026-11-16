using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies creating the moves of a candidate when declaring an operator slot.</summary>
public sealed class MoveCreation<TMove> : IOperation<MoveCreation<TMove>>
{
    static bool IOperation<MoveCreation<TMove>>.IsExecutor<TCandidate, TSearchState>(IExecutor executor) =>
        executor is IMoveCreationExecutor<TCandidate, TMove>;

    static IExecutor IOperation<MoveCreation<TMove>>.Wrap<TCandidate, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper) =>
        new WrappedExecutor<TCandidate, TWrapper>(executor, wrapper);

    private readonly struct Invocation<TCandidate>(IMoveCreationExecutor<TCandidate, TMove> executor,
        TCandidate candidate, IRandomNumberGenerator random) : IInvocation<IEnumerable<TMove>>
    {
        private readonly IMoveCreationExecutor<TCandidate, TMove> executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;

        public IEnumerable<TMove> Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((IMoveCreationExecutor<TCandidate, TMove>?)executor ?? this.executor).Moves(candidate, random ?? this.random);
    }

    private sealed class WrappedExecutor<TCandidate, TWrapper>(IExecutor executor, TWrapper wrapper)
        : WrappingExecutor<IMoveCreationExecutor<TCandidate, TMove>, TWrapper>(executor, wrapper), IMoveCreationExecutor<TCandidate, TMove>
        where TWrapper : struct, IInvocationWrapper
    {
        public IEnumerable<TMove> Moves(TCandidate candidate, IRandomNumberGenerator random) =>
            Wrapper.Invoke<Invocation<TCandidate>, IEnumerable<TMove>>(new(Executor, candidate, random));
    }
}
