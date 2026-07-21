using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

public class RunTests
{
    [Fact]
    public void Run_CannotBeExecutedMoreThanOnce()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var run = new AdditiveStepAlgorithm(1).CreateRun(problem);

        _ = run.Complete(RandomNumberGenerator.Create(42), cancellationToken: TestContext.Current.CancellationToken);

        var exception = Should.Throw<InvalidOperationException>(() =>
            run.Complete(RandomNumberGenerator.Create(42), cancellationToken: TestContext.Current.CancellationToken));
        exception.Message.ShouldBe("A run can only be executed once. Create a new run for another execution.");
    }
}
