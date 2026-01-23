using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.OperatorExtensions.MeasuredOperators;

public static class MeasuredMutatorExtension
{
  extension<TGenotype, TSearchSpace, TProblem>(IMutator<TGenotype, TSearchSpace, TProblem> mutator)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
  {
    public MeasuredMutator<TGenotype, TSearchSpace, TProblem> MeasureTime() => new(mutator);
  }

  extension<TG, TS, TP>(ICrossover<TG, TS, TP> crossover) where TS : class, ISearchSpace<TG> where TP : class, IProblem<TG, TS>
  {
    public MeasuredCrossover<TG, TS, TP> MeasureTime() => new(crossover);
  }
}
