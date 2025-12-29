using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Collections;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.AlgorithmExecutors;

// Todo: Proper Namespace for run-related stuff

public record RunConfiguration<TG, TS, TP, TR>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
{
  public IAlgorithm<TG, TS, TP, TR> Algorithm { get; }
  public TP Problem { get; }
  // Create own ObservablePipeline class?
  public ImmutableList<IIterationObserver<TG, TS, TP, TR>> Observers { get; }
  public IRandomNumberGenerator Random { get; }
  
  public RunConfiguration(IAlgorithm<TG, TS, TP, TR> algorithm, TP problem,
                          IEnumerable<IIterationObserver<TG, TS, TP, TR>>? observers = null,
                          IRandomNumberGenerator? random = null)
  {
    Algorithm = algorithm;
    Problem = problem;
    Observers = new ImmutableList<IIterationObserver<TG, TS, TP, TR>>(observers ?? []);
    Random = random ?? new SystemRandomNumberGenerator();
  }
}
