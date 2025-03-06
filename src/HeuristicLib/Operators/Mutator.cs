namespace HEAL.HeuristicLib.Operators;

public interface IMutator<TSolution> : IOperator {
  TSolution Mutate(TSolution parent);
}

public abstract class MutatorBase<TSolution> : IMutator<TSolution> {
  public abstract TSolution Mutate(TSolution parent);
}

public interface IAdaptableMutator<TSolution> : IMutator<TSolution> {
  TSolution Mutate(TSolution parent, double mutationStrength);
}
