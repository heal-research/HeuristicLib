using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Experiments;

public static class ExperimentExtensions
{
    extension<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>(IExperiment<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey> experiment)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
        where TAlgorithm : class, IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
    {
        public ExperimentRun<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey> CreateRun(TProblem problem, IRandomNumberGenerator random) => new(experiment, problem, random);

        public Execution.ExecutionStream<ExperimentStreamEntry<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>, TSearchState>> Stream(TProblem problem, IRandomNumberGenerator random, ExperimentExecutionPolicy? policy = null, TSearchState? initialState = null, CancellationToken cancellationToken = default) =>
            experiment.CreateRun(problem, random).Stream(policy, initialState, cancellationToken);

        public Task<IReadOnlyList<(ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey> Trial, TSearchState State)>> CompleteAsync(TProblem problem, IRandomNumberGenerator random, ExperimentExecutionPolicy? policy = null, TSearchState? initialState = null, CancellationToken cancellationToken = default) =>
            experiment.CreateRun(problem, random).CompleteAsync(policy, initialState, cancellationToken);

        public IReadOnlyList<(ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey> Trial, TSearchState State)> Complete(TProblem problem, IRandomNumberGenerator random, ExperimentExecutionPolicy? policy = null, TSearchState? initialState = null, CancellationToken cancellationToken = default) =>
            experiment.CreateRun(problem, random).Complete(policy, initialState, cancellationToken);
    }
}
