using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.Composite;
using HEAL.HeuristicLib.Encodings.IntegerVectors;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.MetaOptimization;

public static class MetaOptimizationProblem
{
    public static MetaOptimizationProblem<TCandidate, TSearchSpace, TProblem, TSearchState> AsMetaProblem<TCandidate, TSearchSpace, TProblem, TSearchState>(this TProblem problem,
                                                                                      CompositeSearchSpace<RealVector, BoundedRealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace> searchSpace,
                                                                                      Func<CompositeGenotype<RealVector, IntegerVector>, IAlgorithm<TCandidate>> algBuilder) where TCandidate : class where TSearchSpace : class, ISearchSpace<TCandidate> where TProblem : class, IProblem<TCandidate, TSearchSpace> where TSearchState : PopulationState<TCandidate> => new MetaOptimizationProblem<TCandidate, TSearchSpace, TProblem, TSearchState>(problem, searchSpace, algBuilder);
}

public class MetaOptimizationProblem<TCandidate, TSearchSpace, TProblem, TSearchState> :
  SingleSolutionProblem<MetaOptimizationProblem<TCandidate, TSearchSpace, TProblem, TSearchState>, CompositeGenotype<RealVector, IntegerVector>, CompositeSearchSpace<RealVector, BoundedRealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace>>
    where TCandidate : class
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : PopulationState<TCandidate>
{
    private readonly TProblem problem;
    private readonly Func<CompositeGenotype<RealVector, IntegerVector>, IAlgorithm<TCandidate>> algBuilder;

    public MetaOptimizationProblem(TProblem problem,
                                   CompositeSearchSpace<RealVector, BoundedRealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace> searchSpace,
                                   Func<CompositeGenotype<RealVector, IntegerVector>, IAlgorithm<TCandidate>> algBuilder) : base(problem.Objective, searchSpace)
    {
        this.problem = problem;
        this.algBuilder = algBuilder;
    }

    /// <remarks>
    /// The builder hands back an <see cref="IAlgorithm{TCandidate}"/>, which says nothing about the state a run
    /// yields, so the algorithm is resolved explicitly and its execution instance run.
    /// </remarks>
    public override ObjectiveVector Evaluate(CompositeGenotype<RealVector, IntegerVector> solution, IRandomNumberGenerator random)
    {
        var algorithm = new ExecutionInstanceRegistry().Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(algBuilder(solution));
        return algorithm.Complete(problem, random).Population.MinBy(x => x.ObjectiveVector, Objective.TotalOrderComparer)?.ObjectiveVector ?? Objective.Worst;
    }
}
