using HEAL.HeuristicLib.Algorithms;

namespace HEAL.HeuristicLib.Experiments;

public sealed record GridExperiment<TCandidate, TAlgorithm, TSearchState>
    : Experiment<TCandidate, TAlgorithm, TSearchState, TAlgorithm>
    where TAlgorithm : class, IAlgorithm<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    public Grid<TAlgorithm> ParameterGrid { get; }

    public GridExperiment(TAlgorithm algorithm)
    {
        ParameterGrid = Grid.Create(algorithm);
    }

    private GridExperiment(Grid<TAlgorithm> parameterGrid)
    {
        ParameterGrid = parameterGrid;
    }

    public GridExperiment<TCandidate, TAlgorithm, TSearchState> VaryBy<TValue>(IReadOnlyList<TValue> values, Func<TAlgorithm, TValue, TAlgorithm> configurator) =>
        new(ParameterGrid.VaryBy(values, configurator));

    public override ImmutableArray<ExperimentCase<TAlgorithm, TAlgorithm>> MaterializeCases()
    {
        var configurations = ParameterGrid.GetConfigurations();
        if (configurations.Length != configurations.Distinct().Count())
        {
            throw new InvalidOperationException("A grid produced equal algorithm configurations. Use Repeat to execute the same configuration more than once.");
        }

        return configurations.Select((algorithm, index) => ExperimentCase.From(algorithm, algorithm, [index])).ToImmutableArray();
    }
}

public static class GridExperimentExtensions
{
    extension<TSelf, TCandidate, TSearchState>(Algorithm<TSelf, TCandidate, TSearchState> algorithm)
        where TSelf : Algorithm<TSelf, TCandidate, TSearchState>
        where TSearchState : class, ISearchState
    {
        public GridExperiment<TCandidate, TSelf, TSearchState> AsGrid() => new(algorithm.Self);
    }

    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        public GridExperiment<TCandidate, IAlgorithm<TCandidate, TSearchState>, TSearchState> AsGrid() => new(algorithm);
    }
}
