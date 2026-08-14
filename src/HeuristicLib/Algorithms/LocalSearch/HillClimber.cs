using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.LocalSearch;

public record HillClimber<TCandidate, TSearchSpace, TProblem>
    : IterativeAlgorithm<HillClimber<TCandidate, TSearchSpace, TProblem>, TCandidate, TSearchSpace, TProblem, SingleSolutionState<TCandidate>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public required ICreator<TCandidate, TSearchSpace, TProblem> Creator { get; init; }
    public required IMutator<TCandidate, TSearchSpace, TProblem> Mutator { get; init; }
    public IEvaluator<TCandidate, TSearchSpace, TProblem> Evaluator { get; init; } = new ProblemEvaluator<TCandidate, TSearchSpace, TProblem>();
    public required LocalSearchDirection Direction { get; init; }
    public required int MaxNeighbors { get; init; }
    public required int BatchSize { get; init; }

    protected override IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, SingleSolutionState<TCandidate>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry, IInterceptorInstance<TCandidate, TSearchSpace, TProblem, SingleSolutionState<TCandidate>>? resolvedInterceptor) =>
        new Instance(resolvedInterceptor, instanceRegistry.Resolve(Evaluator), instanceRegistry.Resolve(Creator), instanceRegistry.Resolve(Mutator), Direction, MaxNeighbors, BatchSize);

    private sealed class Instance(
        IInterceptorInstance<TCandidate, TSearchSpace, TProblem, SingleSolutionState<TCandidate>>? interceptor,
        IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> evaluator,
        ICreatorInstance<TCandidate, TSearchSpace, TProblem> creator,
        IMutatorInstance<TCandidate, TSearchSpace, TProblem> mutator,
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
            var initialSolution = creator.Create(1, random, problem.SearchSpace, problem)[0];
            var initialCandidate = evaluator.Evaluate([initialSolution], random, problem.SearchSpace, problem)[0];
            return ToState(initialCandidate);
        }

        private bool TryFindImprovement(EvaluatedCandidate<TCandidate> current, TProblem problem, IRandomNumberGenerator random, [NotNullWhen(true)] out EvaluatedCandidate<TCandidate>? improvement)
        {
            improvement = null;

            for (var i = 0; i < maxNeighbors; i += batchSize)
            {
                var candidates = mutator.Mutate(Enumerable.Repeat(current.Candidate, batchSize).ToArray(), random, problem.SearchSpace, problem);
                var evaluatedCandidates = evaluator.Evaluate(candidates, random, problem.SearchSpace, problem);
                var objectiveVectors = evaluatedCandidates.Select(evaluatedCandidate => evaluatedCandidate.ObjectiveVector).ToArray();
                var bestIndex = BestSelector.Select(objectiveVectors, problem.Objective, count: 1)[0];

                if (problem.Objective.TotalOrderComparer.Compare(objectiveVectors[bestIndex], current.ObjectiveVector) >= 0)
                {
                    continue;
                }

                improvement = evaluatedCandidates[bestIndex];
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
    public static HillClimberBuilder<TCandidate, TSearchSpace, TProblem> GetBuilder<TCandidate, TSearchSpace, TProblem>(ICreator<TCandidate, TSearchSpace, TProblem> creator, IMutator<TCandidate, TSearchSpace, TProblem> mutator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        return new()
        {
            Mutator = mutator,
            Creator = creator
        };
    }
}
