namespace HEAL.HeuristicLib.Operators;

public interface IMutator<TSolution>
{
  TSolution Mutate(TSolution individual);
}

