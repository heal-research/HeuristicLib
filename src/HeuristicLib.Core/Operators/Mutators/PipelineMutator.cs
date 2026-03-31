using Generator.Equals;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

[Equatable]
public partial record PipelineMutator<TG, TS, TP> 
  : CompositeMutator<TG, TS, TP, NoState>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  [IgnoreEquality] public ImmutableArray<IMutator<TG, TS, TP>> Mutators => InnerMutators;

  public PipelineMutator(ImmutableArray<IMutator<TG, TS, TP>> mutators)
    : base(mutators)
  {
    // ToDo: think if we want to allow empty pipelines.
    if (mutators.Length == 0) {
      throw new ArgumentException("At least one crossover must be provided.", nameof(mutators));
    }
  }

  protected override NoState CreateInitialState() => NoState.Instance;
  
  protected override IReadOnlyList<TG> Mutate(IReadOnlyList<TG> parents, NoState _, IReadOnlyList<IMutatorInstance<TG, TS, TP>> mutators, IRandomNumberGenerator random, TS searchSpace, TP problem)
  {
    var current = parents;
    foreach (var mutator in mutators) {
      current = mutator.Mutate(current, random, searchSpace, problem);
    }
    return current;
  }
}
