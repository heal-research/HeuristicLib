using System;
using System.Collections.Generic;
using System.Text;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.MetaOptimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

using MetaOptimizationGenotype = CompositeGenotype<RealVector, IntegerVector>;
using MetaOptimizationSearchSpace = CompositeSearchSpace<RealVector, RealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace>;

public record DynamicRacingAlgorithm<TG,TS,TP, TA> : StatefulIterativeAlgorithm<TG, TS, TP, TA, DynamicRacingAlgorithm<TG, TS, TP, TA>.State> 
  where TS : class, ISearchSpace<TG> 
  where TP : class, IProblem<TG, TS>
  where TA : PopulationState<TG>
{

  public required ICreator<MetaOptimizationGenotype, MetaOptimizationSearchSpace, IProblem<MetaOptimizationGenotype, MetaOptimizationSearchSpace>> Creator { get; init; }
  public required IMutator<MetaOptimizationGenotype, MetaOptimizationSearchSpace, IProblem<MetaOptimizationGenotype, MetaOptimizationSearchSpace>> Mutator { get; init; }
  public required MetaOptimizationSearchSpace MetaSpace { get; init; }
  public required Func<MetaOptimizationGenotype, IProblem<MetaOptimizationGenotype, MetaOptimizationSearchSpace>> algBuilder { get; init; }

  public readonly int NoRacers;

  public class State
  {
    public State(ExecutionInstanceRegistry currentAlgorithm) {
      CurrentAlgorithm = currentAlgorithm;
    }
    public IAlgorithm<TG, TS, TP, TA>[] CurrentAlgorithm { get; set; }
  }

  protected override State CreateInitialRuntimeState() => new(Algorithm);

  protected override TA ExecuteStep(TA? previousState, State runtimeState, IOperatorExecutor executor, TP problem, IRandomNumberGenerator random)
  {
    Cr.
  }
}
