using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// The inputs and outputs captured after one crossover call.
/// </summary>
public sealed record CrossoverObservation<TCandidate, TSearchSpace, TProblem>(ICrossover<TCandidate> Crossover, IReadOnlyList<TCandidate> Offspring, IReadOnlyList<Parents<TCandidate>> Parents, TSearchSpace SearchSpace, TProblem Problem)
    : Observation<TProblem>(Problem)
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>;
