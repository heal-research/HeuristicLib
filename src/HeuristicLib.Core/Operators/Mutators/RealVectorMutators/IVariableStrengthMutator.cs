using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;

[Obsolete("This should be replaced by a proper operator or an operator feature.")]
public interface IVariableStrengthMutator<TG, in TS, in TP>
  : IMutator<TG, TS, TP>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  double MutationStrength { get; [Obsolete("Should not be mutable")] set; }
}
