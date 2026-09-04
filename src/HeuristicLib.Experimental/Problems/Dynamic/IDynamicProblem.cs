using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Dynamic;

public interface IDynamicProblem<TCandidate, out TSearchSpace> : IProblem<TCandidate, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TCandidate>
{
    event EventHandler<IReadOnlyList<(TCandidate, ObjectiveVector, EvaluationTiming)>>? OnEvaluation;

    /// <remarks>
    /// Here rather than on <see cref="DynamicProblem{TSelf, TCandidate, TSearchSpace}"/> alone so an operator that
    /// only needs the clock can hold the problem by this interface, and take its candidate and search space from that
    /// parameter instead of naming the concrete problem type.
    /// </remarks>
    EvaluationClock EpochClock { get; }
}
