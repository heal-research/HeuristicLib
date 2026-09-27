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
        var evaluator = CreateEvaluator().CountCalls(counter).Cached();
        var firstExecution = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator);
        var secondExecution = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator);
        var problem = CreateProblem();

        firstExecution.Evaluate([1], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        firstExecution.Evaluate([1], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);
        secondExecution.Evaluate([1], RandomNumberGenerator.Create(3), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void LimitEvaluator_UsesIndependentCounterPerExecution()
    {
        var counter = new CountAccumulator();
        var evaluator = CreateEvaluator().CountCandidates(counter).LimitEvaluations(2, enforceLimitWithinBatch: true);
        var firstExecution = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator);
        var secondExecution = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator);
        var problem = CreateProblem();

        var limited = firstExecution.Evaluate([1, 2, 3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        secondExecution.Evaluate([4], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        limited.Count.ShouldBe(3);
        counter.CurrentCount.ShouldBe(3);
    }

    [Fact]
    public void RepeatingEvaluator_InvokesResolvedChildForEveryEvaluation()
    {
        var counter = new CountAccumulator();
        var evaluator = CreateEvaluator().CountCalls(counter).AsRepeated(2, ObjectiveVectorAggregation.Mean);
        var execution = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator);
        var problem = CreateProblem();

        execution.Evaluate([1], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void RelativeQualityEvaluator_NormalizesElementwise()
    {
        var evaluator = CreateEvaluator().ScaledToBestKnown(new ObjectiveVector(2.0, -4.0));
        var execution = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator);
        var problem = new FuncProblem<int, DummySearchSpace<int>>(
            static candidate => new ObjectiveVector(candidate, -2.0 * candidate),
            DummySearchSpace<int>.Instance,
            MultiObjective.Minimize(2));

        var result = execution.Evaluate([3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        result[0].ShouldBe(new ObjectiveVector(0.5, -0.5));
    }

    [Fact]
    public void RelativeQualityEvaluator_AppliesZeroBestKnownPolicy()
    {
        var evaluator = CreateEvaluator().ScaledToBestKnown(new ObjectiveVector(0.0),
            RelativeQualityZeroBestKnownPolicy.Difference);
        var execution = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator);
        var problem = CreateProblem();

        var result = execution.Evaluate([3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        result[0].ShouldBe(new ObjectiveVector(3.0));
    }

    private static IEvaluator<int> CreateEvaluator() =>
        new ProblemEvaluator();

    private static FuncProblem<int, DummySearchSpace<int>> CreateProblem() =>
        FuncProblem.Create(static (int candidate) => candidate, DummySearchSpace<int>.Instance, SingleObjective.Minimize);

    private sealed record ProblemEvaluator : SingleCandidateEvaluator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {
        public override ObjectiveVector EvaluateCandidate(int candidate, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, FuncProblem<int, DummySearchSpace<int>> problem) =>
            problem.Evaluate(candidate, random);
    }
}
