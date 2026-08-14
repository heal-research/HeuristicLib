using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.TestSupport.Mocks;

public static class DummyEvaluator
{
    public static readonly ObjectiveVector DummyObjectives = new(0.0);
}

public sealed record DummyEvaluator<TCandidate, TSearchSpace, TProblem> : SingleCandidateEvaluator<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public override ObjectiveVector EvaluateCandidate(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) => DummyEvaluator.DummyObjectives;
}
