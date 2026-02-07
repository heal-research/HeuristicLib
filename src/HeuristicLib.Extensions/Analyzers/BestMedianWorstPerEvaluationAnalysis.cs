using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Observers;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analyzers;

// ToDo: think about interceptor is the right hook here. Algorithm would be more intuitive but it is also more cumbersome.
public class BestMedianWorstPerEvaluationAnalysis<TGenotype>
  : IEvaluatorObserver<TGenotype>, 
    IInterceptorObserver<TGenotype, PopulationState<TGenotype>>
    //IAlgorithmObserver<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, PopulationState<TGenotype>>
{
  private int currentEvaluationsCount = 0;
  private readonly List<(int, BestMedianWorstEntry<TGenotype>)> bestSolutions = [];
  public  IReadOnlyList<(int, BestMedianWorstEntry<TGenotype>)> BestSolutions => bestSolutions;
  
  public void AfterEvaluation(IReadOnlyList<TGenotype> genotypes, IReadOnlyList<ObjectiveVector> values, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
  {
    currentEvaluationsCount++;
  }
  
  public void AfterInterception(PopulationState<TGenotype> newState, PopulationState<TGenotype> currentState, PopulationState<TGenotype>? previousState, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
  {
    if (currentState.Population.Solutions.Count == 0) throw new InvalidOperationException("Population is empty, cannot determine best/median/worst solution.");
    
    var comp = problem.Objective.TotalOrderComparer is NoTotalOrderComparer ? new LexicographicComparer(problem.Objective.Directions) : problem.Objective.TotalOrderComparer;
    var ordered = currentState.Population.OrderBy(keySelector: x => x.ObjectiveVector, comp).ToArray();
  
    bestSolutions.Add((currentEvaluationsCount, new BestMedianWorstEntry<TGenotype>(ordered[0], ordered[ordered.Length / 2], ordered[^1])));
  }

  // public void AfterIteration(PopulationState<TGenotype> currentState, PopulationState<TGenotype>? previousState, IRandomNumberGenerator random, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
  // {
  //   if (currentState.Population.Solutions.Count == 0) throw new InvalidOperationException("Population is empty, cannot determine best/median/worst solution.");
  //   
  //   var comp = problem.Objective.TotalOrderComparer is NoTotalOrderComparer ? new LexicographicComparer(problem.Objective.Directions) : problem.Objective.TotalOrderComparer;
  //   var ordered = currentState.Population.OrderBy(keySelector: x => x.ObjectiveVector, comp).ToArray();
  //
  //   bestSolutions.Add((currentEvaluationsCount, new BestMedianWorstEntry<TGenotype>(ordered[0], ordered[ordered.Length / 2], ordered[^1])));
  // }
}

public static class BestMedianWorstPerEvaluationAnalysis
{
  public static void CreateOperators<TGenotype, TSearchSpace, TProblem, TAlgorithmState>(
    IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator, IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem> interceptor,
    out IEvaluator<TGenotype, TSearchSpace, TProblem> observedEvaluator, out IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem> observedInterceptor,
    out BestMedianWorstPerEvaluationAnalysis<TGenotype> analysis)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TAlgorithmState : class, IAlgorithmState
  {
    analysis = new BestMedianWorstPerEvaluationAnalysis<TGenotype>();
    observedEvaluator = evaluator.ObserveWith(analysis);
    observedInterceptor = interceptor;// interceptor.ObserveWith((analysis);
  }
  
  extension<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm>(TAlgorithm algorithm)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TAlgorithmState : class, IAlgorithmState
    where TAlgorithm : IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  {
    public TAlgorithm AnalyzeBestMedianWorstPerEvaluation(
      IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator, IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem> interceptor,
      Func<TAlgorithm, IEvaluator<TGenotype, TSearchSpace, TProblem>, IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>, TAlgorithm> algorithmAdapter,
      out BestMedianWorstPerEvaluationAnalysis<TGenotype> analysis)
    {
      CreateOperators(evaluator, interceptor, out var observedEvaluator, out var observedInterceptor, out analysis);
      return algorithmAdapter(algorithm, observedEvaluator, observedInterceptor);
    }
  }
}
