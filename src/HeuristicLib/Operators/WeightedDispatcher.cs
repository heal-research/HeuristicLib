using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Selects one weighted operator and invokes it for the complete operation call.</summary>
internal sealed class WeightedDispatcher<TOperator>(IReadOnlyList<TOperator> operators, IReadOnlyList<double>? weights = null)
{
    private readonly WeightedItemSampler<TOperator> sampler = new(operators, weights);

    public TOutput Dispatch<TOutput>(IRandomNumberGenerator random, Func<TOperator, TOutput> invoke) =>
        Dispatch(random, invoke, static (@operator, invoke) => invoke(@operator));

    public TOutput Dispatch<TOutput, TState>(IRandomNumberGenerator random, TState state, Func<TOperator, TState, TOutput> invoke) =>
        invoke(sampler.Sample(random), state);
}

internal static class WeightedDispatcher
{
    public static WeightedDispatcher<TOperator> Create<TOperator>(IReadOnlyList<TOperator> operators, IReadOnlyList<double>? weights = null) =>
        new(operators, weights);
}
