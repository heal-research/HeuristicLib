using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Experiments;

public sealed record GridExperiment<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm>
    : Experiment<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TAlgorithm>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
    where TAlgorithm : class, IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
{
    public Grid<TAlgorithm> ParameterGrid { get; }

    public GridExperiment(TAlgorithm algorithm) => ParameterGrid = Grid.Create(algorithm);

    private GridExperiment(Grid<TAlgorithm> parameterGrid) => ParameterGrid = parameterGrid;

    public GridExperiment<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm> VaryBy<TValue>(IReadOnlyList<TValue> values, Func<TAlgorithm, TValue, TAlgorithm> configurator) =>
        new(ParameterGrid.VaryBy(values, configurator));

    public override IReadOnlyList<ExperimentCase<TAlgorithm, TAlgorithm>> MaterializeCases()
    {
        var configurations = ParameterGrid.GetConfigurations();
        if (configurations.Count != configurations.Distinct().Count())
        {
            throw new InvalidOperationException("A grid produced equal algorithm configurations. Use Repeat to execute the same configuration more than once.");
        }

        return configurations.Select((algorithm, index) => new ExperimentCase<TAlgorithm, TAlgorithm>(algorithm, algorithm, [index])).ToList();
    }
}

public static class GridExperimentExtensions
{
    extension<TAlgorithm, TCandidate, TSearchSpace, TProblem, TSearchState>(Algorithm<TAlgorithm, TCandidate, TSearchSpace, TProblem, TSearchState> algorithm)
        where TAlgorithm : Algorithm<TAlgorithm, TCandidate, TSearchSpace, TProblem, TSearchState>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public GridExperiment<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm> AsGrid() => new(algorithm.Self);
    }

    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public GridExperiment<TCandidate, TSearchSpace, TProblem, TSearchState, IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>> AsGrid() => new(algorithm);
    }
}
