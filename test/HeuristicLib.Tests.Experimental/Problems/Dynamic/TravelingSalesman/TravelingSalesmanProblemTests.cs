using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.Problems.Dynamic.Operators;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Tests.TestSupport.Random;

namespace HEAL.HeuristicLib.Tests.Problems.Dynamic.TravelingSalesman;

public class TravelingSalesmanProblemTests
{
    private static readonly double[,] D = { { 0, 1, 2, 3 }, { 1, 0, 4, 5 }, { 2, 4, 0, 6 }, { 3, 5, 6, 0 } };

    [Fact]
    public void Update_SwitchProbZero_StateUnchanged()
    {
        var data = new TravelingSalesmanDistanceMatrixProblemData(D);
        var env = RandomNumberGenerator.Create(0);
        var p = new ActivatedTravelingSalesmanProblem(data, env, [true, false, true, false], 0.0);

        var before = p.CurrentState.ToArray();
        p.UpdateOnce();
        p.CurrentState.ShouldBe(before);
    }

    [Fact]
    public void Evaluate_AllActive_EqualsFullCycleCost()
    {
        var data = new TravelingSalesmanDistanceMatrixProblemData(D);
        var env = RandomNumberGenerator.Create(0); // irrelevant for evaluation
        var p = new ActivatedTravelingSalesmanProblem(data, env, 1.0, 0.0);
        p.CurrentState.ShouldBe([true, true, true, true]);
        Permutation tour = [0, 1, 2, 3];
        var cost = p.Evaluate(tour, TestRandoms.NoRandom)[0];
        // Full cycle: 0->1->2->3->0 = 1 + 4 + 6 + 3 = 14
        cost.ShouldBe(14.0, 1e-10);
        p.UpdateOnce();
        var cost1 = p.Evaluate(tour, TestRandoms.NoRandom)[0];
        cost1.ShouldBe(14.0, 1e-10);
    }

    [Fact]
    public void Evaluate_SkipsInactiveCities_ReconnectsTour()
    {
        var data = new TravelingSalesmanDistanceMatrixProblemData(D);
        var env = RandomNumberGenerator.Create(0); // irrelevant for evaluation
        var p = new ActivatedTravelingSalesmanProblem(data, env, [true, false, true, true], 0.0);
        Permutation tour = [0, 1, 2, 3];
        var cost = p.Evaluate(tour, TestRandoms.NoRandom)[0];
        // 0->2 (2) + 2->3 (6) + 3->0 (3) = 11
        cost.ShouldBe(11.0, 1e-10);
        p.UpdateOnce();
        var cost1 = p.Evaluate(tour, env)[0];
        cost1.ShouldBe(11.0, 1e-10);
    }

    [Fact]
    public void ActiveCities_ExposesCurrentActiveCityIds()
    {
        var data = new TravelingSalesmanDistanceMatrixProblemData(D);
        var env = RandomNumberGenerator.Create(0);
        var p = new ActivatedTravelingSalesmanProblem(data, env, [true, false, true, true], 0.0);

        p.ActiveCities.ShouldBe([0, 2, 3]);
    }

    [Fact]
    public void CreateActiveSubproblemData_UsesCurrentActiveDistances()
    {
        var data = new TravelingSalesmanDistanceMatrixProblemData(D);
        var env = RandomNumberGenerator.Create(0);
        var p = new ActivatedTravelingSalesmanProblem(data, env, [true, false, true, true], 0.0);
        var subproblem = p.CreateActiveSubproblemData();

        subproblem.NumberOfCities.ShouldBe(3);
        subproblem.GetDistance(0, 1).ShouldBe(2.0);
        subproblem.GetDistance(1, 2).ShouldBe(6.0);
        subproblem.GetDistance(2, 0).ShouldBe(3.0);
    }

