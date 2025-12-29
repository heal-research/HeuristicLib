using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public class PipelineMutator<TG, TS, TP> : BatchMutator<TG, TS, TP>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS> 
{
  public IReadOnlyList<IMutator<TG, TS, TP>> Mutators { get; }

  public PipelineMutator(IReadOnlyList<IMutator<TG, TS, TP>> mutators) {
    if (mutators.Count == 0)
      throw new ArgumentException("At least one crossover must be provided.", nameof(mutators));
    Mutators = mutators;
  }
  
  public override IReadOnlyList<TG> Mutate(IReadOnlyList<TG> parent, IRandomNumberGenerator random, TS searchSpace, TP problem) {
    IReadOnlyList<TG> current = parent;
    foreach (var mutator in Mutators) {
      current = mutator.Mutate(current, random, searchSpace, problem);
    }
    return current;
  }
}
