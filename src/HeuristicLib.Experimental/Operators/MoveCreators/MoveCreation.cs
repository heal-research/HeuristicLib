using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies creating the moves of a candidate when declaring an operator slot.</summary>
public sealed class MoveCreation<TMove> : IOperationDescriptor<MoveCreation<TMove>>
{
    public static OperationExecutorAdapter CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        new ExecutorAdapter<TCandidate, TSearchSpace, TProblem>();

    private sealed class ExecutorAdapter<TCandidate, TSearchSpace, TProblem>
        : OperationExecutorAdapter<IMoveCreationExecutor<TCandidate, TSearchSpace, TProblem, TMove>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        protected override IMoveCreationExecutor<TCandidate, TSearchSpace, TProblem, TMove> CreateWrappedExecutor<TWrapper>(
            IMoveCreationExecutor<TCandidate, TSearchSpace, TProblem, TMove> executor, TWrapper wrapper) =>
            new WrappedExecutor<TWrapper>(executor, wrapper);

        private readonly struct Invocation(IMoveCreationExecutor<TCandidate, TSearchSpace, TProblem, TMove> executor,
            TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) : IInvocation<IEnumerable<TMove>>
        {
            private readonly IMoveCreationExecutor<TCandidate, TSearchSpace, TProblem, TMove> executor = executor;
            private readonly IRandomNumberGenerator random = random;

            public IRandomNumberGenerator Random => random;
            public ISearchSpace SearchSpace => searchSpace;
            public IProblem Problem => problem;
            public IEnumerable<TMove> Proceed() => executor.Moves(candidate, random, searchSpace, problem);
            public IEnumerable<TMove> ProceedWith(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
                ((IMoveCreationExecutor<TCandidate, TSearchSpace, TProblem, TMove>?)executor ?? this.executor).Moves(candidate, random ?? this.random, searchSpace, problem);
        }

        private sealed class WrappedExecutor<TWrapper>(IMoveCreationExecutor<TCandidate, TSearchSpace, TProblem, TMove> executor, TWrapper wrapper)
            : IMoveCreationExecutor<TCandidate, TSearchSpace, TProblem, TMove>, IExecutorControlProvider
            where TWrapper : struct, IInvocationWrapper
        {
            public IEnumerable<TMove> Moves(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                wrapper.Invoke<Invocation, IEnumerable<TMove>>(new Invocation(executor, candidate, random, searchSpace, problem));

            public TControl? Get<TControl>() where TControl : class => ExecutorControls.Find<TControl, TWrapper>(executor, wrapper);
        }
    }
}
