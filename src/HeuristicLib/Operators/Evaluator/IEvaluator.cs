using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators.Evaluator;

public interface IEvaluator<in TGenotype, in TEncoding, in TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding> {
  IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, TEncoding encoding, TProblem problem);
}

public interface IEvaluator<TGenotype, in TEncoding> : IEvaluator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> where TEncoding : class, IEncoding<TGenotype>;