    [Fact]
    public void HeldKarpExactSolver_FindsBestTourForSmallInstance()
    {
        double[,] distances =
        {
            { 0, 1, 5, 1 },
            { 1, 0, 1, 5 },
            { 5, 1, 0, 1 },
            { 1, 5, 1, 0 }
        };
        var data = new TravelingSalesmanDistanceMatrixProblemData(distances);
        var solver = new HeldKarpTravelingSalesmanExactSolver();

        var solution = solver.Solve(data, [0, 1, 2, 3], TestContext.Current.CancellationToken);

        solution.Quality.ShouldBe(4.0, 1e-10);
        solution.Tour.Length.ShouldBe(4);
        CanonicalizeCycle(solution.Tour).ShouldBe(CanonicalizeCycle([0, 1, 2, 3]));
    }

    [Fact]
    public void ExactSolverExtension_SolvesActiveSubproblemInOriginalCityIds()
    {
        double[,] distances =
        {
            { 0, 1, 9, 5 },
            { 1, 0, 9, 2 },
            { 9, 9, 0, 9 },
            { 5, 2, 9, 0 }
        };
        var data = new TravelingSalesmanDistanceMatrixProblemData(distances);
        var env = RandomNumberGenerator.Create(0);
        var problem = new ActivatedTravelingSalesmanProblem(data, env, [true, true, false, true], 0.0);
        var solver = new HeldKarpTravelingSalesmanExactSolver();

        var solution = solver.Solve(problem, TestContext.Current.CancellationToken);

        solution.Quality.ShouldBe(8.0, 1e-10);
        CanonicalizeCycle(solution.Tour).ShouldBe(CanonicalizeCycle([0, 1, 3]));
    }

    [Fact]
    public void DynamicRelativeQualityEvaluator_RefreshesBestKnownOnEpochChange()
    {
        double[,] distances =
        {
            { 0, 1, 9, 1 },
            { 1, 0, 9, 1 },
            { 9, 9, 0, 9 },
            { 1, 1, 9, 0 }
        };
        var data = new TravelingSalesmanDistanceMatrixProblemData(distances);
        var env = RandomNumberGenerator.Create(0);
        var problem = new ActivatedTravelingSalesmanProblem(data, env, [true, true, true, false], 1.0);
        var evaluator = DirectEvaluator.For(problem).WithDynamicRelativeQuality(problem,
            new ActivatedTravelingSalesmanExactBestKnownProvider(new HeldKarpTravelingSalesmanExactSolver()));
        var instance = new ExecutionInstanceRegistry().Resolve(evaluator);

        var before = instance.Evaluate([[0, 1, 2, 3]], TestRandoms.NoRandom, problem.SearchSpace, problem)[0];
        problem.UpdateOnce();
        var after = instance.Evaluate([[0, 1, 2, 3]], TestRandoms.NoRandom, problem.SearchSpace, problem)[0];

        before.ShouldBe(new ObjectiveVector(0.0));
        after.ShouldBe(new ObjectiveVector(0.0));
    }

    [Fact]
    public void Evaluate_NoActiveCities_ReturnsZero()
    {
        var data = new TravelingSalesmanDistanceMatrixProblemData(D);
        var env = RandomNumberGenerator.Create(0); // irrelevant for evaluation
        var p = new ActivatedTravelingSalesmanProblem(data, env, [false, false, false, false], 0.0);
        Permutation tour = [0, 1, 2, 3];

        var cost = p.Evaluate(tour, TestRandoms.NoRandom)[0];
        cost.ShouldBe(0.0, 1e-10);
        p.UpdateOnce();
        var cost1 = p.Evaluate(tour, TestRandoms.NoRandom)[0];
        cost1.ShouldBe(0.0, 1e-10);
    }

