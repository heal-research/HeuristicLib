using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies transforming a search state using its previous state when declaring an operator slot.</summary>
public sealed class Interception : IOperationDescriptor<Interception>
{
    public static OperationExecutorAdapter CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        new ExecutorAdapter<TCandidate, TSearchSpace, TProblem, TSearchState>();

    private sealed class ExecutorAdapter<TCandidate, TSearchSpace, TProblem, TSearchState>
        : OperationExecutorAdapter<IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        protected override IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> CreateWrappedExecutor<TWrapper>(
            IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> executor, TWrapper wrapper) =>
            new WrappedExecutor<TWrapper>(executor, wrapper);

        private readonly struct Invocation(IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> executor,
            TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) : IInvocation<TSearchState>
        {
            private readonly IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> executor = executor;
            private readonly IRandomNumberGenerator random = random;

            public IRandomNumberGenerator Random => random;
            public TSearchState Proceed() => executor.Transform(currentState, previousState, random, searchSpace, problem);
            public TSearchState ProceedWith(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
                ((IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>?)executor ?? this.executor).Transform(currentState, previousState, random ?? this.random, searchSpace, problem);
        }

        private sealed class WrappedExecutor<TWrapper>(IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> executor, TWrapper wrapper)
            : IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>, IExecutorControlProvider
            where TWrapper : struct, IInvocationWrapper
        {
            public TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                wrapper.Invoke<Invocation, TSearchState>(new Invocation(executor, currentState, previousState, random, searchSpace, problem));

            public TControl? Get<TControl>() where TControl : class => ExecutorControls.Find<TControl, TWrapper>(executor, wrapper);
        }
    }
}
