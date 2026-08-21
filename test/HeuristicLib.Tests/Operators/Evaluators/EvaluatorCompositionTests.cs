using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Evaluators;

public class EvaluatorCompositionTests
{
    [Fact]
    public void CachingEvaluator_UsesIndependentCachePerExecutionInstance()
    {
        var counter = new ObservationCounter();
        var evaluator = CreateEvaluator().CountEvaluatorCalls(counter).WithCache();
        var firstInstance = new ExecutionInstanceRegistry().Resolve(evaluator);
        var secondInstance = new ExecutionInstanceRegistry().Resolve(evaluator);
        var problem = CreateProblem();

        firstInstance.Evaluate([1], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        firstInstance.Evaluate([1], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);
        secondInstance.Evaluate([1], RandomNumberGenerator.Create(3), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void LimitEvaluator_UsesIndependentCounterPerExecutionInstance()
    {
        var counter = new ObservationCounter();
        var evaluator = CreateEvaluator().CountEvaluatedCandidates(counter).LimitEvaluations(2, enforceLimitWithinBatch: true);
        var firstInstance = new ExecutionInstanceRegistry().Resolve(evaluator);
        var secondInstance = new ExecutionInstanceRegistry().Resolve(evaluator);
        var problem = CreateProblem();

        var limited = firstInstance.Evaluate([1, 2, 3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        secondInstance.Evaluate([4], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        limited.Count.ShouldBe(3);
        counter.CurrentCount.ShouldBe(3);
    }

    [Fact]
    public void RepeatingEvaluator_InvokesResolvedChildForEveryEvaluation()
    {
        var counter = new ObservationCounter();
        var evaluator = CreateEvaluator().CountEvaluatorCalls(counter).AsRepeated(2, ObjectiveVectorAggregation.Mean);
        var instance = new ExecutionInstanceRegistry().Resolve(evaluator);
        var problem = CreateProblem();

        instance.Evaluate([1], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void RelativeQualityEvaluator_NormalizesElementwise()
    {
        var evaluator = CreateEvaluator().WithRelativeQuality(new ObjectiveVector(2.0, -4.0));
        var instance = new ExecutionInstanceRegistry().Resolve(evaluator);
        var problem = new FuncProblem<int, DummySearchSpace<int>>(
            static candidate => new ObjectiveVector(candidate, -2.0 * candidate),
            DummySearchSpace<int>.Instance,
            MultiObjective.Minimize(2));

        var result = instance.Evaluate([3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        result[0].ShouldBe(new ObjectiveVector(0.5, -0.5));
    }

    [Fact]
    public void RelativeQualityEvaluator_AppliesZeroBestKnownPolicy()
    {
        var evaluator = CreateEvaluator().WithRelativeQuality(new ObjectiveVector(0.0),
            RelativeQualityZeroBestKnownPolicy.Difference);
        var instance = new ExecutionInstanceRegistry().Resolve(evaluator);
        var problem = CreateProblem();

        var result = instance.Evaluate([3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        result[0].ShouldBe(new ObjectiveVector(3.0));
    }

    private static IEvaluator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateEvaluator() =>
        new ProblemEvaluator();

    private static FuncProblem<int, DummySearchSpace<int>> CreateProblem() =>
        FuncProblem.Create(static (int candidate) => candidate, DummySearchSpace<int>.Instance, SingleObjective.Minimize);

    private sealed record ProblemEvaluator : SingleCandidateEvaluator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {
        public override ObjectiveVector EvaluateCandidate(int candidate, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, FuncProblem<int, DummySearchSpace<int>> problem) =>
            problem.Evaluate(candidate, random);
    }
}
