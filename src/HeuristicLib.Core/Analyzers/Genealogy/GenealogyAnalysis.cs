using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Collections;
using HEAL.HeuristicLib.Observation;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analyzers.Genealogy;

// public static class GenealogyAnalysisExtensions {
//   extension<TG, TS, TP, TR, TA, TBuildSpec>(AlgorithmBuilder<TG, TS, TP, TR, TA, TBuildSpec> builder)
//     where TA : class, IAlgorithm<TG, TS, TP, TR>
//     where TS : class, ISearchSpace<TG>
//     where TP : class, IProblem<TG, TS>
//     where TR : PopulationIterationState<TG>
//     where TG : class
//     where TBuildSpec : AlgorithmBuildSpec<TG, TS, TP, TR>
//   {
//     public GenealogyAnalysis<TG> AddGenealogyAnalysis(IEqualityComparer<TG>? equality = null, bool saveSpace = false) {
//       var analysis = new GenealogyAnalysis<TG>(equality, saveSpace);
//       builder.AddRewriter(analysis);
//       //builder.AddAttachment(analysis);
//       return analysis;
//     }
//   }
// }

public static class GenealogyAnalysis {
  extension<TG, TS, TP, TR, TAlg, TBuildSpec>(AlgorithmBuilder<TG, TS, TP, TR, TAlg, TBuildSpec> builder) 
    where TG : class
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
    where TR : PopulationIterationState<TG>
    where TAlg : class, IAlgorithm<TG, TS, TP, TR>
    where TBuildSpec : AlgorithmBuildSpec<TG, TS, TP, TR> 
  {
    public GenealogyGraph<TG> AddGenealogyAnalysis(IEqualityComparer<TG>? equality = null, bool saveSpace = false) {
      GenealogyGraph<TG> graph = new(equality ?? EqualityComparer<TG>.Default);
      
      var universalRewriter = new ObserverRewriter<TG, TS, TP, TR, TBuildSpec>(
        iterationObserver: IterationObserver.Create<TG, TS, TP, TR>((currentState, previousState, searchSpace, problem) => {
          var ordered = currentState.Population.OrderBy(x => x.ObjectiveVector, problem.Objective.TotalOrderComparer).ToArray();
          graph.SetAsNewGeneration(ordered.Select(x => x.Genotype), saveSpace);
        }),
        crossoverObserver: CrossoverObserver.Create<TG, TS, TP>((parents, children, searchSpace, problem) => {
          foreach (var (parents1, child) in parents.Zip(children))
            graph.AddConnection([parents1.Item1, parents1.Item2], child);
        })
        // others
      );
      
      builder.AddRewriter(universalRewriter);

      return graph;
    }
  }
}

//
// public class GenealogyAnalysis<T>(IEqualityComparer<T>? equality = null, bool saveSpace = false) : AttachedAnalysis<T, PopulationIterationState<T>>
//   where T : class {
//   public readonly GenealogyGraph<T> Graph = new(equality ?? EqualityComparer<T>.Default);
//
//   public override void AfterCrossover(IReadOnlyList<T> res, IReadOnlyList<IParents<T>> parents, IRandomNumberGenerator random, ISearchSpace<T> searchSpace, IProblem<T, ISearchSpace<T>> problem) {
//     foreach (var (parents1, child) in parents.Zip(res))
//       Graph.AddConnection([parents1.Item1, parents1.Item2], child);
//   }
//
//   public override void AfterMutation(IReadOnlyList<T> res, IReadOnlyList<T> parent, IRandomNumberGenerator random, ISearchSpace<T> searchSpace, IProblem<T, ISearchSpace<T>> problem) {
//     foreach (var (parents1, child) in parent.Zip(res))
//       Graph.AddConnection([parents1], child);
//   }
//
//   public override void AfterInterception(PopulationIterationState<T> currentIterationState, PopulationIterationState<T>? previousIterationState, ISearchSpace<T> searchSpace, IProblem<T, ISearchSpace<T>> problem) {
//     var ordered = currentIterationState.Population.OrderBy(x => x.ObjectiveVector, problem.Objective.TotalOrderComparer).ToArray();
//     Graph.SetAsNewGeneration(ordered.Select(x => x.Genotype), saveSpace);
//   }
// }

// public class RankAnalysis<T>(IEqualityComparer<T>? equality = null) : GenealogyAnalysis<T>(equality) where T : class {
//   public List<List<double>> Ranks { get; } = [];
//
//   public override void AfterInterception(PopulationIterationState<T> currentIterationState, PopulationIterationState<T>? previousIterationState, ISearchSpace<T> searchSpace, IProblem<T, ISearchSpace<T>> problem) {
//     base.AfterInterception(currentIterationState, previousIterationState, searchSpace, problem);
//     RecordRanks(Graph, Ranks);
//   }
//
//   private static void RecordRanks<TGenotype>(GenealogyGraph<TGenotype> graph, List<List<double>> ranks) where TGenotype : notnull {
//     if (graph.Nodes.Count < 2)
//       return;
//     var line = graph.Nodes[^2].Values
//                     .Where(x => x.Layer == 0)
//                     .OrderBy(x => x.Rank)
//                     .Select(node => node.GetAllDescendants().Where(x => x.Rank >= 0).AverageOrNaN(x => x.Rank))
//                     .ToList();
//     if (line.Count > 0) {
//       ranks.Add(line);
//     }
//   }
// }
