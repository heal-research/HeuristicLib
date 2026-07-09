using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Partial;

public interface IPartialSolutionProblem<TCandidate, out TSearchSpace, in TChoice> : IProblem<TCandidate, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TCandidate>
{
  bool IsTerminal(TCandidate candidate, IRandomNumberGenerator random);
  ObjectiveVector Bound(TCandidate candidate, IRandomNumberGenerator random);
  ObjectiveVector? EvaluatePartial(TCandidate candidate, IRandomNumberGenerator random);
  TCandidate ApplyChoice(TCandidate candidate, TChoice choice);
}
