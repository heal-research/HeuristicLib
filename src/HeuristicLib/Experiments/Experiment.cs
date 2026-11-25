using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Experiments;

/// <summary>An ordered list of grids to run on one or more problems, with repetitions and a shared stream setting.</summary>
public sealed record Experiment<TCandidate, TAlgorithm, TSearchState>
    where TAlgorithm : class, IAlgorithm<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    /// <summary>The grids in the order supplied to the experiment.</summary>
    public ValueArray<ParameterGrid<TCandidate, TAlgorithm, TSearchState>> Grids { get; }

    /// <summary>The number of times each parameter candidate runs on each problem. The default is one.</summary>
    public int Repetitions { get; }

    /// <summary>Uses the same starting stream at every parameter candidate for a given problem and repetition by default.</summary>
    public bool ShareRandomStreamsAcrossParameterCandidates { get; init; } = true;

    internal Experiment(ValueArray<ParameterGrid<TCandidate, TAlgorithm, TSearchState>> grids, int repetitions)
    {
        Grids = grids;
        Repetitions = repetitions;
    }

    internal int ParameterCandidateCount => Grids.Sum(grid => grid.ParameterCandidateCount);

    internal (TAlgorithm Algorithm, ImmutableArray<(IParameter Parameter, object? Value)> Settings) Decode(int parameterCandidateIndex)
    {
        var position = parameterCandidateIndex;
        foreach (var grid in Grids)
        {
            var count = grid.ParameterCandidateCount;
            if (position < count)
                return DecodeParameterCandidate(grid, position, count, parameterCandidateIndex);

            position -= count;
        }

        throw new ArgumentOutOfRangeException(nameof(parameterCandidateIndex));
    }

    internal (TAlgorithm Algorithm, ImmutableArray<(IParameter Parameter, object? Value)> Settings)[] DecodeAll(int parameterCandidateCount)
    {
        var parameterCandidates = new (TAlgorithm Algorithm, ImmutableArray<(IParameter Parameter, object? Value)> Settings)[parameterCandidateCount];
        var parameterCandidateIndex = 0;
        foreach (var grid in Grids)
        {
            var count = grid.ParameterCandidateCount;
            for (var position = 0; position < count; position++)
            {
                parameterCandidates[parameterCandidateIndex] = DecodeParameterCandidate(grid, position, count, parameterCandidateIndex);
                parameterCandidateIndex++;
            }
        }

        return parameterCandidates;
    }

    private static (TAlgorithm Algorithm, ImmutableArray<(IParameter Parameter, object? Value)> Settings) DecodeParameterCandidate(
        ParameterGrid<TCandidate, TAlgorithm, TSearchState> grid, int position, int count, int parameterCandidateIndex)
    {
        try
        {
            return grid.Decode(position, count);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Decoding parameter candidate {parameterCandidateIndex} failed.", exception);
        }
    }
}

/// <summary>Creates experiments from one or more parameter grids.</summary>
public static class Experiment
{
    /// <summary>Creates an experiment that repeats each parameter candidate of the grid on each problem.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The repetition count is less than one.</exception>
    public static Experiment<TCandidate, TAlgorithm, TSearchState> Create<TCandidate, TAlgorithm, TSearchState>(
        ParameterGrid<TCandidate, TAlgorithm, TSearchState> grid, int repetitions = 1)
        where TAlgorithm : class, IAlgorithm<TCandidate, TSearchState>
        where TSearchState : class, ISearchState
    {
        return Create([grid], repetitions);
    }

    /// <summary>Creates an experiment over the supplied grids in order, repeating each parameter candidate on each problem.</summary>
    /// <exception cref="ArgumentException">The grid list is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The repetition count is less than one.</exception>
    public static Experiment<TCandidate, TAlgorithm, TSearchState> Create<TCandidate, TAlgorithm, TSearchState>(
        IReadOnlyList<ParameterGrid<TCandidate, TAlgorithm, TSearchState>> grids, int repetitions = 1)
        where TAlgorithm : class, IAlgorithm<TCandidate, TSearchState>
        where TSearchState : class, ISearchState
    {
        if (grids.Count == 0)
            throw new ArgumentException("An experiment requires at least one grid.", nameof(grids));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(repetitions);
        return new(grids.ToValueArray(), repetitions);
    }
}
