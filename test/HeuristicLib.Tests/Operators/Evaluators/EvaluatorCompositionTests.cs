using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Tests.TestSupport.Execution;
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
        var evaluator = CreateEvaluator().CountEvaluatedCandidates(counter).LimitEvaluations(2, strict: true);
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
        var evaluator = CreateEvaluator().CountEvaluatorCalls(counter).AsRepeatingAggregating(2, static (left, right) => left);
        var instance = new ExecutionInstanceRegistry().Resolve(evaluator);
        var problem = CreateProblem();

        instance.Evaluate([1], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(3);
    }

    private static IEvaluator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateEvaluator() =>
        new DummyEvaluator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>();

    private static FuncProblem<int, DummySearchSpace<int>> CreateProblem() =>
        FuncProblem.Create(static (int candidate) => candidate, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
}
