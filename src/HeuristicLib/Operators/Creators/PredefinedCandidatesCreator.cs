using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Emits predefined candidates across successive calls before delegating remaining requests to a fallback creator.
/// </summary>
public record PredefinedCandidatesCreator<TCandidate>
    : Operator<Creation, TCandidate>
{
    public IOperator<Creation, TCandidate> CreatorForRemainingCandidates { get; init; }

    public ValueArray<TCandidate> PredefinedCandidates { get; init; }

    public static Parameter<PredefinedCandidatesCreator<TCandidate>, IOperator<Creation, TCandidate>> CreatorForRemainingCandidatesParameter { get; } =
        new(nameof(CreatorForRemainingCandidates), static creator => creator.CreatorForRemainingCandidates, static (creator, value) => creator with { CreatorForRemainingCandidates = value });

    public static SettingParameter<PredefinedCandidatesCreator<TCandidate>, ValueArray<TCandidate>> PredefinedCandidatesParameter { get; } =
        new(nameof(PredefinedCandidates), static creator => creator.PredefinedCandidates, static (creator, value) => creator with { PredefinedCandidates = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [CreatorForRemainingCandidatesParameter, PredefinedCandidatesParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    public PredefinedCandidatesCreator(IReadOnlyList<TCandidate> predefinedCandidates, IOperator<Creation, TCandidate> creatorForRemainingCandidates)
    {
        PredefinedCandidates = predefinedCandidates.ToValueArray();
        CreatorForRemainingCandidates = creatorForRemainingCandidates;
    }

    protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
        ResolutionScope<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
    {
        var creatorForRemainingCandidates = scope.Resolve(CreatorForRemainingCandidates);
        return new Executor(creatorForRemainingCandidates, PredefinedCandidates);
    }

    private sealed class Executor(ICreationExecutor<TCandidate> creatorForRemainingCandidates, ValueArray<TCandidate> predefinedCandidates)
        : CreationExecutor<TCandidate>
    {
        private int currentCandidateIndex;

        public override IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random)
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

            var remaining = creatorForRemainingCandidates.Create(countRemaining, random);
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
    public static PredefinedCandidatesCreator<TCandidate> Create<TCandidate>(IReadOnlyList<TCandidate> predefinedCandidates, IOperator<Creation, TCandidate> creatorForRemainingCandidates) =>
        new(predefinedCandidates, creatorForRemainingCandidates);
}

public static class PredefinedCandidatesCreatorExtensions
{
    extension<TCandidate>(IOperator<Creation, TCandidate> creator)
    {
        public PredefinedCandidatesCreator<TCandidate> SeededWith(IReadOnlyList<TCandidate> predefinedCandidates) =>
            PredefinedCandidatesCreator.Create(predefinedCandidates, creator);
    }
}
