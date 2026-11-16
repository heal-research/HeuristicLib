using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Evaluators;

public class EvaluatorCompositionTests
{
    [Fact]
    public void CachingEvaluator_UsesIndependentCachePerExecution()
    {
        var observed = CreateEvaluator();
        var evaluator = observed.Cached().Counting(observed, out var counter);
        var firstScope = ResolutionScope.Create();
        var problem = CreateProblem();
        var firstExecutor = firstScope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator, problem);
        var secondScope = ResolutionScope.Create();
        var secondExecutor = secondScope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator, problem);

        firstExecutor.Evaluate([1], RandomNumberGenerator.Create(1));
        firstExecutor.Evaluate([1], RandomNumberGenerator.Create(2));
        secondExecutor.Evaluate([1], RandomNumberGenerator.Create(3));

        firstScope.Results.Result(counter).CurrentCount.ShouldBe(1);
        secondScope.Results.Result(counter).CurrentCount.ShouldBe(1);
    }

    [Fact]
    public void LimitEvaluator_UsesIndependentCounterPerExecution()
    {
        var observed = CreateEvaluator();
        var evaluator = observed.LimitEvaluations(2, enforceLimitWithinBatch: true).Counting(observed, out var counter, OperatorCountMetric.Candidates);
        var firstScope = ResolutionScope.Create();
        var problem = CreateProblem();
        var firstExecutor = firstScope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator, problem);
        var secondScope = ResolutionScope.Create();
        var secondExecutor = secondScope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator, problem);

        var limited = firstExecutor.Evaluate([1, 2, 3], RandomNumberGenerator.Create(1));
        secondExecutor.Evaluate([4], RandomNumberGenerator.Create(2));

        limited.Count.ShouldBe(3);
        firstScope.Results.Result(counter).CurrentCount.ShouldBe(2);
        secondScope.Results.Result(counter).CurrentCount.ShouldBe(1);
    }

    [Fact]
    public void RepeatingEvaluator_InvokesResolvedChildForEveryEvaluation()
    {
        var observed = CreateEvaluator();
        var evaluator = observed.AsRepeated(2, ObjectiveVectorAggregation.Mean).Counting(observed, out var counter);
        var scope = ResolutionScope.Create();
        var problem = CreateProblem();
        var executor = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator, problem);

        executor.Evaluate([1], RandomNumberGenerator.Create(1));

        scope.Results.Result(counter).CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void RelativeQualityEvaluator_NormalizesElementwise()
    {
        var evaluator = CreateEvaluator().ScaledToBestKnown(new ObjectiveVector(2.0, -4.0));
        var problem = new FuncProblem<int, DummySearchSpace<int>>(
            static candidate => new ObjectiveVector(candidate, -2.0 * candidate),
            DummySearchSpace<int>.Instance,
            MultiObjective.Minimize(2));
        var executor = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator, problem);

        var result = executor.Evaluate([3], RandomNumberGenerator.Create(1));

        result[0].ShouldBe(new ObjectiveVector(0.5, -0.5));
    }

    [Fact]
    public void RelativeQualityEvaluator_AppliesZeroBestKnownPolicy()
    {
        var evaluator = CreateEvaluator().ScaledToBestKnown(new ObjectiveVector(0.0),
            RelativeQualityZeroBestKnownPolicy.Difference);
        var problem = CreateProblem();
        var executor = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator, problem);

        var result = executor.Evaluate([3], RandomNumberGenerator.Create(1));

        result[0].ShouldBe(new ObjectiveVector(3.0));
    }

    private static IOperator<Evaluation, int> CreateEvaluator() =>
        new ProblemEvaluator();

    private static FuncProblem<int, DummySearchSpace<int>> CreateProblem() =>
        FuncProblem.Create(static (int candidate) => candidate, DummySearchSpace<int>.Instance, SingleObjective.Minimize);

    private sealed record ProblemEvaluator : SingleCandidateEvaluator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {
        public override ObjectiveVector EvaluateCandidate(int candidate, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, FuncProblem<int, DummySearchSpace<int>> problem) =>
            problem.Evaluate(candidate, random);
    }
}
