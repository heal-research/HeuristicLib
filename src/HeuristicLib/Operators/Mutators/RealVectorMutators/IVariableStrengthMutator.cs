using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;

public interface IVariableStrengthMutator<TG, in TS, in TP> 
  : IMutator<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS> 
{
  public double MutationStrength { get; [Obsolete("Should not be mutable")]set; }
}
