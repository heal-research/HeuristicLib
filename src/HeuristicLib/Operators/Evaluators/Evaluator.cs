using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public abstract class Evaluator<TGenotype, TSearchSpace, TProblem> : IEvaluator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace> {
  public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> solutions, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract class Evaluator<TGenotype, TSearchSpace> : IEvaluator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype> {
  public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> solutions, IRandomNumberGenerator random, TSearchSpace searchSpace);

  IReadOnlyList<ObjectiveVector> IEvaluator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Evaluate(IReadOnlyList<TGenotype> solutions, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) {
    return Evaluate(solutions, random, searchSpace);
  }
}

public abstract class Evaluator<TGenotype> : IEvaluator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> {
  public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> solutions, IRandomNumberGenerator random);

  IReadOnlyList<ObjectiveVector> IEvaluator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Evaluate(IReadOnlyList<TGenotype> solutions, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) {
    return Evaluate(solutions, random);
  }
}
