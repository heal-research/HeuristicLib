namespace HEAL.HeuristicLib.Operators;

public interface ICrossover<TSolution>
{
  TSolution Crossover(TSolution parent1, TSolution parent2);
}

