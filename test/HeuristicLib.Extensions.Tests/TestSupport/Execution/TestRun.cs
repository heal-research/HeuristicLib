using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Extensions.Tests.TestSupport.Execution;

internal sealed class TestRun : Run
{
  public static TestRun Instance { get; } = new();

  private TestRun()
  {
  }
}

