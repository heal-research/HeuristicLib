using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Emits predefined candidates across successive calls before delegating remaining requests to a fallback creator.
/// </summary>
public record PredefinedCandidatesCreator<TCandidate>
    : ICreator<TCandidate>
{
    public ICreator<TCandidate> CreatorForRemainingCandidates { get; init; }

    public ValueArray<TCandidate> PredefinedCandidates { get; init; }

    public PredefinedCandidatesCreator(IReadOnlyList<TCandidate> predefinedCandidates, ICreator<TCandidate> creatorForRemainingCandidates)
    {
        PredefinedCandidates = predefinedCandidates.ToValueArray();
        CreatorForRemainingCandidates = creatorForRemainingCandidates;
    }

    public ICreatorInstance<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace> =>
        new Instance<TRunSearchSpace, TRunProblem>(instanceRegistry.Resolve<TCandidate, TRunSearchSpace, TRunProblem>(CreatorForRemainingCandidates), PredefinedCandidates);

    private sealed class Instance<TSearchSpace, TProblem>(ICreatorInstance<TCandidate, TSearchSpace, TProblem> creatorForRemainingCandidates, ValueArray<TCandidate> predefinedCandidates)
        : CreatorInstance<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        private int currentCandidateIndex;

        public override IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var candidates = new TCandidate[count];
            var countPredefined = Math.Min(predefinedCandidates.Count - currentCandidateIndex, count);
            if (countPredefined > 0)
            {
                for (var i = 0; i < countPredefined; i++)
                {
                    candidates[i] = predefinedCandidates[currentCandidateIndex + i];
                }

                currentCandidateIndex += countPredefined;
            }

            var countRemaining = count - countPredefined;
            if (countRemaining <= 0)
            {
                return candidates;
            }

            var remaining = creatorForRemainingCandidates.Create(countRemaining, random, searchSpace, problem);
            for (var i = 0; i < remaining.Count; i++)
            {
                candidates[countPredefined + i] = remaining[i];
            }

            return candidates;
        }
    }
}

public static class PredefinedCandidatesCreator
{
    public static PredefinedCandidatesCreator<TCandidate> Create<TCandidate>(IReadOnlyList<TCandidate> predefinedCandidates, ICreator<TCandidate> creatorForRemainingCandidates) =>
        new(predefinedCandidates, creatorForRemainingCandidates);
}

public static class PredefinedCandidatesCreatorExtensions
{
    extension<TCandidate>(ICreator<TCandidate> creator)
    {
        public PredefinedCandidatesCreator<TCandidate> WithPredefinedCandidates(IReadOnlyList<TCandidate> predefinedCandidates) =>
            PredefinedCandidatesCreator.Create(predefinedCandidates, creator);
    }
}
