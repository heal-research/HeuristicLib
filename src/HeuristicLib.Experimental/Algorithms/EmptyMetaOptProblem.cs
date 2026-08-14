using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Algorithms;

public class EmptyMetaOptProblem : IProblem<CompositeGenotype<RealVector, IntegerVector>, CompositeSearchSpace<RealVector, RealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace>>
{
    public EmptyMetaOptProblem(CompositeSearchSpace<RealVector, RealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace> searchSpace)
    {
        SearchSpace = searchSpace;
    }

    public CompositeSearchSpace<RealVector, RealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace> SearchSpace { get; }

    public ObjectiveDirections Objective => throw new NotSupportedException("The empty meta optimization problem has no objective.");

    public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<CompositeGenotype<RealVector, IntegerVector>> candidates, IRandomNumberGenerator random) =>
        throw new NotSupportedException("The empty meta optimization problem does not support evaluation.");
}
