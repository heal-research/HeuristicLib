using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Prototypes;

public interface IMutatorPrototype<T, TE, TP> where TE : class, ISearchSpace<T> where TP : class, IProblem<T, TE>
{
  IMutator<T, TE, TP> Mutator { get; set; }
}
