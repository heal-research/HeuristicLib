using System.Diagnostics;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.OperatorExtensions.MeasuredOperators;

public class MeasuredCrossover<TGenotype, TSearchSpace, TProblem>(ICrossover<TGenotype, TSearchSpace, TProblem> crossover) : ICrossover<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace> 
{
  public OperatorMetric Metric { get; private set; } = new();
  
  public IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) {
    var start = Stopwatch.GetTimestamp();
    var offspring = crossover.Cross(parents, random, searchSpace, problem);
    var end = Stopwatch.GetTimestamp();

    Metric += new OperatorMetric(offspring.Count, Stopwatch.GetElapsedTime(start, end));

    return offspring;
  }
}
