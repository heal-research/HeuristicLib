using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Encodings;

public interface IEncoding {}

public interface IEncoding<TSolution> : IEncoding {
  ICreator<TSolution> Creator { get; }
  IMutator<TSolution> Mutator { get; }
  ICrossover<TSolution> Crossover { get; }
}
