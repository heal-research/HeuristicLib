using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Tests.TestSupport.Execution;

internal sealed class TestRun : AlgorithmRun
{
    public static TestRun Instance { get; } = new();

    private TestRun()
    {
    }
}
