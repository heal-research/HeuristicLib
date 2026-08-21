using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

public record HillClimber<TCandidate, TSearchSpace, TProblem>
    : IterativeAlgorithm<HillClimber<TCandidate, TSearchSpace, TProblem>, TCandidate, TSearchSpace, TProblem, SingleSolutionState<TCandidate>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public required ICreator<TCandidate, TSearchSpace, TProblem> Creator { get; init; }
    public required IMutator<TCandidate, TSearchSpace, TProblem> Mutator { get; init; }
    public IEvaluator<TCandidate, TSearchSpace, TProblem> Evaluator { get; init; } = HillClimberDefaults.Evaluator<TCandidate, TSearchSpace, TProblem>();
    public IRefiner<TCandidate, TSearchSpace, TProblem>? Refiner { get; init; }
    public LocalSearchDirection Direction { get; init; } = HillClimberDefaults.Direction;
    public int MaxNeighbors { get; init; } = HillClimberDefaults.MaxNeighbors;
    public int BatchSize { get; init; } = HillClimberDefaults.BatchSize;

    protected override IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, SingleSolutionState<TCandidate>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry, IInterceptorInstance<TCandidate, TSearchSpace, TProblem, SingleSolutionState<TCandidate>>? resolvedInterceptor) =>
        new Instance(resolvedInterceptor, instanceRegistry.Resolve(Evaluator), instanceRegistry.Resolve(Creator), instanceRegistry.Resolve(Mutator), instanceRegistry.ResolveOptional(Refiner), Direction, MaxNeighbors, BatchSize);

    private sealed class Instance(
        IInterceptorInstance<TCandidate, TSearchSpace, TProblem, SingleSolutionState<TCandidate>>? interceptor,
        IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> evaluator,
        ICreatorInstance<TCandidate, TSearchSpace, TProblem> creator,
        IMutatorInstance<TCandidate, TSearchSpace, TProblem> mutator,
        IRefinerInstance<TCandidate, TSearchSpace, TProblem>? refiner,
        LocalSearchDirection direction,
        int maxNeighbors,
        int batchSize)
        : IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, SingleSolutionState<TCandidate>>(interceptor)
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
    /// Creates a hill climber for a problem that states its own operator preferences, asking the problem first and
    /// falling back to the search space's encoding defaults for the required creator and mutator.
    /// </summary>
    public static HillClimber<TCandidate, TSearchSpace, TProblem> For<TProblem, TCandidate, TSearchSpace>(
        IProblemDefaults<TProblem, TCandidate, TSearchSpace> problem,
        ICreator<TCandidate, TSearchSpace, TProblem>? creator = null,
        IMutator<TCandidate, TSearchSpace, TProblem>? mutator = null,
        IEvaluator<TCandidate, TSearchSpace, TProblem>? evaluator = null,
        IRefiner<TCandidate, TSearchSpace, TProblem>? refiner = null,
        IInterceptor<TCandidate, TSearchSpace, TProblem, SingleSolutionState<TCandidate>>? interceptor = null,
        LocalSearchDirection direction = HillClimberDefaults.Direction,
        int maxNeighbors = HillClimberDefaults.MaxNeighbors,
        int batchSize = HillClimberDefaults.BatchSize)
        where TProblem : class,
                         IProblemDefaultCreator<TProblem, TCandidate, TSearchSpace>,
                         IProblemDefaultMutator<TProblem, TCandidate, TSearchSpace>
        where TSearchSpace : class, ISearchSpace<TCandidate>,
                             IEncodingDefaultCreator<TCandidate, TSearchSpace>,
                             IEncodingDefaultMutator<TCandidate, TSearchSpace>
    {
        var searchSpace = problem.SearchSpace;
        var self = problem as TProblem;

        return new()
        {
            Creator = creator ?? (self is null ? null : TProblem.CreateDefaultCreator(self)) ?? TSearchSpace.CreateDefaultCreator(searchSpace),
            Mutator = mutator ?? (self is null ? null : TProblem.CreateDefaultMutator(self)) ?? TSearchSpace.CreateDefaultMutator(searchSpace),
            Evaluator = evaluator ?? HillClimberDefaults.Evaluator<TCandidate, TSearchSpace, TProblem>(),
            Refiner = refiner,
            Interceptor = interceptor,
            Direction = direction,
            MaxNeighbors = maxNeighbors,
            BatchSize = batchSize
        };
    }

    /// <summary>
    /// Creates a hill climber from a search space's creator and mutator defaults, with no problem instance.
    /// </summary>
    public static HillClimber<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> For<TCandidate, TSearchSpace>(
        IEncodingDefaults<TCandidate, TSearchSpace> searchSpace,
        ICreator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>? creator = null,
        IMutator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>? mutator = null,
        IEvaluator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>? evaluator = null,
        IRefiner<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>? refiner = null,
        IInterceptor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, SingleSolutionState<TCandidate>>? interceptor = null,
        LocalSearchDirection direction = HillClimberDefaults.Direction,
        int maxNeighbors = HillClimberDefaults.MaxNeighbors,
        int batchSize = HillClimberDefaults.BatchSize)
        where TSearchSpace : class, ISearchSpace<TCandidate>,
                             IEncodingDefaultCreator<TCandidate, TSearchSpace>,
                             IEncodingDefaultMutator<TCandidate, TSearchSpace>
    {
        var typedSearchSpace = (TSearchSpace)searchSpace;

        return new()
        {
            Creator = creator ?? TSearchSpace.CreateDefaultCreator(typedSearchSpace),
            Mutator = mutator ?? TSearchSpace.CreateDefaultMutator(typedSearchSpace),
            Evaluator = evaluator ?? HillClimberDefaults.Evaluator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>(),
            Refiner = refiner,
            Interceptor = interceptor,
            Direction = direction,
            MaxNeighbors = maxNeighbors,
            BatchSize = batchSize
        };
    }

    /// <summary>
    /// Creates a hill climber from the operators it requires, inferring the candidate, search space and problem types
    /// from them. Every remaining member is optional and falls back to <see cref="HillClimberDefaults"/>.
    /// </summary>
    public static HillClimber<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(
        ICreator<TCandidate, TSearchSpace, TProblem> creator,
        IMutator<TCandidate, TSearchSpace, TProblem> mutator,
        IEvaluator<TCandidate, TSearchSpace, TProblem>? evaluator = null,
        IRefiner<TCandidate, TSearchSpace, TProblem>? refiner = null,
        IInterceptor<TCandidate, TSearchSpace, TProblem, SingleSolutionState<TCandidate>>? interceptor = null,
        LocalSearchDirection direction = HillClimberDefaults.Direction,
        int maxNeighbors = HillClimberDefaults.MaxNeighbors,
        int batchSize = HillClimberDefaults.BatchSize)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new()
        {
            Creator = creator,
            Mutator = mutator,
            Evaluator = evaluator ?? HillClimberDefaults.Evaluator<TCandidate, TSearchSpace, TProblem>(),
            Refiner = refiner,
            Interceptor = interceptor,
            Direction = direction,
            MaxNeighbors = maxNeighbors,
            BatchSize = batchSize
        };

}
