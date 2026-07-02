using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

public record GenderSpecificSelector<TCandidate, TSearchSpace, TProblem>
  : MultiSelector<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public GenderSpecificSelector(ImmutableArray<ISelector<TCandidate, TSearchSpace, TProblem>> innerSelectors) : base(innerSelectors) { }

    protected override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IReadOnlyList<InnerSelect> innerSelectors, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(count % innerSelectors.Count, 0);
        var n = count / innerSelectors.Count;
        var r = innerSelectors.Select(select => select(population, objective, n, random, searchSpace, problem)).ToArray();

        var res = new EvaluatedCandidate<TCandidate>[count];
        var c = 0;
        for (int j = 0; j < n; j++)
        {
            for (int i = 0; i < innerSelectors.Count; i++)
            {
                res[c++] = r[i][j];
            }
        }

        return res;
    }
}
