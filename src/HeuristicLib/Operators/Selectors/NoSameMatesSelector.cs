using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

public record NoSameMatesSelector<TCandidate, TSearchSpace, TProblem>(
  ISelector<TCandidate, TSearchSpace, TProblem> InnerSelector,
  int MaxAttempts)
  : WrappingSelector<TCandidate, TSearchSpace, TProblem>(InnerSelector)
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, InnerSelect innerSelect, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
        int selectedParents = 0;
        int poolCount = 0;

        var selected = new EvaluatedCandidate<TCandidate>[count];
        var parentsPool = new EvaluatedCandidate<TCandidate>[count];
        // repeat until enough parents are selected or max attempts are reached
        for (int attempts = 1; attempts <= MaxAttempts && selectedParents < count; attempts++)
        {
            var parents = innerSelect(population, objective, count, random, searchSpace, problem);
            for (int indexParent1 = 0, indexParent2 = 1;
                 indexParent1 < parents.Count - 1 && selectedParents < count - 1;
                 indexParent1 += 2, indexParent2 += 2)
            {
                var qualityParent1 = parents[indexParent1].ObjectiveVector;
                var qualityParent2 = parents[indexParent1].ObjectiveVector;

                if (!qualityParent1.Equals(qualityParent2))
                {
                    // inner selector already copied scopes, no cloning necessary here
                    selected[selectedParents++] = parents[indexParent1];
                    selected[selectedParents++] = parents[indexParent2];
                }
                else if (attempts == MaxAttempts && poolCount < count - selectedParents)
                {
                    // last attempt: save parents to fill remaining positions
                    parentsPool[poolCount++] = parents[indexParent1];
                    parentsPool[poolCount++] = parents[indexParent2];
                }
            }
        }

        // fill remaining positions with parents which don't meet the difference criterion 
        if (selectedParents < count - 1)
        {
            Array.Copy(parentsPool, 0, selected, selectedParents, count - selectedParents);
        }

        return selected;
    }
}
