using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Prototypes;

public interface ISelectorPrototype<T, TE, TP> where TE : class, ISearchSpace<T> where TP : class, IProblem<T, TE>
{
  ISelector<T, TE, TP> Selector { get; set; }
}
