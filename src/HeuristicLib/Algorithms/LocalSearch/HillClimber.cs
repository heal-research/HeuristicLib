using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

public record HillClimber<TCandidate>
    : IterativeAlgorithm<HillClimber<TCandidate>, TCandidate, SingleSolutionState<TCandidate>>
{
    public required ICreator<TCandidate> Creator { get; init; }
    public required IMutator<TCandidate> Mutator { get; init; }
    public IEvaluator<TCandidate> Evaluator { get; init; } = HillClimberDefaults.Evaluator<TCandidate>();
    public IRefiner<TCandidate>? Refiner { get; init; }
    public override bool Fits(ExecutionSignature execution) => base.Fits(execution) && execution.Fits(Creator, Mutator, Evaluator, Refiner);

    public LocalSearchDirection Direction { get; init; } = HillClimberDefaults.Direction;
    public int MaxNeighbors { get; init; } = HillClimberDefaults.MaxNeighbors;
    public int BatchSize { get; init; } = HillClimberDefaults.BatchSize;

    protected override IterativeAlgorithmInstance<TCandidate, TRunSearchSpace, TRunProblem, SingleSolutionState<TCandidate>> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry, IInterceptorInstance<TCandidate, TRunSearchSpace, TRunProblem, SingleSolutionState<TCandidate>>? resolvedInterceptor)
    {
        var resolver = instanceRegistry.For<TCandidate, TRunSearchSpace, TRunProblem>();
        return new Instance<TRunSearchSpace, TRunProblem>(resolvedInterceptor, resolver.Resolve(Evaluator), resolver.Resolve(Creator), resolver.Resolve(Mutator), resolver.ResolveOptional(Refiner), Direction, MaxNeighbors, BatchSize);
    }

    private sealed class Instance<TSearchSpace, TProblem>(
        IInterceptorInstance<TCandidate, TSearchSpace, TProblem, SingleSolutionState<TCandidate>>? interceptor,
        IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> evaluator,
        ICreatorInstance<TCandidate, TSearchSpace, TProblem> creator,
        IMutatorInstance<TCandidate, TSearchSpace, TProblem> mutator,
        IRefinerInstance<TCandidate, TSearchSpace, TProblem>? refiner,
        LocalSearchDirection direction,
        int maxNeighbors,
        int batchSize)
        : IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, SingleSolutionState<TCandidate>>(interceptor)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        protected override SingleSolutionState<TCandidate> ExecuteStep(SingleSolutionState<TCandidate>? previousState, TProblem problem, IRandomNumberGenerator random) =>
            TryExecuteStep(previousState, problem, random, out var nextState)
                ? nextState
                : throw new InvalidOperationException("HillClimber has structurally completed and cannot produce another step.");

        protected override bool TryExecuteStep(SingleSolutionState<TCandidate>? previousState, TProblem problem, IRandomNumberGenerator random, [NotNullWhen(true)] out SingleSolutionState<TCandidate>? nextState)
        {
            if (previousState is null)
            {
                nextState = CreateInitialState(problem, random);
                return true;
            }

            if (!TryFindImprovement(previousState.EvaluatedCandidate, problem, random, out var improvement))
            {
                nextState = null;
                return false;
            }

            nextState = ToState(improvement);
            return true;
        }

        private SingleSolutionState<TCandidate> CreateInitialState(TProblem problem, IRandomNumberGenerator random)
        {
            var created = creator.Create(1, random, problem.SearchSpace, problem);
            if (refiner is not null)
            {
                created = refiner.Refine(created, random, problem.SearchSpace, problem);
            }

            var initialSolution = created[0];
            var initialCandidate = initialSolution.ToEvaluated(evaluator.Evaluate([initialSolution], random, problem.SearchSpace, problem)[0]);
            return ToState(initialCandidate);
        }

        private bool TryFindImprovement(EvaluatedCandidate<TCandidate> current, TProblem problem, IRandomNumberGenerator random, [NotNullWhen(true)] out EvaluatedCandidate<TCandidate>? improvement)
        {
            improvement = null;

            for (var i = 0; i < maxNeighbors; i += batchSize)
            {
                var candidates = mutator.Mutate(Enumerable.Repeat(current.Candidate, batchSize).ToArray(), random, problem.SearchSpace, problem);
                if (refiner is not null)
                {
                    candidates = refiner.Refine(candidates, random, problem.SearchSpace, problem);
                }

                var objectiveVectors = evaluator.Evaluate(candidates, random, problem.SearchSpace, problem);
                var bestIndex = BestSelector.Select(objectiveVectors, problem.Objective, count: 1)[0];

                if (problem.Objective.TotalOrderComparer.Compare(objectiveVectors[bestIndex], current.ObjectiveVector) >= 0)
                {
                    continue;
                }

                improvement = candidates[bestIndex].ToEvaluated(objectiveVectors[bestIndex]);
                if (direction == LocalSearchDirection.FirstImprovement)
                {
                    return true;
                }
            }

            return improvement is not null;
        }

        private static SingleSolutionState<TCandidate> ToState(EvaluatedCandidate<TCandidate> solution) => new() { Population = Population.From([solution]) };
    }
}

