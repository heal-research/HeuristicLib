using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.Composite;
using HEAL.HeuristicLib.Encodings.IntegerVectors;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.MetaOptimization;

public static class MetaOptimizationProblem
{
    public static MetaOptimizationProblem<TCandidate, TSearchSpace, TProblem, TSearchState> AsMetaProblem<TCandidate, TSearchSpace, TProblem, TSearchState>(this TProblem problem,
                                                                                      CompositeSearchSpace<RealVector, BoundedRealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace> searchSpace,
                                                                                      Func<CompositeGenotype<RealVector, IntegerVector>, IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>> algBuilder) where TCandidate : class where TSearchSpace : class, ISearchSpace<TCandidate> where TProblem : class, IProblem<TCandidate, TSearchSpace> where TSearchState : PopulationState<TCandidate> => new MetaOptimizationProblem<TCandidate, TSearchSpace, TProblem, TSearchState>(problem, searchSpace, algBuilder);
}

public class MetaOptimizationProblem<TCandidate, TSearchSpace, TProblem, TSearchState> :
  SingleSolutionProblem<CompositeGenotype<RealVector, IntegerVector>, CompositeSearchSpace<RealVector, BoundedRealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace>>
  where TCandidate : class
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : PopulationState<TCandidate>
{
    private readonly TProblem problem;
    private readonly Func<CompositeGenotype<RealVector, IntegerVector>, IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>> algBuilder;

    public MetaOptimizationProblem(TProblem problem,
                                   CompositeSearchSpace<RealVector, BoundedRealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace> searchSpace,
                                   Func<CompositeGenotype<RealVector, IntegerVector>, IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>> algBuilder) : base(problem.Objective, searchSpace)
    {
        this.problem = problem;
        this.algBuilder = algBuilder;
    }

    public override ObjectiveVector Evaluate(CompositeGenotype<RealVector, IntegerVector> solution, IRandomNumberGenerator random)
        => algBuilder(solution).Complete(problem, random).Population.MinBy(x => x.ObjectiveVector, Objective.TotalOrderComparer)?.ObjectiveVector ?? Objective.Worst;
}
