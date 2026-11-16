using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Refiners;

public class ImprovementCheckingRefinerTests
{
    [Fact]
    public void Refine_KeepsTheRefinedCandidateWhenItImproves()
    {
        // The problem minimizes the candidate value, so subtracting improves.
        var executor = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(new AddOffsetRefiner(-5).CheckedForImprovement(), CreateProblem());

        Refine(executor, 10, 20).ShouldBe([5, 15]);
    }

    [Fact]
    public void Refine_KeepsTheOriginalCandidateWhenRefinementMakesItWorse()
    {
        var executor = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(new AddOffsetRefiner(5).CheckedForImprovement(), CreateProblem());

        Refine(executor, 10, 20).ShouldBe([10, 20]);
    }

    // A refiner that cannot improve a candidate returns it unchanged, so failure never produces a worse candidate.
    [Fact]
    public void Refine_KeepsTheOriginalCandidateWhenTheRefinerChangesNothing()
    {
        var executor = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(NoChangeRefiner<int>.Instance.CheckedForImprovement(), CreateProblem());

        Refine(executor, 10, 20).ShouldBe([10, 20]);
    }

    [Fact]
    public void Refine_DecidesPerCandidateRatherThanPerBatch()
    {
        // Halving improves 10 but worsens -10, because the problem minimizes.
        var executor = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(new HalveRefiner().CheckedForImprovement(), CreateProblem());

        Refine(executor, 10, -10).ShouldBe([5, -10]);
    }

