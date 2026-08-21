using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems;

public static class FuncProblem
{
    public static FuncProblem<TCandidate, TSearchSpace> Create<TCandidate, TSearchSpace>(Func<TCandidate, double> evaluateFunc, TSearchSpace encoding, ObjectiveDirections objective) where TSearchSpace : class, ISearchSpace<TCandidate> => new(evaluateFunc, encoding, objective);
}

public class FuncProblem<TCandidate, TSearchSpace> : SingleSolutionProblem<TCandidate, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public FuncProblem(Func<TCandidate, ObjectiveVector> evaluateFunc, TSearchSpace searchSpace, ObjectiveDirections objective) : base(objective, searchSpace)
    {
        EvaluateFunc = evaluateFunc;
    }

    public FuncProblem(Func<TCandidate, double> evaluateFunc, TSearchSpace searchSpace, ObjectiveDirections objective) : base(objective, searchSpace)
    {
        EvaluateFunc = sol => evaluateFunc(sol);
    }

    public FuncProblem(Func<TCandidate, double[]> evaluateFunc, TSearchSpace searchSpace, ObjectiveDirections objective) : base(objective, searchSpace)
    {
        EvaluateFunc = sol => evaluateFunc(sol);
    }

    private Func<TCandidate, ObjectiveVector> EvaluateFunc { get; }

    public override ObjectiveVector Evaluate(TCandidate candidate, IRandomNumberGenerator random) => EvaluateFunc(candidate);
}
