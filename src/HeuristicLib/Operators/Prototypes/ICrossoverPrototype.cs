using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Prototypes;

public interface ICrossoverPrototype<T, TS, TP> where TS : class, ISearchSpace<T> where TP : class, IProblem<T, TS> {
  ICrossover<T, TS, TP> Crossover { get; set; }
}

public interface IOptionalCrossoverPrototype<T, TS, TP> where TS : class, ISearchSpace<T> where TP : class, IProblem<T, TS> {
  ICrossover<T, TS, TP>? Crossover { get; set; }
}
