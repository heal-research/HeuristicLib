using System.Diagnostics;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.OperatorExtensions.MeasuredOperators;

public class MeasuredMutator<TGenotype, TSearchSpace, TProblem>(IMutator<TGenotype, TSearchSpace, TProblem> mutator) : IMutator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public OperatorMetric Metric { get; private set; }

  public IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TSearchSpace encoding, TProblem problem)
  {
    var start = Stopwatch.GetTimestamp();
    var offspring = mutator.Mutate(parent, random, encoding, problem);
    var end = Stopwatch.GetTimestamp();

    Metric += new OperatorMetric(offspring.Count, Stopwatch.GetElapsedTime(start, end));

    return offspring;
  }
}
