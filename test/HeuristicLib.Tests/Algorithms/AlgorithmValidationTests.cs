using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;

namespace HEAL.HeuristicLib.Tests.Algorithms;

/// <summary>
/// Pre-flight validation answers two questions, and this pins the second one: whether the configuration can be built
/// for the run at all, which is where problem compatibility is decided.
/// </summary>
public class AlgorithmValidationTests
{
    /// <summary>
    /// An operator written for one problem, run over a different one, is refused. The invariant walk cannot see this —
    /// it is handed only the search space — so it is the binding half that catches it.
    /// </summary>
    /// <remarks>
    /// The mismatch is a genuinely different problem: the same operator over a <see cref="TestFunctionProblem"/>
    /// binds, because the run supplies the problem the operator was written for.
    /// </remarks>
    [Fact]
    public void Validate_ReportsAnOperatorThatCannotBeBuiltForThisRunsProblem()
    {
        var problem = CreateDifferentProblem();
        var algorithm = CreateAlgorithm(new TestFunctionProblemSpecificCrossover());

        var report = algorithm.Validate(problem);

        report.IsValid.ShouldBeFalse();
        report.Diagnostics.ShouldContain(diagnostic => diagnostic.Message.Contains(nameof(TestFunctionProblemSpecificCrossover)));
    }

    [Fact]
    public void Validate_AcceptsAConfigurationEveryOperatorCanBeBuiltFor()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 3));
        var algorithm = CreateAlgorithm(new AlphaBetaBlendCrossover());

        algorithm.Validate(problem).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ValidateAndThrow_NamesTheOperatorThatCannotBeBuilt()
    {
        var problem = CreateDifferentProblem();
        var algorithm = CreateAlgorithm(new TestFunctionProblemSpecificCrossover());

        Should.Throw<InvalidOperationException>(() => algorithm.ValidateAndThrow(problem))
            .Message.ShouldContain(nameof(TestFunctionProblemSpecificCrossover));
    }

    /// <summary>A problem the crossover above was not written for.</summary>
    private static FuncProblem<RealVector, BoundedRealVectorSearchSpace> CreateDifferentProblem() =>
        FuncProblem.Create((RealVector candidate) => candidate[0] * candidate[0],
            new BoundedRealVectorSearchSpace(3, -5.0, 5.0), SingleObjective.Minimize);

    private static GeneticAlgorithm<RealVector>
        CreateAlgorithm(ICrossover<RealVector> crossover) =>
        new()
        {
            Creator = new UniformDistributedCreator(),
            Crossover = crossover,
            Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.1),
            PopulationSize = 8,
            MaximumGenerations = 2
        };

    private sealed record TestFunctionProblemSpecificCrossover
        : SingleCandidateCrossover<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override RealVector CrossParents(Parents<RealVector> parents, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
            RealVector.Repeat(problem.TestFunction.Dimension, parents.Parent1.Count);
    }
}
