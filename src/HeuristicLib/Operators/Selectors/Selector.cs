using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

public abstract record Selector<TCandidate, TSearchSpace, TProblem, TExecutionState>
  : ISelector<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TExecutionState : class
{
    protected abstract TExecutionState CreateInitialState();

    protected abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population,
      ObjectiveDirections objective, int count, TExecutionState executionState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    public ISelectorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new SelectorInstance(this, CreateInitialState());

    private sealed class SelectorInstance(Selector<TCandidate, TSearchSpace, TProblem, TExecutionState> selector, TExecutionState executionState)
      : ISelectorInstance<TCandidate, TSearchSpace, TProblem>
    {
        public IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            return selector.Select(population, objective, count, executionState, random, searchSpace, problem);
        }
    }
}

public abstract record Selector<TCandidate, TSearchSpace, TExecutionState>
  : ISelector<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TExecutionState : class
{
    protected abstract TExecutionState CreateInitialState();

    protected abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population,
      ObjectiveDirections objective, int count, TExecutionState executionState, IRandomNumberGenerator random, TSearchSpace searchSpace);

    public ISelectorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new SelectorInstance(this, CreateInitialState());

    private sealed class SelectorInstance(Selector<TCandidate, TSearchSpace, TExecutionState> selector, TExecutionState executionState)
      : ISelectorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    {
        public IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem)
        {
            return selector.Select(population, objective, count, executionState, random, searchSpace);
        }
    }
}

public abstract record Selector<TCandidate, TExecutionState>
  : ISelector<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
  where TExecutionState : class
{
    protected abstract TExecutionState CreateInitialState();

    protected abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population,
      ObjectiveDirections objective, int count, TExecutionState executionState, IRandomNumberGenerator random);

    public ISelectorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new SelectorInstance(this, CreateInitialState());

    private sealed class SelectorInstance(Selector<TCandidate, TExecutionState> selector, TExecutionState executionState)
      : ISelectorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
    {
        public IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem)
        {
            return selector.Select(population, objective, count, executionState, random);
        }
    }
}

