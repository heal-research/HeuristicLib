using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators.Mutator.RealVectors;

public interface IVariableStrengthMutator<T, in T1, in T2> : IMutator<T, T1, T2> where T1 : class, IEncoding<T> where T2 : class, IProblem<T, T1> {
  public double MutationStrength { get; set; }
}
