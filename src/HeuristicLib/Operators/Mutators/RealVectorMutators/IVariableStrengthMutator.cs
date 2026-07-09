using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;

[Obsolete("This should be replaced by a proper operator or an operator feature.")]
public interface IVariableStrengthMutator<TCandidate, in TSearchSpace, in TProblem>
  : IMutator<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    double MutationStrength { get; [Obsolete("Should not be mutable")] set; }
}
