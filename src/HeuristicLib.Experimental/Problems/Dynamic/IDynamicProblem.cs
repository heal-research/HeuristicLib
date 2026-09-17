using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Dynamic;

public interface IDynamicProblem<TCandidate, out TSearchSpace> : IProblem<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    event EventHandler<IReadOnlyList<(TCandidate, ObjectiveVector, EvaluationTiming)>>? OnEvaluation;

    EvaluationClock EpochClock { get; }
}
