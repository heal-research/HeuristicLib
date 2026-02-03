using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators.Prototypes;

public interface IMutatorPrototype<T, TE, TP> where TE : class, IEncoding<T> where TP : class, IProblem<T, TE> {
  IMutator<T, TE, TP> Mutator { get; set; }
}
