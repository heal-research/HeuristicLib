namespace HEAL.HeuristicLib.Operators;

public interface ICreator<out TGenotype>
{
  TGenotype Create();
}

