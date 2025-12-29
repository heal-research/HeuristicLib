using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.OperatorPrototypes;

public interface ICrossoverPrototype<T, TS, TP> where TS : class, ISearchSpace<T> where TP : class, IProblem<T, TS> {
  ICrossover<T, TS, TP> Crossover { get; set; }
}

public interface IOptionalCrossoverPrototype<T, TS, TP> where TS : class, ISearchSpace<T> where TP : class, IProblem<T, TS> {
  ICrossover<T, TS, TP>? Crossover { get; set; }
}
