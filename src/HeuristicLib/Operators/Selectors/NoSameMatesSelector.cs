using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public record NoSameMatesSelector<TCandidate>
    : WrappingOperator<Selection, TCandidate>
{
    public NoSameMatesSelector(IOperator<Selection, TCandidate> childSelector, int maxAttempts)
        : base(childSelector)
    {
        MaxAttempts = maxAttempts;
    }

    public int MaxAttempts { get; init; }

    protected override OperatorExecutorFactory CreateOperatorFactory() => new Factory(this);

    private sealed class Factory(NoSameMatesSelector<TCandidate> source) : OperatorExecutorFactory<Selection, TCandidate>
    {
        protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
            ResolutionScope<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> scope) =>
            new Executor<TRunSearchSpace, TRunProblem>(scope.Resolve(source.ChildOperator), source.MaxAttempts);
    }

    private sealed class Executor<TSearchSpace, TProblem>(ISelectionExecutor<TCandidate, TSearchSpace, TProblem> childSelector, int maxAttempts)
        : ISelectionExecutor<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var selectedParents = 0;
            var poolCount = 0;

            var selected = new EvaluatedCandidate<TCandidate>[count];
            var parentsPool = new EvaluatedCandidate<TCandidate>[count];
            for (var attempts = 1; attempts <= maxAttempts && selectedParents < count; attempts++)
            {
                var parents = childSelector.Select(population, objective, count, random, searchSpace, problem);
                for (int indexParent1 = 0, indexParent2 = 1; indexParent1 < parents.Count - 1 && selectedParents < count - 1; indexParent1 += 2, indexParent2 += 2)
                {
                    var qualityParent1 = parents[indexParent1].ObjectiveVector;
                    var qualityParent2 = parents[indexParent2].ObjectiveVector;

                    if (!qualityParent1.Equals(qualityParent2))
                    {
                        selected[selectedParents++] = parents[indexParent1];
                        selected[selectedParents++] = parents[indexParent2];
                    }
                    else if (attempts == maxAttempts && poolCount < count - selectedParents)
                    {
                        parentsPool[poolCount++] = parents[indexParent1];
                        parentsPool[poolCount++] = parents[indexParent2];
                    }
                }
            }

            if (selectedParents < count - 1)
            {
                Array.Copy(parentsPool, 0, selected, selectedParents, count - selectedParents);
            }

            return selected;
        }
    }
}

public static class NoSameMatesSelector
{
    public static NoSameMatesSelector<TCandidate> Create<TCandidate>(IOperator<Selection, TCandidate> selector, int maximumAttempts) => new(selector, maximumAttempts);
}

public static class NoSameMatesSelectorExtensions
{
    extension<TCandidate>(IOperator<Selection, TCandidate> selector)
    {
        public NoSameMatesSelector<TCandidate> AvoidSameMates(int maximumAttempts) => NoSameMatesSelector.Create(selector, maximumAttempts);
    }
}
