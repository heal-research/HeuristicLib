using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Encodings;

public record AnyEncoding<TSolution> : IEncoding<TSolution> {
  public static readonly AnyEncoding<TSolution> Instance = new();
  private AnyEncoding() { }
  public bool Contains(TSolution genotype) => true;
}
