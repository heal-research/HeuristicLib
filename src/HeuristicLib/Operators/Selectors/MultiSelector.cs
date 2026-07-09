using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

[Equatable]
public abstract partial record MultiSelector<TCandidate, TSearchSpace, TProblem, TExecutionState>
  : ISelector<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    [OrderedEquality]
    protected ImmutableArray<ISelector<TCandidate, TSearchSpace, TProblem>> InnerSelectors { get; }

    protected delegate IReadOnlyList<EvaluatedCandidate<TCandidate>> InnerSelect(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    protected MultiSelector(ImmutableArray<ISelector<TCandidate, TSearchSpace, TProblem>> innerSelectors)
    {
        InnerSelectors = innerSelectors;
    }

    public ISelectorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new Instance(this, InnerSelectors.Select(instanceRegistry.Resolve).Select(x => (InnerSelect)x.Select).ToArray(), CreateInitialState());

    protected abstract TExecutionState CreateInitialState();

    protected abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population,
                                                                  ObjectiveDirections objective, int count, TExecutionState state, IReadOnlyList<InnerSelect> innerSelectors,
                                                                  IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    private sealed class Instance(
      MultiSelector<TCandidate, TSearchSpace, TProblem, TExecutionState> multiSelector,
      IReadOnlyList<InnerSelect> innerSelectors,
      TExecutionState executionState)
      : ISelectorInstance<TCandidate, TSearchSpace, TProblem>
    {
        public IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            return multiSelector.Select(population, objective, count, executionState, innerSelectors, random, searchSpace, problem);
        }
    }
}

public abstract record MultiSelector<TCandidate, TSearchSpace, TProblem>
  : MultiSelector<TCandidate, TSearchSpace, TProblem, NoState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected MultiSelector(ImmutableArray<ISelector<TCandidate, TSearchSpace, TProblem>> innerSelectors)
      : base(innerSelectors)
    { }

    protected sealed override NoState CreateInitialState() => NoState.Instance;

    protected sealed override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population,
                                                                         ObjectiveDirections objective, int count, NoState state,
                                                                         IReadOnlyList<InnerSelect> innerSelectors,
                                                                         IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
      => Select(population, objective, count, innerSelectors, random, searchSpace, problem);

    protected abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population,
                                                                  ObjectiveDirections objective, int count, IReadOnlyList<InnerSelect> innerSelectors,
                                                                  IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}
