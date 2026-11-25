using HEAL.HeuristicLib.Encodings.Composite;
using HEAL.HeuristicLib.Encodings.IntegerVectors;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms;

public class EmptyMetaOptProblem : IProblem<CompositeGenotype<RealVector, IntegerVector>, CompositeSearchSpace<RealVector, BoundedRealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace>>
{
    public EmptyMetaOptProblem(CompositeSearchSpace<RealVector, BoundedRealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace> searchSpace)
    {
        SearchSpace = searchSpace;
    }

    public CompositeSearchSpace<RealVector, BoundedRealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace> SearchSpace { get; }

    public ObjectiveDirections Objective => throw new NotSupportedException("The empty meta optimization problem has no objective.");

    public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<CompositeGenotype<RealVector, IntegerVector>> candidates, IRandomNumberGenerator random) =>
        throw new NotSupportedException("The empty meta optimization problem does not support evaluation.");
}
