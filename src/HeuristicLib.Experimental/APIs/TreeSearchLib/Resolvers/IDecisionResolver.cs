namespace HEAL.HeuristicLib.APIs.TreeSearchLib;

public interface IDecisionResolver<out T, in TM>
{
    T Resolve(IEnumerable<TM> choices);
}
