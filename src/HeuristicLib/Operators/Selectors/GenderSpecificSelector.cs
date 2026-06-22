using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

public record GenderSpecificSelector<TGenotype, TSearchSpace, TProblem>
  : MultiSelector<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    public GenderSpecificSelector(ImmutableArray<ISelector<TGenotype, TSearchSpace, TProblem>> innerSelectors) : base(innerSelectors) { }

    protected override IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IReadOnlyList<InnerSelect> innerSelectors, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(count % innerSelectors.Count, 0);
        var n = count / innerSelectors.Count;
        var r = innerSelectors.Select(select => select(population, objective, n, random, searchSpace, problem)).ToArray();

        var res = new Solution<TGenotype>[count];
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
