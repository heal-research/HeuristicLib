using System.Diagnostics;
using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.OperatorExtensions.MeasuredOperators;

public class MeasuredMutator<TGenotype, TEncoding, TProblem>(IMutator<TGenotype, TEncoding, TProblem> mutator) : IMutator<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public OperatorMetric Metric { get; private set; }

  public IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TEncoding encoding, TProblem problem) {
    var start = Stopwatch.GetTimestamp();
    var offspring = mutator.Mutate(parent, random, encoding, problem);
    var end = Stopwatch.GetTimestamp();

    Metric += new OperatorMetric(offspring.Count, Stopwatch.GetElapsedTime(start, end));

    return offspring;
  }
}
