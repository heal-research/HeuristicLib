using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

[Equatable]
public abstract partial record MultiReplacer<TGenotype, TSearchSpace, TProblem, TExecutionState>
  : IReplacer<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    [OrderedEquality] protected ImmutableArray<IReplacer<TGenotype, TSearchSpace, TProblem>> InnerReplacers { get; }

    protected delegate IReadOnlyList<Solution<TGenotype>> InnerReplace(IReadOnlyList<Solution<TGenotype>> previousPopulation, IReadOnlyList<Solution<TGenotype>> offspringPopulation, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    protected MultiReplacer(ImmutableArray<IReplacer<TGenotype, TSearchSpace, TProblem>> innerReplacers)
    {
        InnerReplacers = innerReplacers;
    }

    public IReplacerInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new Instance(this, InnerReplacers.Select(instanceRegistry.Resolve).Select(x => (InnerReplace)x.Replace).ToArray(), CreateInitialState());

    protected abstract TExecutionState CreateInitialState();

    protected abstract IReadOnlyList<Solution<TGenotype>> Replace(IReadOnlyList<Solution<TGenotype>> previousPopulation,
      IReadOnlyList<Solution<TGenotype>> offspringPopulation, Objective objective, int count, TExecutionState executionState,
      IReadOnlyList<InnerReplace> innerReplacers,
      IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    private sealed class Instance(MultiReplacer<TGenotype, TSearchSpace, TProblem, TExecutionState> multiReplacer,
      IReadOnlyList<InnerReplace> innerReplacers, TExecutionState executionState)
      : IReplacerInstance<TGenotype, TSearchSpace, TProblem>
    {
        public IReadOnlyList<Solution<TGenotype>> Replace(IReadOnlyList<Solution<TGenotype>> previousPopulation, IReadOnlyList<Solution<TGenotype>> offspringPopulation, Objective objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            return multiReplacer.Replace(previousPopulation, offspringPopulation, objective, count, executionState, innerReplacers, random, searchSpace, problem);
        }
    }
}

public abstract record MultiReplacer<TGenotype, TSearchSpace, TProblem>
  : MultiReplacer<TGenotype, TSearchSpace, TProblem, NoState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    protected MultiReplacer(ImmutableArray<IReplacer<TGenotype, TSearchSpace, TProblem>> innerReplacers)
      : base(innerReplacers)
    {
    }

    protected sealed override NoState CreateInitialState() => NoState.Instance;

    protected sealed override IReadOnlyList<Solution<TGenotype>> Replace(IReadOnlyList<Solution<TGenotype>> previousPopulation,
      IReadOnlyList<Solution<TGenotype>> offspringPopulation, Objective objective, int count, NoState executionState,
      IReadOnlyList<InnerReplace> innerReplacers,
      IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
      => Replace(previousPopulation, offspringPopulation, objective, count, innerReplacers, random, searchSpace, problem);

    protected abstract IReadOnlyList<Solution<TGenotype>> Replace(IReadOnlyList<Solution<TGenotype>> previousPopulation,
      IReadOnlyList<Solution<TGenotype>> offspringPopulation, Objective objective, int count,
      IReadOnlyList<InnerReplace> innerReplacers,
      IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}
