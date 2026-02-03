using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Prototypes;

public interface ICrossoverPrototype<T, TE, TP> where TE : class, ISearchSpace<T> where TP : class, IProblem<T, TE>
{
  ICrossover<T, TE, TP> Crossover { get; set; }
}

public interface IOptionalCrossoverPrototype<T, TE, TP> where TE : class, ISearchSpace<T> where TP : class, IProblem<T, TE>
{
  ICrossover<T, TE, TP>? Crossover { get; set; }
}
