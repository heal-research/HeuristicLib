using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators.Neighborhoods;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;

namespace HEAL.HeuristicLib.Tests.Experimental.Operators;

/// <summary>
/// The three move roles follow the same migration shape as the nine core roles, so they get the same binding check:
/// an operator written for a search space and problem resolves over exactly those, and is reported over anything else.
/// </summary>
/// <remarks>
/// A neighborhood is where all three meet. It is authored against the search space and problem it is written for, and
/// it is itself all three move operators, which name only the candidate and the move. That lets a neighborhood be held
/// without repeating the triple.
/// </remarks>
public class MoveRoleBindingTests
{
    private static readonly BoundedRealVectorSearchSpace SearchSpace = new(2, -1.0, 1.0);

    /// <summary>
    /// The move operators are operations, so advice wraps their calls as it wraps those of the core operators. A
    /// neighborhood hands out itself for each of them, so advice selecting one operation reaches the neighborhood a run
    /// resolves and leaves its other operations alone.
    /// </summary>
    [Fact]
    public void MoveOperators_AreWrappedByAdviceOfTheirOwnOperation()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 2));
        INeighborhood<RealVector, int> neighborhood = new ShiftNeighborhood();
        var (creations, applications, evaluations) = (new CountAccumulator(), new CountAccumulator(), new CountAccumulator());
        var scope = ResolutionScope.Create(
                Aspect.CountCalls(neighborhood.MoveCreator, creations),
                Aspect.CountCalls(neighborhood.MoveApplier, applications),
                Aspect.CountCalls(neighborhood.MoveEvaluator, evaluations))
            .For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();
        var random = RandomNumberGenerator.Create(1);
        var candidate = new RealVector(0.0, 0.0);

        var move = scope.Resolve(neighborhood.MoveCreator).Moves(candidate, random, SearchSpace, problem).First();
        scope.Resolve(neighborhood.MoveApplier).Apply(candidate, move, random, SearchSpace, problem);
        var evaluator = scope.Resolve(neighborhood.MoveEvaluator);
        evaluator.Evaluate(new ObjectiveVector(0.0), candidate, move, random, SearchSpace, problem);
        evaluator.Evaluate(candidate, random, SearchSpace, problem);

        neighborhood.MoveEvaluator.ShouldBeSameAs(neighborhood);
        (creations.CurrentCount, applications.CurrentCount, evaluations.CurrentCount).ShouldBe((1, 1, 2));
    }

    [Fact]
    public void BoundMoveOperators_ResolveOverTheirOwnSearchSpaceAndProblem()
    {
        INeighborhood<RealVector, int> neighborhood = new ShiftNeighborhood();
        var scope = ResolutionScope.Create().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();

        scope.Resolve(neighborhood.MoveCreator).ShouldNotBeNull();
        scope.Resolve(neighborhood.MoveApplier).ShouldNotBeNull();
        scope.Resolve(neighborhood.MoveEvaluator).ShouldNotBeNull();
    }

    [Fact]
    public void BoundMoveOperators_AreReportedOverAWiderProblem()
    {
        INeighborhood<RealVector, int> neighborhood = new ShiftNeighborhood();
        var scope = ResolutionScope.Create()
            .For<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>();

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
        var scope = ResolutionScope.Create().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();
        var creator = scope.Resolve(((INeighborhood<RealVector, int>)neighborhood).MoveCreator);
        var applier = scope.Resolve(((INeighborhood<RealVector, int>)neighborhood).MoveApplier);
        var random = RandomNumberGenerator.Create(1);

        var candidate = new RealVector(0.0, 0.0);
        var moves = creator.Moves(candidate, random, SearchSpace, problem).ToList();

        moves.ShouldBe([0, 1]);
        applier.Apply(candidate, 1, random, SearchSpace, problem).ShouldBe(new RealVector(0.0, 0.5));
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
