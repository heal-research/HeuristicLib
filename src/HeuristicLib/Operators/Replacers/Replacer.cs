using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

public abstract record Replacer<TCandidate, TSearchSpace, TProblem, TExecutionState>
  : IReplacer<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TExecutionState : class
{
    protected abstract TExecutionState CreateInitialState();

    protected abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation,
      IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, TExecutionState executionState,
      IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    public IReplacerInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new ReplacerInstance(this, CreateInitialState());

    private sealed class ReplacerInstance(Replacer<TCandidate, TSearchSpace, TProblem, TExecutionState> replacer, TExecutionState executionState)
      : IReplacerInstance<TCandidate, TSearchSpace, TProblem>
    {
        public IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            return replacer.Replace(previousPopulation, offspringPopulation, objective, count, executionState, random, searchSpace, problem);
        }
    }
}

public abstract record Replacer<TCandidate, TSearchSpace, TExecutionState>
  : IReplacer<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TExecutionState : class
{
    protected abstract TExecutionState CreateInitialState();

    protected abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation,
      IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, TExecutionState executionState,
      IRandomNumberGenerator random, TSearchSpace searchSpace);

    public IReplacerInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new ReplacerInstance(this, CreateInitialState());

    private sealed class ReplacerInstance(Replacer<TCandidate, TSearchSpace, TExecutionState> replacer, TExecutionState executionState)
      : IReplacerInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    {
        public IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem)
        {
            return replacer.Replace(previousPopulation, offspringPopulation, objective, count, executionState, random, searchSpace);
        }
    }
}

public abstract record Replacer<TCandidate, TExecutionState>
  : IReplacer<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
  where TExecutionState : class
{
    protected abstract TExecutionState CreateInitialState();

    protected abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation,
      IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, TExecutionState executionState,
      IRandomNumberGenerator random);

    public IReplacerInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new ReplacerInstance(this, CreateInitialState());

    private sealed class ReplacerInstance(Replacer<TCandidate, TExecutionState> replacer, TExecutionState executionState)
      : IReplacerInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
    {
        public IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem)
        {
            return replacer.Replace(previousPopulation, offspringPopulation, objective, count, executionState, random);
        }
    }
}



