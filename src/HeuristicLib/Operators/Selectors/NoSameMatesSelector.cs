using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public record NoSameMatesSelector<TCandidate>
    : WrappingSelector<TCandidate>
{
    public NoSameMatesSelector(ISelector<TCandidate> childSelector, int maxAttempts)
        : base(childSelector)
    {
        MaxAttempts = maxAttempts;
    }

    public int MaxAttempts { get; init; }

    protected override ISelectorInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(ISelectorInstance<TCandidate, TRunSearchSpace, TRunProblem> childSelector) =>
        new Instance<TRunSearchSpace, TRunProblem>(childSelector, MaxAttempts);

    private sealed class Instance<TSearchSpace, TProblem>(ISelectorInstance<TCandidate, TSearchSpace, TProblem> childSelector, int maxAttempts)
        : WrappingSelectorInstance<TCandidate, TSearchSpace, TProblem>(childSelector)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var selectedParents = 0;
            var poolCount = 0;

            var selected = new EvaluatedCandidate<TCandidate>[count];
            var parentsPool = new EvaluatedCandidate<TCandidate>[count];
            for (var attempts = 1; attempts <= maxAttempts && selectedParents < count; attempts++)
            {
                var parents = ChildSelector.Select(population, objective, count, random, searchSpace, problem);
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
    public static NoSameMatesSelector<TCandidate> Create<TCandidate>(ISelector<TCandidate> selector, int maximumAttempts) => new(selector, maximumAttempts);
}

public static class NoSameMatesSelectorExtensions
{
    extension<TCandidate>(ISelector<TCandidate> selector)
    {
        public NoSameMatesSelector<TCandidate> AvoidSameMates(int maximumAttempts) => NoSameMatesSelector.Create(selector, maximumAttempts);
    }
}
