using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Selects equal numbers of female and male candidates, then aligns them as consecutive pairs.
/// </summary>
/// <remarks>
/// The result order is female 0, male 0, female 1, male 1 and so on. For an odd requested count, the final
/// candidate is selected by the female selector.
/// </remarks>
public record GenderSpecificSelector<TCandidate>
    : Operator<Selection, TCandidate>
{
    public GenderSpecificSelector(IOperator<Selection, TCandidate> femaleSelector, IOperator<Selection, TCandidate> maleSelector)
    {
        FemaleSelector = femaleSelector;
        MaleSelector = maleSelector;
    }

    public IOperator<Selection, TCandidate> FemaleSelector { get; init; }

    public IOperator<Selection, TCandidate> MaleSelector { get; init; }

    public static Parameter<GenderSpecificSelector<TCandidate>, IOperator<Selection, TCandidate>> FemaleSelectorParameter { get; } =
        new(nameof(FemaleSelector), static selector => selector.FemaleSelector, static (selector, value) => selector with { FemaleSelector = value });

    public static Parameter<GenderSpecificSelector<TCandidate>, IOperator<Selection, TCandidate>> MaleSelectorParameter { get; } =
        new(nameof(MaleSelector), static selector => selector.MaleSelector, static (selector, value) => selector with { MaleSelector = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [FemaleSelectorParameter, MaleSelectorParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
        ResolutionScope<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
    {
        var femaleSelector = scope.Resolve(FemaleSelector);
        var maleSelector = scope.Resolve(MaleSelector);
        return new Executor(femaleSelector, maleSelector);
    }

    private sealed class Executor(ISelectionExecutor<TCandidate> femaleSelector, ISelectionExecutor<TCandidate> maleSelector)
        : SelectionExecutor<TCandidate>
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, int count, IRandomNumberGenerator random)
        {
            var pairCount = count / 2;
            var femaleCount = count - pairCount;
            var females = femaleSelector.Select(population, femaleCount, random);
            var males = maleSelector.Select(population, pairCount, random);

            var result = new EvaluatedCandidate<TCandidate>[count];
            for (var i = 0; i < pairCount; i++)
            {
                result[2 * i] = females[i];
                result[(2 * i) + 1] = males[i];
            }

            if (femaleCount > pairCount)
                result[^1] = females[^1];

            return result;
        }
    }
}

public static class GenderSpecificSelector
{
    public static GenderSpecificSelector<TCandidate> Create<TCandidate>(
        IOperator<Selection, TCandidate> femaleSelector, IOperator<Selection, TCandidate> maleSelector) => new(femaleSelector, maleSelector);
}

public static class GenderSpecificSelectorExtensions
{
    extension<TCandidate>(IOperator<Selection, TCandidate> femaleSelector)
    {
        public GenderSpecificSelector<TCandidate> PairWith(IOperator<Selection, TCandidate> maleSelector) =>
            GenderSpecificSelector.Create(femaleSelector, maleSelector);
    }
}
