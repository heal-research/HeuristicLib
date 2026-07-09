using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public abstract record WrappingInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState, TExecutionState>
  : IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected delegate TSearchState InnerTransform(TSearchState currentState, TSearchState? previousState, TSearchSpace searchSpace, TProblem problem);

    protected IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> InnerInterceptor { get; }

    protected WrappingInterceptor(IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> innerInterceptor)
    {
        InnerInterceptor = innerInterceptor;
    }

    public IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new Instance(this, instanceRegistry.Resolve(InnerInterceptor).Transform, CreateInitialState());

    protected abstract TExecutionState CreateInitialState();

    protected abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, TExecutionState executionState,
      InnerTransform innerTransform,
      TSearchSpace searchSpace, TProblem problem);

    private sealed class Instance(WrappingInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState, TExecutionState> wrappingInterceptor,
      InnerTransform innerTransform, TExecutionState executionState)
      : IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    {
        public TSearchState Transform(TSearchState currentState, TSearchState? previousState, TSearchSpace searchSpace, TProblem problem)
        {
            return wrappingInterceptor.Transform(currentState, previousState, executionState, innerTransform, searchSpace, problem);
        }
    }
}

public abstract record WrappingInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
  : WrappingInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState, NoState>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected WrappingInterceptor(IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> innerInterceptor)
      : base(innerInterceptor)
    {
    }

    protected sealed override NoState CreateInitialState() => NoState.Instance;

    protected sealed override TSearchState Transform(TSearchState currentState, TSearchState? previousState,
      NoState executionState, InnerTransform innerTransform,
      TSearchSpace searchSpace, TProblem problem)
      => Transform(currentState, previousState, innerTransform, searchSpace, problem);

    protected abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState,
      InnerTransform innerTransform,
      TSearchSpace searchSpace, TProblem problem);
}
