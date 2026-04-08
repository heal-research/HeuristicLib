using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analysis;

public record QualityCurveAnalysis<T, TS, TP, TR> : Analyzer<T, TS, TP, TR, QualityCurve<T>>
  where TS : class, ISearchSpace<T>
  where TP : class, IProblem<T, TS>
  where TR : class, IAlgorithmState

{
  private IEvaluator<T, TS, TP>[] Evaluators { get; }

  public QualityCurveAnalysis(IAlgorithm<T, TS, TP, TR> Algorithm, params IEvaluator<T, TS, TP>[] Evaluators) : base(Algorithm)
  {
    this.Evaluators = Evaluators;
  }

  public void AfterEvaluation(QualityCurve<T> state, IReadOnlyList<T> genotypes, IReadOnlyList<ObjectiveVector> objectiveVectors, IProblem<T, ISearchSpace<T>> problem)
  {
    for (var i = 0; i < genotypes.Count; i++) {
      var genotype = genotypes[i];
      var q = objectiveVectors[i];
      state.EvalCount++;

      if (state.Best is not null) {
        var comp = problem.Objective.TotalOrderComparer;
        if (NoTotalOrderComparer.Instance.Equals(comp)) {
          comp = new LexicographicComparer(problem.Objective.Directions);
        }

        if (comp.Compare(q, state.Best.ObjectiveVector) >= 0) {
          continue;
        }
      }

      state.Add(new Solution<T>(genotype, q));
    }
  }

  public override QualityCurve<T> CreateInitialState() => new();

  public override void RegisterObservations(IObservationRegistry observationRegistry, QualityCurve<T> curve)
  {
    foreach (var evaluator in Evaluators) {
      observationRegistry.Add(evaluator, (genotypes, objectiveVectors, _, problem) => AfterEvaluation(curve, genotypes, objectiveVectors, problem));
    }
  }
}

public sealed class QualityCurve<TGenotype>
{
  private readonly List<(ISolution<TGenotype> best, int evalCount)> currentState = [];
  public IReadOnlyList<(ISolution<TGenotype> best, int evalCount)> CurrentState => currentState;

  public void Add(ISolution<TGenotype> solution)
  {
    Best = solution;
    currentState.Add((solution, EvalCount));
  }

  public int EvalCount { get; set; }
  public ISolution<TGenotype>? Best { get; private set; }
}
