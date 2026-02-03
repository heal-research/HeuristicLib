using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.OperatorExtensions.MeasuredOperators;

public static class MeasuredMutatorExtension {
  public static MeasuredMutator<TGenotype, TEncoding, TProblem> MeasureTime<TGenotype, TEncoding, TProblem>(this IMutator<TGenotype, TEncoding, TProblem> mutator)
    where TEncoding : class, IEncoding<TGenotype>
    where TProblem : class, IProblem<TGenotype, TEncoding> {
    return new MeasuredMutator<TGenotype, TEncoding, TProblem>(mutator);
  }
}
