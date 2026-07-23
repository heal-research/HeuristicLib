using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.DataAnalysis;

public abstract class DataAnalysisProblem<TProblemData, TISolution, TSearchSpace>(TProblemData problemData, ObjectiveDirections objective, TSearchSpace encoding)
  : SingleSolutionProblem<TISolution, TSearchSpace>(objective, encoding)
  where TProblemData : DataAnalysisProblemData
  where TSearchSpace : class, ISearchSpace<TISolution>
{
    public virtual TProblemData ProblemData
    {
        get;
    } = problemData;

    // Maybe earlier in the hierarchy for general deterministic problems?
    public sealed override ObjectiveVector Evaluate(TISolution genotype, IRandomNumberGenerator random) => Evaluate(genotype);
    public abstract ObjectiveVector Evaluate(TISolution solution);
}
