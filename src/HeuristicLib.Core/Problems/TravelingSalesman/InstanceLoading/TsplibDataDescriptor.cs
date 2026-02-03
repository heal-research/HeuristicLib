namespace HEAL.HeuristicLib.Problems.TravelingSalesman.InstanceLoading;

public class TsplibDataDescriptor
{

  internal TsplibDataDescriptor(string name, string description, string instanceIdentifier, string solutionIdentifier, double? bestQuality)
  {
    Name = name;
    Description = description;
    InstanceIdentifier = instanceIdentifier;
    SolutionIdentifier = solutionIdentifier;
    BestQuality = bestQuality;
  }
  public string Name { get; internal set; }
  public string Description { get; internal set; }

  internal string InstanceIdentifier { get; set; }
  internal string SolutionIdentifier { get; set; }
  internal double? BestQuality { get; set; }
}
