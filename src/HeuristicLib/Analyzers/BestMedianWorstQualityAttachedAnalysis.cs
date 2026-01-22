// using HEAL.HeuristicLib.Algorithms;
// using HEAL.HeuristicLib.Operators.Interceptors;
// using HEAL.HeuristicLib.Optimization;
// using HEAL.HeuristicLib.Problems;
// using HEAL.HeuristicLib.SearchSpaces;
// using HEAL.HeuristicLib.States;
//
// namespace HEAL.HeuristicLib.Analyzers;
//
// public class BestMedianWorstTrackingInterceptor<TG, TS, TP, TR, TA> : IInterceptor<TG, TR, TS, TP>
//   where TA : IAlgorithm<TG, TS, TP, TR>
//   where TS : class, ISearchSpace<TG>
//   where TP : class, IProblem<TG, TS>
//   where TR : PopulationIterationState<TG>
//   where TG : class 
// {
//   private readonly List<BestMedianWorstEntry<TG>> history = [];
//   public IReadOnlyList<BestMedianWorstEntry<TG>> History => history;
//   
//   public TR Transform(TR currentState, TR? previousState, TS searchSpace, TP problem) {
//     var ordered = currentState.Population.OrderBy(x => x.ObjectiveVector, problem.Objective.TotalOrderComparer).ToArray();
//     if (ordered.Length == 0) {
//       history.Add(default);
//     } else {
//       history.Add(new BestMedianWorstEntry<TG>(ordered[0], ordered[ordered.Length / 2], ordered[^1]));
//     }
//     return currentState;
//   }
// }
//
// public record BestMedianWorstEntry<T>(ISolution<T> Best, ISolution<T> Median, ISolution<T> Worst);
//
// public class BestMedianWorstQualityAnalyzer<TG, TS, TP, TR, TA, TBuilder> : IAnalyzer<TBuilder> 
//   where TBuilder : IAlgorithmBuilder<TG, TS, TP, TR, TA>, IHasEvaluator<TG, TS, TP, TR, TA>
//   where TS : class, ISearchSpace<TG>
//   where TP : class, IProblem<TG, TS>
//   where TR : PopulationIterationState<TG>
//   where TA : IAlgorithm<TG, TS, TP, TR>
//   where TG : class
// {
//   public void AddToBuilder(TBuilder builder) {
//     
//   }
// }
//
// public class BestMedianWorstQualityAttachedAnalysis<TGenotype> : 
//   AttachedAnalysis<TGenotype, PopulationIterationState<TGenotype>> 
//   where TGenotype : class 
// {
//   public readonly List<BestMedianWorstEntry<TGenotype>> BestISolutions = [];
//
//   public override void AfterInterception(PopulationIterationState<TGenotype> currentIterationState, PopulationIterationState<TGenotype>? previousIterationState, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) {
//     var ordered = currentIterationState.Population.OrderBy(x => x.ObjectiveVector, problem.Objective.TotalOrderComparer).ToArray();
//     if (ordered.Length == 0) {
//       BestISolutions.Add(default);
//       return;
//     }
//
//     BestISolutions.Add(new BestMedianWorstEntry<TGenotype>(ordered[0], ordered[ordered.Length / 2], ordered[^1]));
//   }
// }
//
// public static class BestMedianWorstAnalysis {
//   public static BestMedianWorstQualityAttachedAnalysis<TGenotype> Analyze<TGenotype, TS, TP, TR>(
//     IAlgorithmBuilder<TGenotype, TS, TP, TR> ga)
//     where TS : class, ISearchSpace<TGenotype>
//     where TP : class, IProblem<TGenotype, TS>
//     where TR : PopulationIterationState<TGenotype>
//     where TGenotype : class {
//     var r = new BestMedianWorstQualityAttachedAnalysis<TGenotype>();
//     ga.AddAttachment(r);
//     return r;
//   }
// }
