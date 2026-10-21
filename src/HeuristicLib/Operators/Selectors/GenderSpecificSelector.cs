using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

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

    protected override OperatorExecutorFactory CreateOperatorFactory() => new Factory(this);

    private sealed class Factory(GenderSpecificSelector<TCandidate> source) : OperatorExecutorFactory<Selection, TCandidate>
    {
        protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
            ResolutionScope<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> scope) =>
            new Executor<TRunSearchSpace, TRunProblem>(scope.Resolve(source.FemaleSelector), scope.Resolve(source.MaleSelector));
    }

    private sealed class Executor<TSearchSpace, TProblem>(ISelectionExecutor<TCandidate, TSearchSpace, TProblem> femaleSelector, ISelectionExecutor<TCandidate, TSearchSpace, TProblem> maleSelector)
        : SelectionExecutor<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var pairCount = count / 2;
            var femaleCount = count - pairCount;
            var females = femaleSelector.Select(population, objective, femaleCount, random, searchSpace, problem);
            var males = maleSelector.Select(population, objective, pairCount, random, searchSpace, problem);

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
