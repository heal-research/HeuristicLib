using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators.Neighborhoods;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;

namespace HEAL.HeuristicLib.Tests.Experimental.Operators;

/// <summary>
/// The three move roles follow the same migration shape as the nine core roles, so they get the same resolution check:
/// an operator written for a search space and problem resolves over exactly those, and is reported over anything else.
/// </summary>
/// <remarks>
/// A neighborhood is where all three meet. It is authored against the search space and problem it is written for, and
/// it is itself all three move operators, which name only the candidate and the move. That lets a neighborhood be held
/// without repeating the triple.
/// </remarks>
public class MoveRoleResolutionTests
{
    private static readonly BoundedRealVectorSearchSpace SearchSpace = new(2, -1.0, 1.0);

    /// <summary>
    /// The move operators are operations, so a wrapper wraps their calls as it wraps those of the core operators. A
    /// neighborhood hands out itself for each of them, so a wrapper for one operation wraps the neighborhood's executor for
    /// that operation and leaves its other operations alone.
    /// </summary>
    [Fact]
    public void MoveOperators_AreWrappedByWrappersOfTheirOwnOperation()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 2));
        INeighborhood<RealVector, int> neighborhood = new ShiftNeighborhood();
        var (creations, applications, evaluations) = (new Counter(), new Counter(), new Counter());
        var root = ResolutionScope.Create();
        var scope = root
            .For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(problem);
        var random = RandomNumberGenerator.Create(1);
        var candidate = new RealVector(0.0, 0.0);

        var move = scope.Resolve(neighborhood.MoveCreator.Counted(creations)).Moves(candidate, random).First();
        scope.Resolve(neighborhood.MoveApplier.Counted(applications)).Apply(candidate, move, random);
        var evaluator = scope.Resolve(neighborhood.MoveEvaluator.Counted(evaluations));
        evaluator.Evaluate(new ObjectiveVector(0.0), candidate, move, random);
        evaluator.Evaluate(candidate, random);

        neighborhood.MoveEvaluator.ShouldBeSameAs(neighborhood);
        (root.Results.Result(creations).CurrentCount, root.Results.Result(applications).CurrentCount, root.Results.Result(evaluations).CurrentCount)
            .ShouldBe((1, 1, 2));
    }

    // One neighborhood performs all three operations with one executor. A substitution for one operation wraps its slot and
    // leaves the others holding the neighborhood.
    [Fact]
    public void MoveOperators_AreCountedThroughSubstitutionPerOperation()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 2));
        INeighborhood<RealVector, int> neighborhood = new ShiftNeighborhood();
        var user = new MoveUser(neighborhood.MoveCreator, neighborhood.MoveApplier, neighborhood.MoveEvaluator);

        var onlyEvaluations = user.Counting(neighborhood.MoveEvaluator, out _);
        onlyEvaluations.Creator.ShouldBeSameAs(neighborhood);
        onlyEvaluations.Applier.ShouldBeSameAs(neighborhood);
        onlyEvaluations.Evaluator.ShouldNotBeSameAs(neighborhood);

        var counted = user
            .Counting(neighborhood.MoveCreator, out var creations)
            .Counting(neighborhood.MoveApplier, out var applications)
            .Counting(neighborhood.MoveEvaluator, out var evaluations);
        var root = ResolutionScope.Create();
        var scope = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(problem);
        var random = RandomNumberGenerator.Create(1);
        var candidate = new RealVector(0.0, 0.0);

        var move = scope.Resolve(counted.Creator).Moves(candidate, random).First();
        scope.Resolve(counted.Applier).Apply(candidate, move, random);
        var evaluator = scope.Resolve(counted.Evaluator);
        evaluator.Evaluate(new ObjectiveVector(0.0), candidate, move, random);
        evaluator.Evaluate(candidate, random);

        (root.Results.Result(creations).CurrentCount, root.Results.Result(applications).CurrentCount, root.Results.Result(evaluations).CurrentCount).ShouldBe((1, 1, 2));
    }

    private sealed record MoveUser(
        IOperator<MoveCreation<int>, RealVector> Creator,
        IOperator<MoveApplication<int>, RealVector> Applier,
        IOperator<MoveEvaluation<int>, RealVector> Evaluator) : IConfigurationNode
    {
        public static Parameter<MoveUser, IOperator<MoveCreation<int>, RealVector>> CreatorParameter { get; } =
            new(nameof(Creator), static user => user.Creator, static (user, value) => user with { Creator = value });

        public static Parameter<MoveUser, IOperator<MoveApplication<int>, RealVector>> ApplierParameter { get; } =
            new(nameof(Applier), static user => user.Applier, static (user, value) => user with { Applier = value });

        public static Parameter<MoveUser, IOperator<MoveEvaluation<int>, RealVector>> EvaluatorParameter { get; } =
            new(nameof(Evaluator), static user => user.Evaluator, static (user, value) => user with { Evaluator = value });

        public IReadOnlyList<IParameter> GetParameters() => [CreatorParameter, ApplierParameter, EvaluatorParameter];
    }

    [Fact]
    public void BoundMoveOperators_ResolveOverTheirOwnSearchSpaceAndProblem()
    {
        INeighborhood<RealVector, int> neighborhood = new ShiftNeighborhood();
        var scope = ResolutionScope.Create().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(new TestFunctionProblem(new HEAL.HeuristicLib.Problems.TestFunctions.SphereFunction(1)));

        scope.Resolve(neighborhood.MoveCreator).ShouldNotBeNull();
        scope.Resolve(neighborhood.MoveApplier).ShouldNotBeNull();
        scope.Resolve(neighborhood.MoveEvaluator).ShouldNotBeNull();
    }

    [Fact]
    public void BoundMoveOperators_AreReportedOverAWiderProblem()
    {
        INeighborhood<RealVector, int> neighborhood = new ShiftNeighborhood();
        var scope = ResolutionScope.Create()
            .For<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>(new TestFunctionProblem(new HEAL.HeuristicLib.Problems.TestFunctions.SphereFunction(1)));

        scope.TryResolve(neighborhood.MoveCreator, out _, out var reason).ShouldBeFalse();
        reason.ShouldContain(nameof(ShiftNeighborhood));
        scope.TryResolve(neighborhood.MoveApplier, out _, out _).ShouldBeFalse();
        scope.TryResolve(neighborhood.MoveEvaluator, out _, out _).ShouldBeFalse();
    }

    [Fact]
    public void AResolvedMoveCreatorAndApplier_WalkTheNeighborhood()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 2));
        var neighborhood = new ShiftNeighborhood();
        var scope = ResolutionScope.Create().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(problem);
        var creator = scope.Resolve(((INeighborhood<RealVector, int>)neighborhood).MoveCreator);
        var applier = scope.Resolve(((INeighborhood<RealVector, int>)neighborhood).MoveApplier);
        var random = RandomNumberGenerator.Create(1);

        var candidate = new RealVector(0.0, 0.0);
        var moves = creator.Moves(candidate, random).ToList();

        moves.ShouldBe([0, 1]);
        applier.Apply(candidate, 1, random).ShouldBe(new RealVector(0.0, 0.5));
    }

    /// <summary>Written for one search space and problem; a move is the index to shift.</summary>
    private sealed record ShiftNeighborhood
        : Neighborhood<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, int>
    {
        public override IEnumerable<int> Moves(RealVector candidate, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
            Enumerable.Range(0, candidate.Count);

        public override RealVector Apply(RealVector candidate, int move, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem)
        {
            var shifted = candidate.ToArray();
            shifted[move] += 0.5;
            return RealVector.FromOwnedArray(shifted);
        }
    }
}