public static class HillClimber
{
    /// <summary>
    /// Creates a hill climber for a problem, asking the problem first and its search space second for every required
    /// operator the caller does not supply.
    /// </summary>
    /// <remarks>
    /// The creator and mutator are selected independently. An explicit argument wins, followed by a problem
    /// recommendation and then a search space recommendation. The evaluator comes from
    /// <see cref="HillClimberDefaults"/> when omitted. Other omitted operators remain null.
    /// </remarks>
    /// <exception cref="InvalidOperationException">No value or recommendation is available for one or more required operators.</exception>
    public static HillClimber<TCandidate> For<TProblem, TCandidate, TSearchSpace>(
        Problem<TProblem, TCandidate, TSearchSpace> problem,
        ICreator<TCandidate>? creator = null,
        IMutator<TCandidate>? mutator = null,
        IEvaluator<TCandidate>? evaluator = null,
        IRefiner<TCandidate>? refiner = null,
        IInterceptor<TCandidate>? interceptor = null,
        LocalSearchDirection direction = HillClimberDefaults.Direction,
        int maxNeighbors = HillClimberDefaults.MaxNeighbors,
        int batchSize = HillClimberDefaults.BatchSize)
        where TProblem : Problem<TProblem, TCandidate, TSearchSpace>
        where TSearchSpace : class, ISearchSpace<TCandidate>
    {
        var searchSpace = problem.SearchSpace;
        var recommendations = new OperatorRecommendationResolution(problem, searchSpace);
        creator = recommendations.GetOrRecommend(nameof(creator), creator);
        mutator = recommendations.GetOrRecommend(nameof(mutator), mutator);
        recommendations.ThrowIfIncomplete(nameof(HillClimber));

        return new()
        {
            Creator = creator!,
            Mutator = mutator!,
            Evaluator = evaluator ?? HillClimberDefaults.Evaluator<TCandidate>(),
            Refiner = refiner,
            Interceptor = interceptor,
            Direction = direction,
            MaxNeighbors = maxNeighbors,
            BatchSize = batchSize
        };
    }

    /// <summary>
    /// Creates a hill climber from a search space's creator and mutator recommendations, with no problem instance.
    /// </summary>
    /// <remarks>
    /// An explicit creator or mutator wins over its search space recommendation. The evaluator comes from
    /// <see cref="HillClimberDefaults"/> when omitted. Other omitted operators remain null.
    /// </remarks>
    /// <exception cref="InvalidOperationException">No value or recommendation is available for one or more required operators.</exception>
    public static HillClimber<TCandidate> For<TCandidate>(
        ISearchSpace<TCandidate> searchSpace,
        ICreator<TCandidate>? creator = null,
        IMutator<TCandidate>? mutator = null,
        IEvaluator<TCandidate>? evaluator = null,
        IRefiner<TCandidate>? refiner = null,
        IInterceptor<TCandidate>? interceptor = null,
        LocalSearchDirection direction = HillClimberDefaults.Direction,
        int maxNeighbors = HillClimberDefaults.MaxNeighbors,
        int batchSize = HillClimberDefaults.BatchSize)
    {
        var recommendations = new OperatorRecommendationResolution(problem: null, searchSpace);
        creator = recommendations.GetOrRecommend(nameof(creator), creator);
        mutator = recommendations.GetOrRecommend(nameof(mutator), mutator);
        recommendations.ThrowIfIncomplete(nameof(HillClimber));

        return new()
        {
            Creator = creator!,
            Mutator = mutator!,
            Evaluator = evaluator ?? HillClimberDefaults.Evaluator<TCandidate>(),
            Refiner = refiner,
            Interceptor = interceptor,
            Direction = direction,
            MaxNeighbors = maxNeighbors,
            BatchSize = batchSize
        };
    }

    /// <summary>
    /// Creates a hill climber from the operators it requires, inferring the candidate type from them. Every remaining
    /// member is optional and falls back to <see cref="HillClimberDefaults"/>.
    /// </summary>
    public static HillClimber<TCandidate> Create<TCandidate>(
        ICreator<TCandidate> creator,
        IMutator<TCandidate> mutator,
        IEvaluator<TCandidate>? evaluator = null,
        IRefiner<TCandidate>? refiner = null,
        IInterceptor<TCandidate>? interceptor = null,
        LocalSearchDirection direction = HillClimberDefaults.Direction,
        int maxNeighbors = HillClimberDefaults.MaxNeighbors,
        int batchSize = HillClimberDefaults.BatchSize) =>
        new()
        {
            Creator = creator,
            Mutator = mutator,
            Evaluator = evaluator ?? HillClimberDefaults.Evaluator<TCandidate>(),
            Refiner = refiner,
            Interceptor = interceptor,
            Direction = direction,
            MaxNeighbors = maxNeighbors,
            BatchSize = batchSize
        };

}
