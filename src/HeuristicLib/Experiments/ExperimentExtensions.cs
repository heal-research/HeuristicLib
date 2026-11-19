using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Experiments;

public static class ExperimentExtensions
{
    extension<TSelf, TCandidate, TSearchState>(Algorithm<TSelf, TCandidate, TSearchState> algorithm)
        where TSelf : Algorithm<TSelf, TCandidate, TSearchState>
        where TSearchState : class, ISearchState
    {
        /// <summary>Starts a grid with this algorithm as its prototype.</summary>
        public ParameterGrid<TCandidate, TSelf, TSearchState> AsParameterGrid() => new(algorithm.Self);

        /// <summary>Creates an experiment that repeats this configuration on each problem.</summary>
        public Experiment<TCandidate, TSelf, TSearchState> Repeat(int repetitions)
        {
            var grid = algorithm.AsParameterGrid();
            return Experiment.Create(grid, repetitions);
        }
    }

    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        /// <summary>Starts a grid typed by the algorithm interface.</summary>
        public ParameterGrid<TCandidate, IAlgorithm<TCandidate, TSearchState>, TSearchState> AsParameterGrid() => new(algorithm);

        /// <summary>Creates an experiment that repeats this configuration on each problem.</summary>
        public Experiment<TCandidate, IAlgorithm<TCandidate, TSearchState>, TSearchState> Repeat(int repetitions)
        {
            var grid = algorithm.AsParameterGrid();
            return Experiment.Create(grid, repetitions);
        }
    }

    /// <summary>Adds a labelled dimension using a typed parameter descriptor, which may be declared on an ancestor type.</summary>
    /// <exception cref="ArgumentException">The value list is empty.</exception>
    public static ParameterGrid<TCandidate, TAlgorithm, TSearchState> VaryBy<TCandidate, TAlgorithm, TSearchState, TOwner, TValue>(
        this ParameterGrid<TCandidate, TAlgorithm, TSearchState> grid, IParameter<TOwner, TValue> parameter, IReadOnlyList<TValue> values)
        where TAlgorithm : class, IAlgorithm<TCandidate, TSearchState>, TOwner
        where TSearchState : class, ISearchState
        where TOwner : class, IConfigurationNode
    {
        var snapshot = ParameterGrid<TCandidate, TAlgorithm, TSearchState>.SnapshotValues(values, $"parameter '{parameter.Name}'");
        return grid.AddDimension(new ParameterDimension<TAlgorithm, TValue>(parameter, snapshot));
    }

    extension<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm>(Experiment<TCandidate, TAlgorithm, TSearchState> experiment)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : Problem<TProblem, TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
        where TAlgorithm : class, IAlgorithm<TCandidate, TSearchState>
    {
        /// <summary>Creates a ready run on one problem, applying substitutions and optionally validating each parameter candidate.</summary>
        public ExperimentRun<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm> CreateRun(
            Problem<TProblem, TCandidate, TSearchSpace> problem, IRandomNumberGenerator random, IReadOnlyList<ISubstitution>? substitutions = null, bool validate = true) =>
            experiment.CreateRun([problem], random, substitutions, validate);

        /// <summary>Creates a ready run over the supplied problems, applying substitutions once per parameter candidate and optionally validating each problem and parameter candidate.</summary>
        public ExperimentRun<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm> CreateRun(
            IReadOnlyList<Problem<TProblem, TCandidate, TSearchSpace>> problems, IRandomNumberGenerator random, IReadOnlyList<ISubstitution>? substitutions = null, bool validate = true)
        {
            var problemSnapshot = problems.Select(problem => (TProblem)problem).ToImmutableArray();
            return new(experiment, problemSnapshot, random, substitutions, validate);
        }

        /// <summary>Creates a fresh run and streams states from its trials.</summary>
        public ExecutionStream<ExperimentStreamEntry<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm>, TSearchState>> RunStreaming(Problem<TProblem, TCandidate, TSearchSpace> problem, IRandomNumberGenerator random, ExecutionConcurrency? concurrency = null, TSearchState? initialState = null, CancellationToken cancellationToken = default, bool validate = true)
        {
            var run = experiment.CreateRun(problem, random, validate: validate);
            return run.Stream(concurrency, initialState, cancellationToken);
        }

        /// <summary>Creates a fresh run and starts each trial, returning its task in trial order.</summary>
        public ImmutableArray<Task<(ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm> Trial, TSearchState State)>> RunTrials(Problem<TProblem, TCandidate, TSearchSpace> problem, IRandomNumberGenerator random, ExecutionConcurrency? concurrency = null, TSearchState? initialState = null, CancellationToken cancellationToken = default, bool validate = true)
        {
            var run = experiment.CreateRun(problem, random, validate: validate);
            return run.StartTrials(concurrency, initialState, cancellationToken);
        }

        /// <summary>Creates a fresh run and asynchronously collects the final state of each trial in trial order.</summary>
        public Task<ImmutableArray<(ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm> Trial, TSearchState State)>> RunToCompletionAsync(Problem<TProblem, TCandidate, TSearchSpace> problem, IRandomNumberGenerator random, ExecutionConcurrency? concurrency = null, TSearchState? initialState = null, CancellationToken cancellationToken = default, bool validate = true)
        {
            var run = experiment.CreateRun(problem, random, validate: validate);
            return run.CompleteAsync(concurrency, initialState, cancellationToken);
        }

        /// <summary>Creates a fresh run and blocks until it has collected the final state of each trial in trial order.</summary>
        public ImmutableArray<(ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm> Trial, TSearchState State)> RunToCompletion(Problem<TProblem, TCandidate, TSearchSpace> problem, IRandomNumberGenerator random, ExecutionConcurrency? concurrency = null, TSearchState? initialState = null, CancellationToken cancellationToken = default, bool validate = true)
        {
            var run = experiment.CreateRun(problem, random, validate: validate);
            return run.Complete(concurrency, initialState, cancellationToken);
        }
    }

    extension<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm>(ExperimentRun<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm> run)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
        where TAlgorithm : class, IAlgorithm<TCandidate, TSearchState>
    {
        public async Task<ImmutableArray<(ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm> Trial, TSearchState State)>> CompleteAsync(
            ExecutionConcurrency? concurrency = null, TSearchState? initialState = null, CancellationToken cancellationToken = default)
        {
            var trialTasks = run.StartTrials(concurrency, initialState, cancellationToken);
            try
            {
                return (await Task.WhenAll(trialTasks)).ToImmutableArray();
            }
            catch
            {
                cancellationToken.ThrowIfCancellationRequested();

                var failures = trialTasks.Select(task => task.Exception).OfType<AggregateException>().SelectMany(exception => exception.InnerExceptions).ToList();
                if (failures.Count > 0)
                    throw new AggregateException(failures);

                throw;
            }
            finally
            {
                await run.ExecutionCompletion;
            }
        }

        public ImmutableArray<(ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm> Trial, TSearchState State)> Complete(
            ExecutionConcurrency? concurrency = null, TSearchState? initialState = null, CancellationToken cancellationToken = default)
        {
            return run.CompleteAsync(concurrency, initialState, cancellationToken).GetAwaiter().GetResult();
        }
    }

    /// <remarks>
    /// For a problem held behind its interface. The run is then typed at the interface rather than at the concrete
    /// problem.
    /// </remarks>
    extension<TCandidate, TSearchSpace, TSearchState, TAlgorithm>(Experiment<TCandidate, TAlgorithm, TSearchState> experiment)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TSearchState : class, ISearchState
        where TAlgorithm : class, IAlgorithm<TCandidate, TSearchState>
    {
        /// <summary>Creates a ready run bound to the problem interface.</summary>
        public ExperimentRun<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState, TAlgorithm> CreateRun(
            IProblem<TCandidate, TSearchSpace> problem, IRandomNumberGenerator random, IReadOnlyList<ISubstitution>? substitutions = null, bool validate = true) =>
            experiment.CreateRun([problem], random, substitutions, validate);

        /// <summary>Creates a ready run over problems that share a candidate and search-space type.</summary>
        public ExperimentRun<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState, TAlgorithm> CreateRun(
            IReadOnlyList<IProblem<TCandidate, TSearchSpace>> problems, IRandomNumberGenerator random, IReadOnlyList<ISubstitution>? substitutions = null, bool validate = true)
        {
            var problemSnapshot = problems.ToImmutableArray();
            return new(experiment, problemSnapshot, random, substitutions, validate);
        }

        /// <summary>Creates a fresh run bound to the problem interface and streams states from its trials.</summary>
        public ExecutionStream<ExperimentStreamEntry<ExperimentTrial<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState, TAlgorithm>, TSearchState>> RunStreaming(
            IProblem<TCandidate, TSearchSpace> problem, IRandomNumberGenerator random, ExecutionConcurrency? concurrency = null, TSearchState? initialState = null, CancellationToken cancellationToken = default, bool validate = true)
        {
            var run = experiment.CreateRun(problem, random, validate: validate);
            return run.Stream(concurrency, initialState, cancellationToken);
        }

        /// <summary>Creates a fresh run bound to the problem interface and starts its trials, returning the tasks in trial order.</summary>
        public ImmutableArray<Task<(ExperimentTrial<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState, TAlgorithm> Trial, TSearchState State)>> RunTrials(
            IProblem<TCandidate, TSearchSpace> problem, IRandomNumberGenerator random, ExecutionConcurrency? concurrency = null, TSearchState? initialState = null, CancellationToken cancellationToken = default, bool validate = true)
        {
            var run = experiment.CreateRun(problem, random, validate: validate);
            return run.StartTrials(concurrency, initialState, cancellationToken);
        }

        /// <summary>Creates a fresh run bound to the problem interface and asynchronously collects its final states in trial order.</summary>
        public Task<ImmutableArray<(ExperimentTrial<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState, TAlgorithm> Trial, TSearchState State)>> RunToCompletionAsync(
            IProblem<TCandidate, TSearchSpace> problem, IRandomNumberGenerator random, ExecutionConcurrency? concurrency = null, TSearchState? initialState = null, CancellationToken cancellationToken = default, bool validate = true)
        {
            var run = experiment.CreateRun(problem, random, validate: validate);
            return run.CompleteAsync(concurrency, initialState, cancellationToken);
        }

        /// <summary>Creates a fresh run bound to the problem interface and blocks until it has collected its final states in trial order.</summary>
        public ImmutableArray<(ExperimentTrial<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState, TAlgorithm> Trial, TSearchState State)> RunToCompletion(
            IProblem<TCandidate, TSearchSpace> problem, IRandomNumberGenerator random, ExecutionConcurrency? concurrency = null, TSearchState? initialState = null, CancellationToken cancellationToken = default, bool validate = true)
        {
            var run = experiment.CreateRun(problem, random, validate: validate);
            return run.Complete(concurrency, initialState, cancellationToken);
        }
    }
}
