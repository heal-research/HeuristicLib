using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators.Prototypes;

public interface ICrossoverPrototype<T, TE, TP> where TE : class, IEncoding<T> where TP : class, IProblem<T, TE> {
  ICrossover<T, TE, TP> Crossover { get; set; }
}

public interface IOptionalCrossoverPrototype<T, TE, TP> where TE : class, IEncoding<T> where TP : class, IProblem<T, TE> {
  ICrossover<T, TE, TP>? Crossover { get; set; }
}
