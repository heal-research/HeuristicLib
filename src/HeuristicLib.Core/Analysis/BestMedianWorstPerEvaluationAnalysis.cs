using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analysis;

public record BestMedianWorstPerEvaluationAnalysis<TGenotype, TSearchSpace, TProblem, TSearchState> : Analyzer<TGenotype, TSearchSpace, TProblem, TSearchState, BestMedianWorstPerEvaluationAnalysisState<TGenotype>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TSearchState : PopulationState<TGenotype>
{
  private IEvaluator<TGenotype, TSearchSpace, TProblem>[] Evaluators { get; }
  private IInterceptor<TGenotype, TSearchSpace, TProblem, TSearchState>[] Interceptors { get; }

  public BestMedianWorstPerEvaluationAnalysis(IAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState> Algorithm,
                                              IEvaluator<TGenotype, TSearchSpace, TProblem>[] Evaluators,
                                              IInterceptor<TGenotype, TSearchSpace, TProblem, TSearchState>[] Interceptors) : base(Algorithm)
  {
    this.Evaluators = Evaluators;
    this.Interceptors = Interceptors;
  }

  public override BestMedianWorstPerEvaluationAnalysisState<TGenotype> CreateInitialState() => new();

  public override void RegisterObservations(IObservationRegistry observationRegistry, BestMedianWorstPerEvaluationAnalysisState<TGenotype> state)
  {
    foreach (var evaluator in Evaluators) {
      observationRegistry.Add(evaluator, (genotypes, _, _, _) => state.AfterEvaluation(genotypes));
    }

    foreach (var interceptor in Interceptors) {
      observationRegistry.Add(interceptor, ((populationState, _, _, _, problem) => state.AfterInterception(populationState, problem.Objective)));
    }
  }
}

public sealed class BestMedianWorstPerEvaluationAnalysisState<TGenotype>
{
  private int currentEvaluationsCount;
  private readonly List<(int evaluations, BestMedianWorstEntry<TGenotype> entry)> bestSolutions = [];

  public IReadOnlyList<(int evaluations, BestMedianWorstEntry<TGenotype> entry)> BestSolutions => bestSolutions;

  public void AfterEvaluation(IReadOnlyList<TGenotype> genotypes)
  {
    currentEvaluationsCount += genotypes.Count;
  }

  public void AfterInterception(PopulationState<TGenotype> currentState, Objective objective)
  {
    if (currentState.Population.Solutions.Length == 0) {
      throw new InvalidOperationException("Population is empty, cannot determine best/median/worst solution.");
    }

    var comp = objective.TotalOrderComparer is NoTotalOrderComparer ? new LexicographicComparer(objective.Directions) : objective.TotalOrderComparer;
    var ordered = currentState.Population.OrderBy(keySelector: x => x.ObjectiveVector, comp).ToArray();

    bestSolutions.Add((currentEvaluationsCount, new BestMedianWorstEntry<TGenotype>(ordered[0], ordered[ordered.Length / 2], ordered[^1])));
  }
}
