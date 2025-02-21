namespace HEAL.HeuristicLib.Operators;

public interface ICreator<TSolution>
{
  TSolution Create();
}
