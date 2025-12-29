using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.OperatorPrototypes;

public interface ISelectorPrototype<T, TS, TP> where TS : class, ISearchSpace<T> where TP : class, IProblem<T, TS> {
  ISelector<T, TS, TP> Selector { get; set; }
}
