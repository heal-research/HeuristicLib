using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Problems.MetaOptimization;

public static class MetaOptimizationProblem
{
  public static MetaOptimizationProblem<T, TE, TP, TS> AsMetaProblem<T, TE, TP, TS>(this TP problem,
                                                                                    CompositeSearchSpace<RealVector, RealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace> searchSpace,
                                                                                    Func<CompositeGenotype<RealVector, IntegerVector>, IAlgorithm<T, TE, TP, TS>> algBuilder) where T : class where TE : class, ISearchSpace<T> where TP : class, IProblem<T, TE> where TS : PopulationState<T> => new MetaOptimizationProblem<T, TE, TP, TS>(problem, searchSpace, algBuilder);
}

public class MetaOptimizationProblem<T, TE, TP, TS> :
  SingleSolutionProblem<CompositeGenotype<RealVector, IntegerVector>, CompositeSearchSpace<RealVector, RealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace>>
  where T : class
  where TE : class, ISearchSpace<T>
  where TP : class, IProblem<T, TE>
  where TS : PopulationState<T>
{
  private readonly TP problem;
  private readonly Func<CompositeGenotype<RealVector, IntegerVector>, IAlgorithm<T, TE, TP, TS>> algBuilder;

  public MetaOptimizationProblem(TP problem,
                                 CompositeSearchSpace<RealVector, RealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace> searchSpace,
                                 Func<CompositeGenotype<RealVector, IntegerVector>, IAlgorithm<T, TE, TP, TS>> algBuilder) : base(problem.Objective, searchSpace)
  {
    this.problem = problem;
    this.algBuilder = algBuilder;
  }

  public override ObjectiveVector Evaluate(CompositeGenotype<RealVector, IntegerVector> solution, IRandomNumberGenerator random)
    => algBuilder(solution).RunToCompletion(problem, random).Population.MinBy(x => x.ObjectiveVector, Objective.TotalOrderComparer)?.ObjectiveVector ?? Objective.Worst;
}
