using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.OperatorExtensions.MeasuredOperators;

public static class MeasuredMutatorExtension {
  public static MeasuredMutator<TGenotype, TSearchSpace, TProblem> MeasureTime<TGenotype, TSearchSpace, TProblem>(this IMutator<TGenotype, TSearchSpace, TProblem> mutator)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace> {
    return new MeasuredMutator<TGenotype, TSearchSpace, TProblem>(mutator);
  }
}