    [Fact]
    public void Refine_WithAnEmptyBatch_ReturnsAnEmptyResultWithoutEvaluating()
    {
        var observed = CreateEvaluator();
        var refiner = new AddOffsetRefiner(-5).CheckedForImprovement(observed);

        var scope = ResolutionScope.Create();
        Refine(scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(refiner.Counting(observed, out var counter), CreateProblem())).ShouldBeEmpty();

        scope.Results.Result(counter).CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void Refine_EvaluatesTheOriginalAndTheRefinedCandidates()
    {
        var observed = CreateEvaluator();
        var refiner = new AddOffsetRefiner(-5).CheckedForImprovement(observed);

        var scope = ResolutionScope.Create();
        Refine(scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(
            refiner.Counting(observed, out var counter, OperatorCountMetric.Candidates), CreateProblem()), 10, 20);

        scope.Results.Result(counter).CurrentCount.ShouldBe(4);
    }

    // Sharing one cache instance is what turns the algorithm's own evaluation of the returned candidate into a hit.
    [Fact]
    public void Refine_WithASharedCachingEvaluator_LetsALaterEvaluationHitTheCache()
    {
        var counter = new Counter();
        var observed = CreateEvaluator();
        var sharedEvaluator = observed.Counted(counter, OperatorCountMetric.Candidates).Cached();
        var refiner = new AddOffsetRefiner(-5).CheckedForImprovement(sharedEvaluator);
        var scope = ResolutionScope.Create();
        var problem = CreateProblem();
        var refinerExecutor = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(refiner, problem);
        var algorithmEvaluator = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(sharedEvaluator, problem);

        var accepted = refinerExecutor.Refine([10], RandomNumberGenerator.Create(1));
        algorithmEvaluator.Evaluate(accepted, RandomNumberGenerator.Create(2));

        accepted.ShouldBe([5]);
        scope.Results.Result(counter).CurrentCount.ShouldBe(2);
    }

    // Operator budgets install a counted replacement keyed by reference, so a refiner holding the same evaluator
    // instance resolves it too.
    [Fact]
    public void Refine_WithTheEvaluatorAnOperatorBudgetObserves_CountsItsComparisonEvaluations()
    {
        var sharedEvaluator = CreateEvaluator();
        var refiner = new AddOffsetRefiner(-5).CheckedForImprovement(sharedEvaluator);

        var budgetScope = ResolutionScope.Create().CreateChildScope();

        Refine(budgetScope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(
            refiner.Counting(sharedEvaluator, out var counter, OperatorCountMetric.Candidates), CreateProblem()), 10, 20);

        budgetScope.Results.Result(counter).CurrentCount.ShouldBe(4);
    }

    // The counterpart: a refiner with its own evaluator does not hold the algorithm's, so a budget on that one cannot reach it.
    [Fact]
    public void Refine_WithItsOwnEvaluator_KeepsComparisonEvaluationsOutOfTheBudget()
    {
        var algorithmEvaluator = CreateEvaluator();
        var refiner = new AddOffsetRefiner(-5).CheckedForImprovement();

        Should.Throw<InvalidOperationException>(() => refiner.Counting(algorithmEvaluator, out _, OperatorCountMetric.Candidates))
            .Message.ShouldContain("is not reachable");
    }

    [Fact]
    public void Refine_WithAThresholdCriterion_RejectsAnInsufficientImprovement()
    {
        var refiner = new AddOffsetRefiner(-1).CheckedForImprovement(ImprovementChecking.MinimumImprovement(5.0));

        Refine(ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(refiner, CreateProblem()), 10).ShouldBe([10]);

        var sufficient = new AddOffsetRefiner(-5).CheckedForImprovement(ImprovementChecking.MinimumImprovement(5.0));

        Refine(ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(sufficient, CreateProblem()), 10).ShouldBe([5]);
    }

    // A lateral move needs a candidate that differs while its objective vector does not: the problem minimizes the
    // absolute value and the refiner negates.
    [Fact]
    public void Refine_WithNotWorse_TakesALateralMoveThatStrictlyBetterRejects()
    {
        var problem = FuncProblem.Create(static (int candidate) => Math.Abs(candidate), DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var strict = new NegateRefiner().CheckedForImprovement(ImprovementChecking.StrictlyBetter);
        var notWorse = new NegateRefiner().CheckedForImprovement(ImprovementChecking.NotWorse);

        Refine(ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(strict, problem), 10).ShouldBe([10]);
        Refine(ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(notWorse, problem), 10).ShouldBe([-10]);
    }

    [Fact]
    public void Refine_WhenTheRefinerChangesTheBatchSize_Throws()
    {
        var executor = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(new DroppingRefiner().CheckedForImprovement(), CreateProblem());

        Should.Throw<InvalidOperationException>(() => Refine(executor, 10, 20));
    }

    [Fact]
    public void ImprovementCheckingRefiner_DefaultsToAProblemEvaluatorAndTheDefaultCriterion()
    {
        var refiner = new AddOffsetRefiner(-5).CheckedForImprovement();

        refiner.Evaluator.ShouldBe(new ProblemEvaluator<int>());
        refiner.Criterion.ShouldBe(ImprovementChecking.Default);
    }

    // Every combination of the two optional settings is reachable in one call, so no with expression is needed after it.
    [Fact]
    public void WithImprovementCheck_ReachesEverySettingCombinationInOneCall()
    {
        var child = new AddOffsetRefiner(-5);
        var evaluator = CreateEvaluator();
        var criterion = ImprovementChecking.NotWorse;
        var expected = new ProblemEvaluator<int>();

        child.CheckedForImprovement().Evaluator.ShouldBe(expected);
        child.CheckedForImprovement().Criterion.ShouldBe(ImprovementChecking.Default);

        child.CheckedForImprovement(criterion).Evaluator.ShouldBe(expected);
        child.CheckedForImprovement(criterion).Criterion.ShouldBe(criterion);

        child.CheckedForImprovement(evaluator).Evaluator.ShouldBeSameAs(evaluator);
        child.CheckedForImprovement(evaluator).Criterion.ShouldBe(ImprovementChecking.Default);

        child.CheckedForImprovement(evaluator, criterion).Evaluator.ShouldBeSameAs(evaluator);
        child.CheckedForImprovement(evaluator, criterion).Criterion.ShouldBe(criterion);
    }

    [Fact]
    public void Create_MatchesTheFluentOverloads()
    {
        var child = new AddOffsetRefiner(-5);
        var evaluator = CreateEvaluator();
        var criterion = ImprovementChecking.NotWorse;

        ImprovementCheckingRefiner.Create(child).ShouldBe(child.CheckedForImprovement());
        ImprovementCheckingRefiner.Create(child, criterion).ShouldBe(child.CheckedForImprovement(criterion));
        ImprovementCheckingRefiner.Create(child, evaluator).ShouldBe(child.CheckedForImprovement(evaluator));
        ImprovementCheckingRefiner.Create(child, evaluator, criterion).ShouldBe(child.CheckedForImprovement(evaluator, criterion));
    }

    [Fact]
    public void ImprovementCheckingRefiner_RetainsItsRefiner()
    {
        var child = new AddOffsetRefiner(-5);

        child.CheckedForImprovement().Refiner.ShouldBeSameAs(child);
    }

    [Fact]
    public void ImprovementCheckingRefiner_WithEqualSettings_IsEqual()
    {
        var left = new AddOffsetRefiner(-5).CheckedForImprovement();
        var right = new AddOffsetRefiner(-5).CheckedForImprovement();

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
        left.ShouldNotBe(new AddOffsetRefiner(-6).CheckedForImprovement());
        left.ShouldNotBe(new AddOffsetRefiner(-5).CheckedForImprovement(ImprovementChecking.NotWorse));
    }

    // The refiner reaches a better candidate only through a worse one: 10 becomes 20, and 20 becomes 5. Accepting each
    // round never gets there, while accepting once at the end keeps 5.
    [Fact]
    public void Refine_ComposedWithIteratedRefinerInBothOrders_SearchesDifferently()
    {
        var acceptEachRound = new UphillRefiner().CheckedForImprovement().AsIterated(3);
        var acceptOnceAtTheEnd = new UphillRefiner().AsIterated(3).CheckedForImprovement();

        Refine(ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(acceptEachRound, CreateProblem()), 10).ShouldBe([10]);
        Refine(ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(acceptOnceAtTheEnd, CreateProblem()), 10).ShouldBe([5]);
    }

    private static IReadOnlyList<int> Refine(IRefinementExecutor<int> executor, params int[] candidates) =>
        executor.Refine(candidates, RandomNumberGenerator.Create(42));

    private static IOperator<Evaluation, int> CreateEvaluator() =>
        new CandidateValueEvaluator();

    private static FuncProblem<int, DummySearchSpace<int>> CreateProblem() =>
        FuncProblem.Create(static (int candidate) => candidate, DummySearchSpace<int>.Instance, SingleObjective.Minimize);

    private sealed record CandidateValueEvaluator : SingleCandidateEvaluator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {
        public override ObjectiveVector EvaluateCandidate(int candidate, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, FuncProblem<int, DummySearchSpace<int>> problem) =>
            problem.Evaluate(candidate, random);
    }

    private sealed record AddOffsetRefiner(int Offset) : SingleCandidateRefiner<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {
        public override int RefineCandidate(int candidate, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, FuncProblem<int, DummySearchSpace<int>> problem) =>
            candidate + Offset;
    }

    private sealed record HalveRefiner : SingleCandidateRefiner<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {
        public override int RefineCandidate(int candidate, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, FuncProblem<int, DummySearchSpace<int>> problem) =>
            candidate / 2;
    }

    private sealed record NegateRefiner : SingleCandidateRefiner<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {
        public override int RefineCandidate(int candidate, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, FuncProblem<int, DummySearchSpace<int>> problem) =>
            -candidate;
    }

    // Reaches a better candidate only by passing through a worse one, so the two composition orders disagree.
    private sealed record UphillRefiner : SingleCandidateRefiner<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {
        public override int RefineCandidate(int candidate, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, FuncProblem<int, DummySearchSpace<int>> problem) =>
            candidate switch
            {
                10 => 20,
                20 => 5,
                _ => candidate
            };
    }

    private sealed record DroppingRefiner : StatelessRefiner<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {
        public override IReadOnlyList<int> Refine(IReadOnlyList<int> candidates, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, FuncProblem<int, DummySearchSpace<int>> problem) =>
            [.. candidates.Take(candidates.Count - 1)];
    }
}
