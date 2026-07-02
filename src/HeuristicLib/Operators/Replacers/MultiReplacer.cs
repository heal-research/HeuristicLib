using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

[Equatable]
public abstract partial record MultiReplacer<TCandidate, TSearchSpace, TProblem, TExecutionState>
  : IReplacer<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    [OrderedEquality] protected ImmutableArray<IReplacer<TCandidate, TSearchSpace, TProblem>> InnerReplacers { get; }

    protected delegate IReadOnlyList<EvaluatedCandidate<TCandidate>> InnerReplace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    protected MultiReplacer(ImmutableArray<IReplacer<TCandidate, TSearchSpace, TProblem>> innerReplacers)
    {
        InnerReplacers = innerReplacers;
    }

    public IReplacerInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new Instance(this, InnerReplacers.Select(instanceRegistry.Resolve).Select(x => (InnerReplace)x.Replace).ToArray(), CreateInitialState());

    protected abstract TExecutionState CreateInitialState();

    protected abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation,
      IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, TExecutionState executionState,
      IReadOnlyList<InnerReplace> innerReplacers,
      IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    private sealed class Instance(MultiReplacer<TCandidate, TSearchSpace, TProblem, TExecutionState> multiReplacer,
      IReadOnlyList<InnerReplace> innerReplacers, TExecutionState executionState)
      : IReplacerInstance<TCandidate, TSearchSpace, TProblem>
    {
        public IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            return multiReplacer.Replace(previousPopulation, offspringPopulation, objective, count, executionState, innerReplacers, random, searchSpace, problem);
        }
    }
}

public abstract record MultiReplacer<TCandidate, TSearchSpace, TProblem>
  : MultiReplacer<TCandidate, TSearchSpace, TProblem, NoState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected MultiReplacer(ImmutableArray<IReplacer<TCandidate, TSearchSpace, TProblem>> innerReplacers)
      : base(innerReplacers)
    {
    }

    protected sealed override NoState CreateInitialState() => NoState.Instance;

    protected sealed override IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation,
      IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, NoState executionState,
      IReadOnlyList<InnerReplace> innerReplacers,
      IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
      => Replace(previousPopulation, offspringPopulation, objective, count, innerReplacers, random, searchSpace, problem);

    protected abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation,
      IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count,
      IReadOnlyList<InnerReplace> innerReplacers,
      IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}
