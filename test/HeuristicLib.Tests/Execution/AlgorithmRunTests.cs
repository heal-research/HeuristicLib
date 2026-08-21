using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

public class AlgorithmRunTests
{
    [Fact]
    public void AlgorithmRun_CannotBeExecutedMoreThanOnce()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var run = new AdditiveStepAlgorithm(1).CreateRun(problem, RandomNumberGenerator.Create(42));

        run.ExecutionStarted.ShouldBeFalse();
        _ = run.Complete(cancellationToken: TestContext.Current.CancellationToken);
        run.ExecutionStarted.ShouldBeTrue();

        var exception = Should.Throw<InvalidOperationException>(() =>
            run.Complete(cancellationToken: TestContext.Current.CancellationToken));
        exception.Message.ShouldBe("A run can only be configured and executed once. Create a new run for another execution.");
    }
}
