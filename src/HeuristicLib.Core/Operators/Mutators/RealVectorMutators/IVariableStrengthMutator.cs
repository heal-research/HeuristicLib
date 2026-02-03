using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;

public interface IVariableStrengthMutator<T, in T1, in T2> : IMutator<T, T1, T2> where T1 : class, ISearchSpace<T> where T2 : class, IProblem<T, T1>
{
  double MutationStrength { get; set; }
}
