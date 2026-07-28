using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

/// <summary>
/// Selects equal numbers of female and male candidates, then aligns them as consecutive pairs.
/// </summary>
/// <remarks>
/// The requested count must be even. The result order is female 0, male 0, female 1, male 1 and so on.
/// </remarks>
public record GenderSpecificSelector<TCandidate, TSearchSpace, TProblem>
    : MultiSelector<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ISelector<TCandidate, TSearchSpace, TProblem> FemaleSelector => InnerSelectors[0];

    public ISelector<TCandidate, TSearchSpace, TProblem> MaleSelector => InnerSelectors[1];

    public GenderSpecificSelector(ISelector<TCandidate, TSearchSpace, TProblem> femaleSelector, ISelector<TCandidate, TSearchSpace, TProblem> maleSelector)
        : base([femaleSelector, maleSelector])
    {
    }

    protected override MultiSelectorInstance<TCandidate, TSearchSpace, TProblem> CreateSelectorInstance(ImmutableArray<ISelectorInstance<TCandidate, TSearchSpace, TProblem>> innerSelectors) =>
        new Instance(innerSelectors);

    private sealed class Instance(ImmutableArray<ISelectorInstance<TCandidate, TSearchSpace, TProblem>> innerSelectors)
        : MultiSelectorInstance<TCandidate, TSearchSpace, TProblem>(innerSelectors)
    {
        private ISelectorInstance<TCandidate, TSearchSpace, TProblem> FemaleSelector => InnerSelectors[0];

        private ISelectorInstance<TCandidate, TSearchSpace, TProblem> MaleSelector => InnerSelectors[1];

        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            if (count % 2 != 0)
            {
                throw new ArgumentException("The requested count must be even.", nameof(count));
            }

            var pairCount = count / 2;
            var females = FemaleSelector.Select(population, objective, pairCount, random, searchSpace, problem);
            var males = MaleSelector.Select(population, objective, pairCount, random, searchSpace, problem);

            var result = new EvaluatedCandidate<TCandidate>[count];
            for (var i = 0; i < pairCount; i++)
            {
                result[2 * i] = females[i];
                result[(2 * i) + 1] = males[i];
            }

            return result;
        }
    }
}

public static class GenderSpecificSelector
{
    public static GenderSpecificSelector<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(
        ISelector<TCandidate, TSearchSpace, TProblem> femaleSelector, ISelector<TCandidate, TSearchSpace, TProblem> maleSelector)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> => new(femaleSelector, maleSelector);
}

public static class GenderSpecificSelectorExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(ISelector<TCandidate, TSearchSpace, TProblem> femaleSelector)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public GenderSpecificSelector<TCandidate, TSearchSpace, TProblem> PairWith(ISelector<TCandidate, TSearchSpace, TProblem> maleSelector) =>
            GenderSpecificSelector.Create(femaleSelector, maleSelector);
    }
}
