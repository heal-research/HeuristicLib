//using HEAL.HeuristicLib.APIs.TreeSearchLib;
//using HEAL.HeuristicLib.Encodings.Vectors;
//using HEAL.HeuristicLib.Operators;
//using HEAL.HeuristicLib.Objectives;


//using HEAL.HeuristicLib.Problems.Partial;
//using HEAL.HeuristicLib.Problems.TravelingSalesman;
//using HEAL.HeuristicLib.Random;
//using HEAL.HeuristicLib.Encodings.Vectors;

//namespace HEAL.HeuristicLib.Tests.APIs.TreeSearchLib;

//using Operators.Neighborhoods.Specific;

//public sealed class TreeSearchLibExtensionTests
//{
//    [Fact]
//    public void AsSearchState_ShouldCreateBranchableTspState()
//    {
//        var problem = new PartialTspProblem();
//        var neighborhood = new Swap2Neighborhood();
//        var start = new Permutation(0, 1, 2, 3);

//        var state = problem.ToTreeSearch<Permutation, PermutationSearchSpace, PartialTspProblem>().From(neighborhood, start);

//        var state2 = state.WithLazyEvaluation();

//        state.Genotype.ShouldBe(start);
//        state.IsTerminal().ShouldBeTrue();
//        state.Quality().ShouldNotBeNull();

//        var moves = state.Branches().ToList();
//        moves.Count.ShouldBeGreaterThan(0);

//        var child = state.Branch(moves[0]);

//        child.Genotype.ShouldNotBe(start);
//        child.IsTerminal().ShouldBeTrue();
//        child.Quality().ShouldNotBeNull();
//    }

//    private sealed class PartialTspProblem()
//        : TravelingSalesmanProblem(new TravelingSalesmanCoordinatesData(Coordinates)),
//          IPartialSolutionProblem<Permutation, PermutationSearchSpace>
//    {
//        private static readonly double[,] Coordinates = { { 0, 0 }, { 0, 1 }, { 1, 1 }, { 1, 0 } };

//        private ObjectiveVector? EvaluatePartial(Permutation solution, IRandomNumberGenerator random) => solution.Count == SearchSpace.Length ? Evaluate(solution, random) : null;

//        public IReadOnlyList<bool> IsTerminal(IReadOnlyList<Permutation> genotypes, IRandomNumberGenerator random) => genotypes.Select(x => x.Count == SearchSpace.Length).ToArray();

//        public IReadOnlyList<ObjectiveVector?> EvaluatePartial(IReadOnlyList<Permutation> genotypes, IRandomNumberGenerator random) => genotypes.Select(x => EvaluatePartial(x, random)).ToArray();
//    }
//}
