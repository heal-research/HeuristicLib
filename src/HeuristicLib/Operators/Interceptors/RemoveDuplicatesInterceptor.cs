using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public sealed record RemoveDuplicatesInterceptor<TCandidate, TSearchState>
    : StatelessInterceptor<TCandidate, TSearchState>
    where TSearchState : PopulationState<TCandidate>
{
    public IEqualityComparer<TCandidate> Comparer { get; init; }

    public static Parameter<RemoveDuplicatesInterceptor<TCandidate, TSearchState>, IEqualityComparer<TCandidate>> ComparerParameter { get; } =
        new(nameof(Comparer), static interceptor => interceptor.Comparer, static (interceptor, value) => interceptor with { Comparer = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [ComparerParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    public RemoveDuplicatesInterceptor(IEqualityComparer<TCandidate> comparer)
    {
        Comparer = comparer;
    }

    public override TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random) =>
        RemoveDuplicatesInterceptor.Transform(currentState, previousState, Comparer);
}

public static class RemoveDuplicatesInterceptor
{
    public static TSearchState Transform<TCandidate, TSearchState>(TSearchState currentState, TSearchState? previousState, IEqualityComparer<TCandidate> comparer)
        where TSearchState : PopulationState<TCandidate>
    {
        var newSolutions = currentState.Population.DistinctBy(s => s.Candidate, comparer).ToImmutableArray();
        return currentState with
        {
            Population = Population.From(newSolutions)
        };
    }
}
