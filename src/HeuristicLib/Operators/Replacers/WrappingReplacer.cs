using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

public abstract record WrappingReplacer<TCandidate, TSearchSpace, TProblem, TExecutionState>
  : IReplacer<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TExecutionState : class
{
    protected delegate IReadOnlyList<EvaluatedCandidate<TCandidate>> InnerReplace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    protected IReplacer<TCandidate, TSearchSpace, TProblem> InnerReplacer { get; }

    protected WrappingReplacer(IReplacer<TCandidate, TSearchSpace, TProblem> innerReplacer)
    {
        InnerReplacer = innerReplacer;
    }

    public IReplacerInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new Instance(this, instanceRegistry.Resolve(InnerReplacer).Replace, CreateInitialState());

    protected abstract TExecutionState CreateInitialState();

    protected abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation,
      IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, TExecutionState executionState,
      InnerReplace innerReplace, IRandomNumberGenerator random,
      TSearchSpace searchSpace, TProblem problem);

    private sealed class Instance(WrappingReplacer<TCandidate, TSearchSpace, TProblem, TExecutionState> wrappingReplacer,
      InnerReplace innerReplace, TExecutionState executionState)
      : IReplacerInstance<TCandidate, TSearchSpace, TProblem>
    {
        public IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            return wrappingReplacer.Replace(previousPopulation, offspringPopulation, objective, count, executionState, innerReplace, random, searchSpace, problem);
        }
    }
}

public abstract record WrappingReplacer<TCandidate, TSearchSpace, TProblem>
  : WrappingReplacer<TCandidate, TSearchSpace, TProblem, NoState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected WrappingReplacer(IReplacer<TCandidate, TSearchSpace, TProblem> innerReplacer)
      : base(innerReplacer)
    {
    }

    protected sealed override NoState CreateInitialState() => NoState.Instance;

    protected sealed override IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation,
      IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, NoState executionState,
      InnerReplace innerReplace, IRandomNumberGenerator random,
      TSearchSpace searchSpace, TProblem problem)
      => Replace(previousPopulation, offspringPopulation, objective, count, innerReplace, random, searchSpace, problem);

    protected abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation,
      IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count,
      InnerReplace innerReplace, IRandomNumberGenerator random,
      TSearchSpace searchSpace, TProblem problem);
}
