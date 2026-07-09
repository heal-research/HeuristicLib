using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

public abstract record WrappingSelector<TCandidate, TSearchSpace, TProblem, TExecutionState>
  : ISelector<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TExecutionState : class
{
    protected delegate IReadOnlyList<EvaluatedCandidate<TCandidate>> InnerSelect(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    protected ISelector<TCandidate, TSearchSpace, TProblem> InnerSelector { get; }

    protected WrappingSelector(ISelector<TCandidate, TSearchSpace, TProblem> innerSelector)
    {
        InnerSelector = innerSelector;
    }

    public ISelectorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new Instance(this, instanceRegistry.Resolve(InnerSelector).Select, CreateInitialState());

    protected abstract TExecutionState CreateInitialState();

    protected abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population,
                                                                  ObjectiveDirections objective, int count, TExecutionState executionState, InnerSelect innerSelect,
                                                                  IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    private sealed class Instance(
      WrappingSelector<TCandidate, TSearchSpace, TProblem, TExecutionState> wrappingSelector,
      InnerSelect innerSelect,
      TExecutionState executionState) : ISelectorInstance<TCandidate, TSearchSpace, TProblem>
    {
        public IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            return wrappingSelector.Select(population, objective, count, executionState, innerSelect, random, searchSpace, problem);
        }
    }
}

public abstract record WrappingSelector<TCandidate, TSearchSpace, TProblem>
  : WrappingSelector<TCandidate, TSearchSpace, TProblem, NoState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected WrappingSelector(ISelector<TCandidate, TSearchSpace, TProblem> innerSelector)
      : base(innerSelector)
    { }

    protected sealed override NoState CreateInitialState() => NoState.Instance;

    protected sealed override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population,
                                                                         ObjectiveDirections objective, int count, NoState state, InnerSelect innerSelect,
                                                                         IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
      => Select(population, objective, count, innerSelect, random, searchSpace, problem);

    protected abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population,
                                                                  ObjectiveDirections objective, int count, InnerSelect innerSelect,
                                                                  IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}
