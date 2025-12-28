using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Evaluator;

public abstract class BatchEvaluator<TGenotype, TEncoding, TProblem> : IEvaluator<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> solutions, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
}

public abstract class BatchEvaluator<TGenotype, TEncoding> : IEvaluator<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype> {
  public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> solutions, IRandomNumberGenerator random, TEncoding encoding);

  IReadOnlyList<ObjectiveVector> IEvaluator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Evaluate(IReadOnlyList<TGenotype> solutions, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Evaluate(solutions, random, encoding);
  }
}

public abstract class BatchEvaluator<TGenotype> : IEvaluator<TGenotype> {
  public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> solutions, IRandomNumberGenerator random);

  IReadOnlyList<ObjectiveVector> IEvaluator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Evaluate(IReadOnlyList<TGenotype> solutions, IRandomNumberGenerator random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return Evaluate(solutions, random);
  }

  IReadOnlyList<ObjectiveVector> IEvaluator<TGenotype, IEncoding<TGenotype>>.Evaluate(IReadOnlyList<TGenotype> solutions, IRandomNumberGenerator random, IEncoding<TGenotype> encoding) {
    return Evaluate(solutions, random);
  }
}
