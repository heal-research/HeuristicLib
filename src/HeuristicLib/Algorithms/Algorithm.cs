using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public abstract record Algorithm<TCandidate, TSearchSpace, TProblem, TSearchState, TExecutionState>
  : IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : class, ISearchState
  where TExecutionState : Algorithm<TCandidate, TSearchSpace, TProblem, TSearchState, TExecutionState>.ExecutionState
{
    public class ExecutionState
    {
        public required IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> Evaluator { get; init; }
    }

    // NOTE: Evaluator remains part of the base algorithm contract for now.
    public IEvaluator<TCandidate, TSearchSpace, TProblem> Evaluator { get; init; } = new DirectEvaluator<TCandidate>();

    protected abstract TExecutionState CreateInitialExecutionState(IExecutionInstanceResolver resolver);

    protected abstract IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateAlgorithmInstance(Run run, TExecutionState executionState);

    public IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
    {
        return CreateAlgorithmInstance(instanceRegistry.Run, CreateInitialExecutionState(instanceRegistry));
    }
}

public abstract class AlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
  : IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : class, ISearchState
{
    protected readonly IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> Evaluator;

    public Run Run { get; }

    protected AlgorithmInstance(Run run, IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> evaluator)
    {
        Run = run;
        Evaluator = evaluator;
    }

    public abstract IAsyncEnumerable<TSearchState> RunStreamingAsync(
        TProblem problem,
        IRandomNumberGenerator random,
        TSearchState? initialState = null,
        CancellationToken ct = default);
}

public static class AlgorithmExtensions
{
    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm)
      where TSearchSpace : class, ISearchSpace<TCandidate>
      where TProblem : class, IProblem<TCandidate, TSearchSpace>
      where TSearchState : class, ISearchState
    {
        public Run<TCandidate, TSearchSpace, TProblem, TSearchState> CreateRun(TProblem problem, params IReadOnlyList<IAnalyzer> analyzers)
        {
            return new Run<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm, problem, analyzers);
        }

        public IAsyncEnumerable<TSearchState> RunStreamingAsync(
            TProblem problem,
            IRandomNumberGenerator random,
            TSearchState? initialState = null,
            CancellationToken ct = default)
        {
            var run = algorithm.Run(problem);
            return run.StreamAsync(random, initialState, ct);
        }

        public async Task<TSearchState> RunToCompletionAsync(
            TProblem problem,
            IRandomNumberGenerator random,
            TSearchState? initialState = null,
            CancellationToken ct = default
        )
        {
            var run = algorithm.Run(problem);
            return await run.CompleteAsync(random, initialState, ct);
        }

        public IEnumerable<TSearchState> RunStreaming(
            TProblem problem,
            IRandomNumberGenerator random,
            TSearchState? initialState = null,
            CancellationToken ct = default
        )
        {
            var run = algorithm.Run(problem);
            return run.Stream(random, initialState, ct);
        }

        public TSearchState RunToCompletion(
            TProblem problem,
            IRandomNumberGenerator random,
            TSearchState? initialState = null,
            CancellationToken ct = default
        )
        {
            var run = algorithm.Run(problem);
            return run.Complete(random, initialState, ct);
        }
    }

    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> algorithmInstance)
      where TSearchSpace : class, ISearchSpace<TCandidate>
      where TProblem : class, IProblem<TCandidate, TSearchSpace>
      where TSearchState : class, ISearchState
    {
        public async Task<TSearchState> RunToCompletionAsync(
            TProblem problem,
            IRandomNumberGenerator random,
            TSearchState? initialState = null,
            CancellationToken ct = default
        )
        {
            return await algorithmInstance.RunStreamingAsync(problem, random, initialState, ct).LastAsync(ct);
        }

        public IEnumerable<TSearchState> RunStreaming(
            TProblem problem,
            IRandomNumberGenerator random,
            TSearchState? initialState = null,
            CancellationToken ct = default
        )
        {
            return algorithmInstance.RunStreamingAsync(problem, random, initialState, ct).ToBlockingEnumerable(ct);
        }

        public TSearchState RunToCompletion(
            TProblem problem,
            IRandomNumberGenerator random,
            TSearchState? initialState = null,
            CancellationToken ct = default
        )
        {
            return algorithmInstance.RunToCompletionAsync(problem, random, initialState, ct).GetAwaiter().GetResult();
        }
    }
}
