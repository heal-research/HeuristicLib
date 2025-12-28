using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutator.RealVectors;

public interface IVariableStrengthMutator<T, in T1, in T2> : IMutator<T, T1, T2> where T1 : class, ISearchSpace<T> where T2 : class, IProblem<T, T1> {
  public double MutationStrength { get; set; }
}
