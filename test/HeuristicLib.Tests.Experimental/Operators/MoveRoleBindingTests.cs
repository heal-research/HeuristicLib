using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators.MoveAppliers;
using HEAL.HeuristicLib.Operators.MoveCreators;
using HEAL.HeuristicLib.Operators.MoveEvaluators;
using HEAL.HeuristicLib.Operators.Neighborhoods;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.Experimental.Operators;

/// <summary>
/// The three move roles follow the same migration shape as the nine core roles, so they get the same binding check:
/// an operator written for a search space and problem resolves over exactly those, and is reported over anything else.
/// </summary>
/// <remarks>
/// A neighborhood is where all three meet. It is authored against the search space and problem it is written for, but
/// the three operators it hands out name only the candidate and the move, which is what lets a neighborhood be held
/// without repeating the triple.
/// </remarks>
public class MoveRoleBindingTests
{
    private static readonly BoundedRealVectorSearchSpace SearchSpace = new(2, -1.0, 1.0);

    [Fact]
    public void ANeighborhood_HandsOutOperatorsThatNameOnlyTheCandidateAndTheMove()
    {
        INeighborhood<RealVector, int> neighborhood = new ShiftNeighborhood();

        neighborhood.MoveCreator.ShouldBeAssignableTo<IMoveCreator<RealVector, int>>();
        neighborhood.MoveApplier.ShouldBeAssignableTo<IMoveApplier<RealVector, int>>();
        neighborhood.MoveEvaluator.ShouldBeAssignableTo<IMoveEvaluator<RealVector, int>>();
    }

    [Fact]
    public void BoundMoveOperators_ResolveOverTheirOwnSearchSpaceAndProblem()
    {
        INeighborhood<RealVector, int> neighborhood = new ShiftNeighborhood();
        var resolver = new ExecutionInstanceRegistry().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();

        resolver.Resolve(neighborhood.MoveCreator).ShouldNotBeNull();
        resolver.Resolve(neighborhood.MoveApplier).ShouldNotBeNull();
        resolver.Resolve(neighborhood.MoveEvaluator).ShouldNotBeNull();
    }

    [Fact]
    public void BoundMoveOperators_AreReportedOverAWiderProblem()
    {
        INeighborhood<RealVector, int> neighborhood = new ShiftNeighborhood();
        var resolver = new ExecutionInstanceRegistry()
            .For<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>();

        resolver.TryResolve(neighborhood.MoveCreator, out _, out var reason).ShouldBeFalse();
        reason.ShouldContain(nameof(TestFunctionProblem));
    }

    [Fact]
    public void AResolvedMoveCreatorAndApplier_WalkTheNeighborhood()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 2));
        var neighborhood = new ShiftNeighborhood();
        var resolver = new ExecutionInstanceRegistry().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();
        var creator = resolver.Resolve(((INeighborhood<RealVector, int>)neighborhood).MoveCreator);
        var applier = resolver.Resolve(((INeighborhood<RealVector, int>)neighborhood).MoveApplier);
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
