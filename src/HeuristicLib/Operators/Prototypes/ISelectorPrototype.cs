using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators.Selector;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators.Prototypes;

public interface ISelectorPrototype<T, TE, TP> where TE : class, IEncoding<T> where TP : class, IProblem<T, TE> {
  ISelector<T, TE, TP> Selector { get; set; }
}
