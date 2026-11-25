using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Refiners;

public class RefinerEvaluatorAccountingTests
{
    // Sharing follows the configuration reference. An equal copy is a separate configuration with its own accounting.
    [Fact]
    public void EvaluationAccountingFollowsTheSharedReference()
    {
        var shared = CreateEvaluator().LimitEvaluations(1);
        var separateButEqual = shared with { };
        var scope = ResolutionScope.Create();
        var problem = CreateProblem();

        scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(shared, problem).ShouldBeSameAs(scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(shared, problem));
        scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(shared, problem)
            .Evaluate([10], RandomNumberGenerator.Create(1));
        scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(shared, problem)
            .Evaluate([10], RandomNumberGenerator.Create(2)).ShouldBe([problem.Objective.Worst]);
        scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(separateButEqual, problem)
            .Evaluate([10], RandomNumberGenerator.Create(3)).ShouldNotBe([problem.Objective.Worst]);
        separateButEqual.ShouldBe(shared);
        scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(separateButEqual, problem).ShouldNotBeSameAs(scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(shared, problem));
    }

    // Composition example 2: one shared budget instance, so the comparison evaluations consume it.
    [Fact]
    public void WithASharedBudget_ComparisonEvaluationsConsumeTheAlgorithmsBudget()
    {
        var problem = CreateProblem();
        var sharedEvaluator = CreateEvaluator().LimitEvaluations(2);
        var scope = ResolutionScope.Create();
        var refiner = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(new AddOffsetRefiner(-5).CheckedForImprovement(sharedEvaluator), problem);
        var algorithmEvaluator = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(sharedEvaluator, problem);

        // One original and one refined candidate exhaust the budget of two.
        refiner.Refine([10], RandomNumberGenerator.Create(1));

        algorithmEvaluator.Evaluate([10], RandomNumberGenerator.Create(2))
            .ShouldBe([problem.Objective.Worst]);
    }

    // Composition example 1: the refiner keeps its own evaluator, so refinement costs nothing from the budget.
    [Fact]
    public void WithItsOwnEvaluator_RefinementLeavesTheAlgorithmsBudgetUntouched()
    {
        var problem = CreateProblem();
        var algorithmEvaluator = CreateEvaluator().LimitEvaluations(2);
        var scope = ResolutionScope.Create();
        var refiner = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(new AddOffsetRefiner(-5).CheckedForImprovement(CreateEvaluator().LimitEvaluations(2)), problem);
        var evaluatorExecutor = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(algorithmEvaluator, problem);

        refiner.Refine([10], RandomNumberGenerator.Create(1));

        evaluatorExecutor.Evaluate([10], RandomNumberGenerator.Create(2))
            .ShouldBe([new ObjectiveVector(10.0)]);
    }

    // Composition example 4: with the limit outside a cache hit still consumes budget, with the cache outside it never
    // reaches the limit.
    [Fact]
    public void WrapperOrder_DecidesWhetherCacheHitsConsumeTheBudget()
    {
        var problem = CreateProblem();
        var limitOutside = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(CreateEvaluator().Cached().LimitEvaluations(2), problem);
        var cacheOutside = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(CreateEvaluator().LimitEvaluations(2).Cached(), problem);

        for (var request = 0; request < 2; request++)
        {
            limitOutside.Evaluate([10], RandomNumberGenerator.Create(1));
            cacheOutside.Evaluate([10], RandomNumberGenerator.Create(1));
        }

        limitOutside.Evaluate([10], RandomNumberGenerator.Create(1))
            .ShouldBe([problem.Objective.Worst]);
        cacheOutside.Evaluate([10], RandomNumberGenerator.Create(1))
            .ShouldBe([new ObjectiveVector(10.0)]);
    }

    // Composition example 7: only the stage that needs objective information carries an evaluator.
    [Fact]
    public void InAPipeline_OnlyTheStageWithAnEvaluatorEvaluates()
    {
        var observed = CreateEvaluator();
        var refiner = PipelineRefiner.Create(
            new AddOffsetRefiner(-1),
            new AddOffsetRefiner(-5).CheckedForImprovement(observed));
        var scope = ResolutionScope.Create();
        var problem = CreateProblem();
        var executor = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(
            refiner.Counting(observed, out var evaluations, OperatorCountMetric.Candidates), problem);
        var counter = scope.Results.Result(evaluations);

        executor.Refine([10, 20], RandomNumberGenerator.Create(1));

        // Two candidates, evaluated once as supplied and once as refined, by the checking stage alone.
        counter.CurrentCount.ShouldBe(4);
    }

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
}
