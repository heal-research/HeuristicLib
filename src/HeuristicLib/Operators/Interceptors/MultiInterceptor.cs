using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

[Equatable]
public abstract partial record MultiInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState, TExecutionState>
  : IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    [OrderedEquality] protected ImmutableArray<IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>> InnerInterceptors { get; }

    protected delegate TSearchState InnerTransform(TSearchState currentState, TSearchState? previousState, TSearchSpace searchSpace, TProblem problem);

    protected MultiInterceptor(ImmutableArray<IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>> innerInterceptors)
    {
        InnerInterceptors = innerInterceptors;
    }

    public IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new Instance(this, InnerInterceptors.Select(instanceRegistry.Resolve).Select(x => (InnerTransform)x.Transform).ToArray(), CreateInitialState());

    protected abstract TExecutionState CreateInitialState();

    protected abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, TExecutionState executionState,
      IReadOnlyList<InnerTransform> innerInterceptors,
      TSearchSpace searchSpace, TProblem problem);

    private sealed class Instance(MultiInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState, TExecutionState> multiInterceptor,
      IReadOnlyList<InnerTransform> innerInterceptors, TExecutionState executionState)
      : IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    {
        public TSearchState Transform(TSearchState currentState, TSearchState? previousState, TSearchSpace searchSpace, TProblem problem)
        {
            return multiInterceptor.Transform(currentState, previousState, executionState, innerInterceptors, searchSpace, problem);
        }
    }
}

public abstract record MultiInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
  : MultiInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState, NoState>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected MultiInterceptor(ImmutableArray<IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>> innerInterceptors)
      : base(innerInterceptors)
    {
    }

    protected sealed override NoState CreateInitialState() => NoState.Instance;

    protected sealed override TSearchState Transform(TSearchState currentState, TSearchState? previousState,
      NoState executionState, IReadOnlyList<InnerTransform> innerInterceptors,
      TSearchSpace searchSpace, TProblem problem)
      => Transform(currentState, previousState, innerInterceptors, searchSpace, problem);

    protected abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState,
      IReadOnlyList<InnerTransform> innerInterceptors,
      TSearchSpace searchSpace, TProblem problem);
}
