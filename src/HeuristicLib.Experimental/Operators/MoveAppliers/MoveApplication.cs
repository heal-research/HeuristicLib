using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies applying a move to a candidate when declaring an operator slot.</summary>
public sealed class MoveApplication<TMove> : IOperationDescriptor<MoveApplication<TMove>>
{
    public static OperationExecutorAdapter CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        new ExecutorAdapter<TCandidate, TSearchSpace, TProblem>();

    private sealed class ExecutorAdapter<TCandidate, TSearchSpace, TProblem>
        : OperationExecutorAdapter<IMoveApplicationExecutor<TCandidate, TSearchSpace, TProblem, TMove>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        protected override IMoveApplicationExecutor<TCandidate, TSearchSpace, TProblem, TMove> CreateWrappedExecutor<TWrapper>(
            IMoveApplicationExecutor<TCandidate, TSearchSpace, TProblem, TMove> executor, TWrapper wrapper) =>
            new WrappedExecutor<TWrapper>(executor, wrapper);

        private readonly struct Invocation(IMoveApplicationExecutor<TCandidate, TSearchSpace, TProblem, TMove> executor,
            TCandidate candidate, TMove move, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) : IInvocation<TCandidate>
        {
            private readonly IMoveApplicationExecutor<TCandidate, TSearchSpace, TProblem, TMove> executor = executor;
            private readonly IRandomNumberGenerator random = random;

            public IRandomNumberGenerator Random => random;
            public ISearchSpace SearchSpace => searchSpace;
            public IProblem Problem => problem;
            public TCandidate Proceed() => executor.Apply(candidate, move, random, searchSpace, problem);
            public TCandidate ProceedWith(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
                ((IMoveApplicationExecutor<TCandidate, TSearchSpace, TProblem, TMove>?)executor ?? this.executor).Apply(candidate, move, random ?? this.random, searchSpace, problem);
        }

        private sealed class WrappedExecutor<TWrapper>(IMoveApplicationExecutor<TCandidate, TSearchSpace, TProblem, TMove> executor, TWrapper wrapper)
            : IMoveApplicationExecutor<TCandidate, TSearchSpace, TProblem, TMove>, IExecutorControlProvider
            where TWrapper : struct, IInvocationWrapper
        {
            public TCandidate Apply(TCandidate candidate, TMove move, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                wrapper.Invoke<Invocation, TCandidate>(new Invocation(executor, candidate, move, random, searchSpace, problem));

            public TControl? Get<TControl>() where TControl : class => ExecutorControls.Find<TControl, TWrapper>(executor, wrapper);
        }
    }
}
