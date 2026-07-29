using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;
using HEAL.HeuristicLib.Tests.TestSupport.SpecialTestEncoding;

namespace HEAL.HeuristicLib.Tests.ExecutionModel;

public class ExecutionConcurrencyDefaultTests
{
    [Fact]
    public void SingleSolutionEvaluator_DefaultsToSequentialExecution()
    {
        var evaluator = new DummyEvaluator<SpecialGenotype, SpecialSearchSpace, SpecialProblem>();

        evaluator.Concurrency.ShouldBe(ExecutionConcurrency.Sequential());
    }

    [Fact]
    public void SingleSolutionProblem_DefaultsToSequentialExecution()
    {
        var problem = new SpecialProblem(0.0);

        problem.Concurrency.ShouldBe(ExecutionConcurrency.Sequential());
    }
}
