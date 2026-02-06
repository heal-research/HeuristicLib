using HEAL.HeuristicLib.Observers;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analyzers;

public record BestMedianWorstEntry<T>(ISolution<T> Best, ISolution<T> Median, ISolution<T> Worst);

//public class MyAnalyzer<T> : IEvaluatorObserver<T> where T : class
//{
//  public void AfterEvaluation(IReadOnlyList<T> genotypes,
//                              IReadOnlyList<ObjectiveVector> values,
//                              ISearchSpace<T> searchSpace,
//                              IProblem<T, ISearchSpace<T>> problem)
//  {
//    throw new NotImplementedException();
//  }

//  //public static void test()
//  //{
//  //  var p = new TestFunctionProblem(new AckleyFunction(2));
//  //  IEvaluator<RealVector, RealVectorSearchSpace, TestFunctionProblem> eval = null!;
//  //  var countr = new MyAnalyzer<RealVector>();
//  //  IEvaluator<RealVector, RealVectorSearchSpace, TestFunctionProblem> wrapped = countr.WrapEvaluator(eval);
//  //  wrapped.Evaluate([new RealVector([1,2])], RandomNumberGenerator.Create(0, RandomProfile.NoRandom), p.SearchSpace, p);
//  //  IEvaluator<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>> x = eval;
//  //}
//}

public class BestMedianWorstAnalysis<TGenotype> : IInterceptorObserver<TGenotype, PopulationState<TGenotype>> where TGenotype : class
{
  private readonly List<BestMedianWorstEntry<TGenotype>> bestSolutions = [];
  
  public IReadOnlyList<BestMedianWorstEntry<TGenotype>> BestSolutions => bestSolutions;

  public void AfterInterception(PopulationState<TGenotype> currentAlgorithmState, PopulationState<TGenotype>? previousIterationResult, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
  {
    var comp = problem.Objective.TotalOrderComparer is NoTotalOrderComparer ? new LexicographicComparer(problem.Objective.Directions) : problem.Objective.TotalOrderComparer;
    var ordered = currentAlgorithmState.Population.OrderBy(keySelector: x => x.ObjectiveVector, comp).ToArray();
    if (ordered.Length == 0) {
      bestSolutions.Add(null!);

      return;
    }

    bestSolutions.Add(new BestMedianWorstEntry<TGenotype>(ordered[0], ordered[ordered.Length / 2], ordered[^1]));
  }
}
