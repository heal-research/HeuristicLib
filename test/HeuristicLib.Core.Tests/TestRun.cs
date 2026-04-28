using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Tests;

internal sealed class TestRun : Run
{
  public static TestRun Instance { get; } = new();

  private TestRun()
  {
  }
}

