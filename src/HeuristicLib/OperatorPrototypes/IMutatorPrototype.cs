using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.OperatorPrototypes;

public interface IMutatorPrototype<T, TS, TP> where TS : class, ISearchSpace<T> where TP : class, IProblem<T, TS> {
  IMutator<T, TS, TP> Mutator { get; set; }
}