    [Fact]
    public void Evaluate_OneActiveCity_ReturnsZero()
    {
        var data = new TravelingSalesmanDistanceMatrixProblemData(D);
        var env = RandomNumberGenerator.Create(0); // irrelevant for evaluation
        var p = new ActivatedTravelingSalesmanProblem(data, env, [false, true, false, false], 0.0);

        Permutation tour = [0, 1, 2, 3];

        var cost = p.Evaluate(tour, TestRandoms.NoRandom)[0];
        cost.ShouldBe(0.0, 1e-10);
        p.UpdateOnce();
        var cost1 = p.Evaluate(tour, TestRandoms.NoRandom)[0];
        cost1.ShouldBe(0.0, 1e-10);
    }

    [Fact]
    public void Update_SwitchProbOne_FlipsAllBits()
    {
        var data = new TravelingSalesmanDistanceMatrixProblemData(D);
        var env = RandomNumberGenerator.Create(0); // irrelevant for evaluation
        var p = new ActivatedTravelingSalesmanProblem(data, env, [true, false, true, false], 1.0);
        p.UpdateOnce();
        p.CurrentState.ShouldBe([false, true, false, true]);
    }

    [Fact]
    public void Evaluate_TwoActiveCities_IsTwoWayEdgeSum()
    {
        var data = new TravelingSalesmanDistanceMatrixProblemData(D);
        var env = RandomNumberGenerator.Create(0);
        var p = new ActivatedTravelingSalesmanProblem(data, env, [true, false, false, true], 0.0);

        Permutation tour = [0, 1, 2, 3];
        var cost = p.Evaluate(tour, TestRandoms.NoRandom)[0];

        // filtered tour: [0,3] => 0->3 (3) + 3->0 (3) = 6
        cost.ShouldBe(6.0, 1e-10);
    }

    [Fact]
    public void MyCachedTestCase()
    {
        var data = new TravelingSalesmanDistanceMatrixProblemData(D);
        var env = RandomNumberGenerator.Create(0);
        var p = new ActivatedTravelingSalesmanProblem(data, env, [true, false, false, true], 1.0, epochLength: 200);
        Permutation tour = [0, 1, 2, 3];
        var cachedEval = new ExecutionInstanceRegistry().Resolve(DirectEvaluator.For(p).WithCache());
        p.EpochClock.CurrentEpoch.ShouldBe(0);

        var r1 = cachedEval.Evaluate([tour], TestRandoms.NoRandom, p.SearchSpace, p)[0];
        var r2 = cachedEval.Evaluate([tour], TestRandoms.NoRandom, p.SearchSpace, p)[0];
        r2.ToArray().ShouldBe(r1.ToArray());

        p.UpdateOnce();
        p.EpochClock.CurrentEpoch.ShouldBe(1);

        var r3 = cachedEval.Evaluate([tour], TestRandoms.NoRandom, p.SearchSpace, p)[0];

        r3.ShouldNotBeNull();
    }

    private static int[] CanonicalizeCycle(IReadOnlyList<int> tour)
    {
        if (tour.Count == 0)
        {
            return [];
        }

        var forward = RotateSmallestCityToFront(tour);
        var reverse = RotateSmallestCityToFront(tour.Reverse().ToArray());

        return CompareLexicographically(forward, reverse) <= 0 ? forward : reverse;
    }

    private static int[] RotateSmallestCityToFront(IReadOnlyList<int> tour)
    {
        var start = 0;
        for (var i = 1; i < tour.Count; i++)
        {
            if (tour[i] < tour[start])
            {
                start = i;
            }
        }

        var rotated = new int[tour.Count];
        for (var i = 0; i < tour.Count; i++)
        {
            rotated[i] = tour[(start + i) % tour.Count];
        }

        return rotated;
    }

    private static int CompareLexicographically(IReadOnlyList<int> left, IReadOnlyList<int> right)
    {
        for (var i = 0; i < left.Count; i++)
        {
            var comparison = left[i].CompareTo(right[i]);
            if (comparison != 0)
            {
                return comparison;
            }
        }

        return 0;
    }
}
