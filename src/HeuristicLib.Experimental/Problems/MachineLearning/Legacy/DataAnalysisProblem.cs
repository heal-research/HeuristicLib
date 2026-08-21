using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.MachineLearning.Legacy;

public abstract class DataAnalysisProblem<TProblemData, TCandidate, TSearchSpace>(TProblemData problemData, ObjectiveDirections objective, TSearchSpace encoding)
  : SingleSolutionProblem<TCandidate, TSearchSpace>(objective, encoding)
  where TProblemData : DataAnalysisProblemData
  where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public virtual TProblemData ProblemData
    {
        get;
    } = problemData;

    // Maybe earlier in the hierarchy for general deterministic problems?
    public sealed override ObjectiveVector Evaluate(TCandidate candidate, IRandomNumberGenerator random) => Evaluate(candidate);
    public abstract ObjectiveVector Evaluate(TCandidate candidate);
}
