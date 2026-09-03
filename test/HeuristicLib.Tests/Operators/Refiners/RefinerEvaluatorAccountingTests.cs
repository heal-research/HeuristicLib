using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Refiners;

public class RefinerEvaluatorAccountingTests
{
    // One configuration object resolves to one execution instance, which is what makes one counter, budget and cache
    // shared.
    [Fact]
    public void EvaluationAccounting_FollowsConfigurationInstanceIdentityRatherThanEquality()
    {
        var registry = new ExecutionInstanceRegistry();
        var shared = CreateEvaluator().LimitEvaluations(1);
        var separateButEqual = CreateEvaluator().LimitEvaluations(1);

        registry.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(shared).ShouldBeSameAs(registry.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(shared));
        separateButEqual.ShouldBe(shared);
        registry.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(separateButEqual).ShouldNotBeSameAs(registry.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(shared));
    }

    // Composition example 2: one shared budget instance, so the comparison evaluations consume it.
    [Fact]
    public void WithASharedBudget_ComparisonEvaluationsConsumeTheAlgorithmsBudget()
    {
        var problem = CreateProblem();
        var sharedEvaluator = CreateEvaluator().LimitEvaluations(2);
        var registry = new ExecutionInstanceRegistry();
        var refiner = registry.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(new AddOffsetRefiner(-5).WithImprovementCheck(sharedEvaluator));
        var algorithmEvaluator = registry.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(sharedEvaluator);

        // One original and one refined candidate exhaust the budget of two.
        refiner.Refine([10], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        algorithmEvaluator.Evaluate([10], RandomNumberGenerator.Create(2), problem.SearchSpace, problem)
            .ShouldBe([problem.Objective.Worst]);
    }

    // Composition example 1: the refiner keeps its own evaluator, so refinement costs nothing from the budget.
    [Fact]
    public void WithItsOwnEvaluator_RefinementLeavesTheAlgorithmsBudgetUntouched()
    {
        var problem = CreateProblem();
        var algorithmEvaluator = CreateEvaluator().LimitEvaluations(2);
        var registry = new ExecutionInstanceRegistry();
        var refiner = registry.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(new AddOffsetRefiner(-5).WithImprovementCheck(CreateEvaluator().LimitEvaluations(2)));
        var evaluatorInstance = registry.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(algorithmEvaluator);

        refiner.Refine([10], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        evaluatorInstance.Evaluate([10], RandomNumberGenerator.Create(2), problem.SearchSpace, problem)
            .ShouldBe([new ObjectiveVector(10.0)]);
    }

    // Composition example 4: with the limit outside a cache hit still consumes budget, with the cache outside it never
    // reaches the limit.
    [Fact]
    public void WrapperOrder_DecidesWhetherCacheHitsConsumeTheBudget()
    {
        var problem = CreateProblem();
        var limitOutside = new ExecutionInstanceRegistry().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(CreateEvaluator().WithCache().LimitEvaluations(2));
        var cacheOutside = new ExecutionInstanceRegistry().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(CreateEvaluator().LimitEvaluations(2).WithCache());

        for (var request = 0; request < 2; request++)
        {
            limitOutside.Evaluate([10], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
            cacheOutside.Evaluate([10], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        }

        limitOutside.Evaluate([10], RandomNumberGenerator.Create(1), problem.SearchSpace, problem)
            .ShouldBe([problem.Objective.Worst]);
        cacheOutside.Evaluate([10], RandomNumberGenerator.Create(1), problem.SearchSpace, problem)
            .ShouldBe([new ObjectiveVector(10.0)]);
    }

    // Composition example 7: only the stage that needs objective information carries an evaluator.
    [Fact]
    public void InAPipeline_OnlyTheStageWithAnEvaluatorEvaluates()
    {
        var counter = new ObservationCounter();
        var instance = PipelineRefiner.Create(
                new AddOffsetRefiner(-1),
                new AddOffsetRefiner(-5).WithImprovementCheck(CreateEvaluator().CountEvaluatedCandidates(counter)))
            .CreateExecutionInstance<DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(new ExecutionInstanceRegistry());
        var problem = CreateProblem();

        instance.Refine([10, 20], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        // Two candidates, evaluated once as supplied and once as refined, by the checking stage alone.
        counter.CurrentCount.ShouldBe(4);
    }

    private static IEvaluator<int> CreateEvaluator() =>
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
}
