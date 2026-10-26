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
        var counter = new CountAccumulator();
        var observed = CreateEvaluator();
        var evaluator = observed.Cached();
        var firstExecutor = ResolutionScope.Create(Aspect.CountInvocations(observed, counter)).Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator);
        var secondExecutor = ResolutionScope.Create(Aspect.CountInvocations(observed, counter)).Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator);
        var problem = CreateProblem();

        firstExecutor.Evaluate([1], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        firstExecutor.Evaluate([1], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);
        secondExecutor.Evaluate([1], RandomNumberGenerator.Create(3), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void LimitEvaluator_UsesIndependentCounterPerExecution()
    {
        var counter = new CountAccumulator();
        var observed = CreateEvaluator();
        var evaluator = observed.LimitEvaluations(2, enforceLimitWithinBatch: true);
        var firstExecutor = ResolutionScope.Create(Aspect.CountCandidates(observed, counter)).Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator);
        var secondExecutor = ResolutionScope.Create(Aspect.CountCandidates(observed, counter)).Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator);
        var problem = CreateProblem();

        var limited = firstExecutor.Evaluate([1, 2, 3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        secondExecutor.Evaluate([4], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        limited.Count.ShouldBe(3);
        counter.CurrentCount.ShouldBe(3);
    }

    [Fact]
    public void RepeatingEvaluator_InvokesResolvedChildForEveryEvaluation()
    {
        var counter = new CountAccumulator();
        var observed = CreateEvaluator();
        var evaluator = observed.AsRepeated(2, ObjectiveVectorAggregation.Mean);
        var executor = ResolutionScope.Create(Aspect.CountInvocations(observed, counter)).Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator);
        var problem = CreateProblem();

        executor.Evaluate([1], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void RelativeQualityEvaluator_NormalizesElementwise()
    {
        var evaluator = CreateEvaluator().ScaledToBestKnown(new ObjectiveVector(2.0, -4.0));
        var executor = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator);
        var problem = new FuncProblem<int, DummySearchSpace<int>>(
            static candidate => new ObjectiveVector(candidate, -2.0 * candidate),
            DummySearchSpace<int>.Instance,
            MultiObjective.Minimize(2));

        var result = executor.Evaluate([3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        result[0].ShouldBe(new ObjectiveVector(0.5, -0.5));
    }

    [Fact]
    public void RelativeQualityEvaluator_AppliesZeroBestKnownPolicy()
    {
        var evaluator = CreateEvaluator().ScaledToBestKnown(new ObjectiveVector(0.0),
            RelativeQualityZeroBestKnownPolicy.Difference);
        var executor = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator);
        var problem = CreateProblem();

        var result = executor.Evaluate([3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

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
