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
        var instance = new AddOffsetRefiner(-5).WithImprovementCheck().CreateExecutionInstance();

        Refine(instance, 10, 20).ShouldBe([5, 15]);
    }

    [Fact]
    public void Refine_KeepsTheOriginalCandidateWhenRefinementMakesItWorse()
    {
        var instance = new AddOffsetRefiner(5).WithImprovementCheck().CreateExecutionInstance();

        Refine(instance, 10, 20).ShouldBe([10, 20]);
    }

    // A refiner that cannot improve a candidate returns it unchanged, so failure never produces a worse candidate.
    [Fact]
    public void Refine_KeepsTheOriginalCandidateWhenTheRefinerChangesNothing()
    {
        var instance = NoChangeRefiner<int>.Instance.WithImprovementCheck().CreateExecutionInstance();

        Refine(instance, 10, 20).ShouldBe([10, 20]);
    }

    [Fact]
    public void Refine_DecidesPerCandidateRatherThanPerBatch()
    {
        // Halving improves 10 but worsens -10, because the problem minimizes.
        var instance = new HalveRefiner().WithImprovementCheck().CreateExecutionInstance();

        Refine(instance, 10, -10).ShouldBe([5, -10]);
    }

    [Fact]
    public void Refine_WithAnEmptyBatch_ReturnsAnEmptyResultWithoutEvaluating()
    {
        var counter = new ObservationCounter();
        var refiner = new AddOffsetRefiner(-5).WithImprovementCheck(CreateEvaluator().CountEvaluatorCalls(counter));

        Refine(refiner.CreateExecutionInstance()).ShouldBeEmpty();

        counter.CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void Refine_EvaluatesTheOriginalAndTheRefinedCandidates()
    {
        var counter = new ObservationCounter();
        var refiner = new AddOffsetRefiner(-5).WithImprovementCheck(CreateEvaluator().CountEvaluatedCandidates(counter));

        Refine(refiner.CreateExecutionInstance(), 10, 20);

        counter.CurrentCount.ShouldBe(4);
    }

    // Sharing one cache instance is what turns the algorithm's own evaluation of the returned candidate into a hit.
    [Fact]
    public void Refine_WithASharedCachingEvaluator_LetsALaterEvaluationHitTheCache()
    {
        var counter = new ObservationCounter();
        var sharedEvaluator = CreateEvaluator().CountEvaluatedCandidates(counter).WithCache();
        var refiner = new AddOffsetRefiner(-5).WithImprovementCheck(sharedEvaluator);
        var registry = new ExecutionInstanceRegistry();
        var refinerInstance = registry.Resolve(refiner);
        var algorithmEvaluator = registry.Resolve(sharedEvaluator);
        var problem = CreateProblem();

        var accepted = refinerInstance.Refine([10], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        algorithmEvaluator.Evaluate(accepted, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        accepted.ShouldBe([5]);
        counter.CurrentCount.ShouldBe(2);
    }

    // Operator budgets install a counted replacement keyed by reference, so a refiner holding the same evaluator
    // instance resolves it too.
    [Fact]
    public void Refine_WithTheEvaluatorAnOperatorBudgetObserves_CountsItsComparisonEvaluations()
    {
        var counter = new ObservationCounter();
        var sharedEvaluator = CreateEvaluator();
        var refiner = new AddOffsetRefiner(-5).WithImprovementCheck(sharedEvaluator);

        var budgetRegistry = new ExecutionInstanceRegistry().CreateChildRegistry();
        budgetRegistry.RegisterReplacement(sharedEvaluator, sharedEvaluator.CountEvaluatedCandidates(counter));

        Refine(budgetRegistry.Resolve(refiner), 10, 20);

        counter.CurrentCount.ShouldBe(4);
    }

    // The counterpart: a different evaluator instance is not reached by the budget's replacement.
    [Fact]
    public void Refine_WithItsOwnEvaluator_KeepsComparisonEvaluationsOutOfTheBudget()
    {
        var counter = new ObservationCounter();
        var algorithmEvaluator = CreateEvaluator();
        var refiner = new AddOffsetRefiner(-5).WithImprovementCheck();

        var budgetRegistry = new ExecutionInstanceRegistry().CreateChildRegistry();
        budgetRegistry.RegisterReplacement(algorithmEvaluator, algorithmEvaluator.CountEvaluatedCandidates(counter));

        Refine(budgetRegistry.Resolve(refiner), 10, 20);

        counter.CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void Refine_WithAThresholdCriterion_RejectsAnInsufficientImprovement()
    {
        var refiner = new AddOffsetRefiner(-1).WithImprovementCheck(ImprovementChecking.MinimumImprovement(5.0));

        Refine(refiner.CreateExecutionInstance(), 10).ShouldBe([10]);

        var sufficient = new AddOffsetRefiner(-5).WithImprovementCheck(ImprovementChecking.MinimumImprovement(5.0));

        Refine(sufficient.CreateExecutionInstance(), 10).ShouldBe([5]);
    }

    // A lateral move needs a candidate that differs while its objective vector does not: the problem minimizes the
    // absolute value and the refiner negates.
    [Fact]
    public void Refine_WithNotWorse_TakesALateralMoveThatStrictlyBetterRejects()
    {
        var problem = FuncProblem.Create(static (int candidate) => Math.Abs(candidate), DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var strict = new NegateRefiner().WithImprovementCheck(ImprovementChecking.StrictlyBetter);
        var notWorse = new NegateRefiner().WithImprovementCheck(ImprovementChecking.NotWorse);

        Refine(strict.CreateExecutionInstance(), problem, 10).ShouldBe([10]);
        Refine(notWorse.CreateExecutionInstance(), problem, 10).ShouldBe([-10]);
    }

    [Fact]
    public void Refine_WhenTheRefinerChangesTheBatchSize_Throws()
    {
        var instance = new DroppingRefiner().WithImprovementCheck().CreateExecutionInstance();

        Should.Throw<InvalidOperationException>(() => Refine(instance, 10, 20));
    }

    [Fact]
    public void ImprovementCheckingRefiner_DefaultsToAProblemEvaluatorAndTheDefaultCriterion()
    {
        var refiner = new AddOffsetRefiner(-5).WithImprovementCheck();

        refiner.Evaluator.ShouldBe(new ProblemEvaluator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>());
        refiner.Criterion.ShouldBe(ImprovementChecking.Default);
    }

    // Every combination of the two optional settings is reachable in one call, so no with expression is needed after it.
    [Fact]
    public void WithImprovementCheck_ReachesEverySettingCombinationInOneCall()
    {
        var child = new AddOffsetRefiner(-5);
        var evaluator = CreateEvaluator();
        var criterion = ImprovementChecking.NotWorse;
        var expected = new ProblemEvaluator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>();

        child.WithImprovementCheck().Evaluator.ShouldBe(expected);
        child.WithImprovementCheck().Criterion.ShouldBe(ImprovementChecking.Default);

        child.WithImprovementCheck(criterion).Evaluator.ShouldBe(expected);
        child.WithImprovementCheck(criterion).Criterion.ShouldBe(criterion);

        child.WithImprovementCheck(evaluator).Evaluator.ShouldBeSameAs(evaluator);
        child.WithImprovementCheck(evaluator).Criterion.ShouldBe(ImprovementChecking.Default);

        child.WithImprovementCheck(evaluator, criterion).Evaluator.ShouldBeSameAs(evaluator);
        child.WithImprovementCheck(evaluator, criterion).Criterion.ShouldBe(criterion);
    }

    [Fact]
    public void Create_MatchesTheFluentOverloads()
    {
        var child = new AddOffsetRefiner(-5);
        var evaluator = CreateEvaluator();
        var criterion = ImprovementChecking.NotWorse;

        ImprovementCheckingRefiner.Create(child).ShouldBe(child.WithImprovementCheck());
        ImprovementCheckingRefiner.Create(child, criterion).ShouldBe(child.WithImprovementCheck(criterion));
        ImprovementCheckingRefiner.Create(child, evaluator).ShouldBe(child.WithImprovementCheck(evaluator));
        ImprovementCheckingRefiner.Create(child, evaluator, criterion).ShouldBe(child.WithImprovementCheck(evaluator, criterion));
    }

    [Fact]
    public void ImprovementCheckingRefiner_RetainsItsRefiner()
    {
        var child = new AddOffsetRefiner(-5);

        child.WithImprovementCheck().Refiner.ShouldBeSameAs(child);
    }

    [Fact]
    public void ImprovementCheckingRefiner_WithEqualSettings_IsEqual()
    {
        var left = new AddOffsetRefiner(-5).WithImprovementCheck();
        var right = new AddOffsetRefiner(-5).WithImprovementCheck();

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
        left.ShouldNotBe(new AddOffsetRefiner(-6).WithImprovementCheck());
        left.ShouldNotBe(new AddOffsetRefiner(-5).WithImprovementCheck(ImprovementChecking.NotWorse));
    }

    // The refiner reaches a better candidate only through a worse one: 10 becomes 20, and 20 becomes 5. Accepting each
    // round never gets there, while accepting once at the end keeps 5.
    [Fact]
    public void Refine_ComposedWithIteratedRefinerInBothOrders_SearchesDifferently()
    {
        var acceptEachRound = new UphillRefiner().WithImprovementCheck().AsIterated(3);
        var acceptOnceAtTheEnd = new UphillRefiner().AsIterated(3).WithImprovementCheck();

        Refine(acceptEachRound.CreateExecutionInstance(), 10).ShouldBe([10]);
        Refine(acceptOnceAtTheEnd.CreateExecutionInstance(), 10).ShouldBe([5]);
    }

    private static IReadOnlyList<int> Refine(IRefinerInstance<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> instance, params int[] candidates) =>
        Refine(instance, CreateProblem(), candidates);

    private static IReadOnlyList<int> Refine(
        IRefinerInstance<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> instance,
        FuncProblem<int, DummySearchSpace<int>> problem,
        params int[] candidates) =>
        instance.Refine(candidates, RandomNumberGenerator.Create(42), problem.SearchSpace, problem);

    private static IEvaluator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateEvaluator() =>
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
