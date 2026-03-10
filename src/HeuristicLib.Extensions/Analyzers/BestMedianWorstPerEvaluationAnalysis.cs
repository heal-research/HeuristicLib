using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analyzers;

// ToDo: think about interceptor is the right hook here. Algorithm would be more intuitive but it is also more cumbersome.
public class BestMedianWorstPerEvaluationAnalysis<TGenotype>
  : IEvaluatorObserver<TGenotype>,
    IInterceptorObserver<TGenotype, PopulationState<TGenotype>>
// IAlgorithmObserver<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, PopulationState<TGenotype>>
{
  public sealed class Instance :
    IEvaluatorObserverInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>,
    IInterceptorObserverInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, PopulationState<TGenotype>>
  {
    private int currentEvaluationsCount;
    private readonly List<(int, BestMedianWorstEntry<TGenotype>)> bestSolutions = [];
    public IReadOnlyList<(int, BestMedianWorstEntry<TGenotype>)> BestSolutions => bestSolutions;

    public void AfterEvaluation(IReadOnlyList<TGenotype> genotypes, IReadOnlyList<ObjectiveVector> values, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
    {
      currentEvaluationsCount++;
    }

    public void AfterInterception(PopulationState<TGenotype> newState, PopulationState<TGenotype> currentState, PopulationState<TGenotype>? previousState, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
    {
      if (currentState.Population.Solutions.Length == 0)
        throw new InvalidOperationException("Population is empty, cannot determine best/median/worst solution.");

      var comp = problem.Objective.TotalOrderComparer is NoTotalOrderComparer ? new LexicographicComparer(problem.Objective.Directions) : problem.Objective.TotalOrderComparer;
      var ordered = currentState.Population.OrderBy(keySelector: x => x.ObjectiveVector, comp).ToArray();

      bestSolutions.Add((currentEvaluationsCount, new BestMedianWorstEntry<TGenotype>(ordered[0], ordered[ordered.Length / 2], ordered[^1])));
    }
  }

  IEvaluatorObserverInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
    IExecutable<IEvaluatorObserverInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>>
    .CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
    => new Instance();

  IInterceptorObserverInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, PopulationState<TGenotype>>
    IExecutable<IInterceptorObserverInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, PopulationState<TGenotype>>>
    .CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
    => new Instance();
}

public static class BestMedianWorstPerEvaluationAnalysis
{
  private static void CreateOperators<TGenotype, TSearchSpace, TProblem, TAlgorithmState>(
    IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator,
    IInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState> interceptor,
    out IEvaluator<TGenotype, TSearchSpace, TProblem> observedEvaluator,
    out IInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState> observedInterceptor,
    out BestMedianWorstPerEvaluationAnalysis<TGenotype> analysis)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TAlgorithmState : class, IAlgorithmState
  {
    analysis = new BestMedianWorstPerEvaluationAnalysis<TGenotype>();
    observedEvaluator = evaluator.ObserveWith(analysis);
    observedInterceptor = interceptor; // interceptor.ObserveWith((analysis);
  }

  extension<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm>(TAlgorithm algorithm)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TAlgorithmState : class, IAlgorithmState
    where TAlgorithm : IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  {
    public TAlgorithm AnalyzeBestMedianWorstPerEvaluation(
      IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator, IInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState> interceptor,
      Func<TAlgorithm, IEvaluator<TGenotype, TSearchSpace, TProblem>, IInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState>, TAlgorithm> algorithmAdapter,
      out BestMedianWorstPerEvaluationAnalysis<TGenotype> analysis)
    {
      CreateOperators(evaluator, interceptor, out var observedEvaluator, out var observedInterceptor, out analysis);
      return algorithmAdapter(algorithm, observedEvaluator, observedInterceptor);
    }
  }
}
