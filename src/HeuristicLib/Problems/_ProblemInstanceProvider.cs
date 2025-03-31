
using HEAL.HeuristicLib.Core;

namespace HEAL.HeuristicLib.Problems;

public interface IProblemInstanceProvider<TProblem>
  where TProblem : IProblem 
{
  (TProblem, InstanceInformation?) GetNamedInstance(string name);
}


public record class InstanceInformation {
  public required string Name { get; init; }
  public string? Description { get; init; }
  public string? Publication { get; init; }
  public Fitness? BestKnownQuality { get; init; }
}

public record class InstanceInformation<TSolution> : InstanceInformation {
  public TSolution? BestKnownSolution { get; init; }
}
