using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary;

public enum EvolutionStrategyType
{
    Comma,
    Plus
}

public record EvolutionStrategy<TCandidate, TSearchSpace, TProblem>
    : IterativeAlgorithm<EvolutionStrategy<TCandidate, TSearchSpace, TProblem>, TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public required int PopulationSize { get; init; }
    public required int NumberOfChildren { get; init; }
    public required EvolutionStrategyType Strategy { get; init; }
    public required ICreator<TCandidate, TSearchSpace, TProblem> Creator { get; init; }
    public required IMutator<TCandidate, TSearchSpace, TProblem> Mutator { get; init; }
    public required ICrossover<TCandidate, TSearchSpace, TProblem>? Crossover { get; init; }
    public IEvaluator<TCandidate, TSearchSpace, TProblem> Evaluator { get; init; } = new ProblemEvaluator<TCandidate, TSearchSpace, TProblem>();
    public required ISelector<TCandidate, TSearchSpace, TProblem> Selector { get; init; }
    public IRefiner<TCandidate, TSearchSpace, TProblem>? Refiner { get; init; }

    /// <summary>
    /// Gets the generation limit, or <see langword="null"/> for no limit. The expected value is positive.
    /// </summary>
    /// <remarks>A nonpositive limit completes before the first generation is produced.</remarks>
    public int? MaximumGenerations { get; init; }

    protected override IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry, IInterceptorInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? resolvedInterceptor)
    {
        var mutator = instanceRegistry.Resolve(Mutator);
        return new Instance(resolvedInterceptor, instanceRegistry.Resolve(Evaluator), instanceRegistry.Resolve(Creator), mutator, instanceRegistry.Resolve(Selector), instanceRegistry.ResolveOptional(Crossover), instanceRegistry.ResolveOptional(Refiner), PopulationSize, NumberOfChildren, Strategy, MaximumGenerations);
    }

    private sealed class Instance(
        IInterceptorInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? interceptor,
        IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> evaluator,
        ICreatorInstance<TCandidate, TSearchSpace, TProblem> creator,
        IMutatorInstance<TCandidate, TSearchSpace, TProblem> mutator,
        ISelectorInstance<TCandidate, TSearchSpace, TProblem> selector,
        ICrossoverInstance<TCandidate, TSearchSpace, TProblem>? crossover,
        IRefinerInstance<TCandidate, TSearchSpace, TProblem>? refiner,
        int populationSize,
        int numberOfChildren,
        EvolutionStrategyType strategy,
        int? maximumGenerations)
        : IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>(interceptor)
    {
        protected override bool HasCompleted(int yieldedStateCount, PopulationState<TCandidate>? previousState, TProblem problem) =>
            yieldedStateCount >= maximumGenerations;

        protected override PopulationState<TCandidate> ExecuteStep(PopulationState<TCandidate>? previousState, TProblem problem, IRandomNumberGenerator random)
        {
            if (previousState is null)
            {
                var initialPopulation = creator.Create(populationSize, random, problem.SearchSpace, problem);
                if (refiner is not null)
                {
                    initialPopulation = refiner.Refine(initialPopulation, random, problem.SearchSpace, problem);
                }

                var objectiveVectors = evaluator.Evaluate(initialPopulation, random, problem.SearchSpace, problem);
                return Population.From(initialPopulation.ToEvaluated(objectiveVectors)).ToPopulationState();
            }

            IReadOnlyList<TCandidate> parents;
            IReadOnlyList<ObjectiveVector> parentQualities;

            if (crossover is null)
            {
                var parentSolutions = selector.Select(previousState.Population.EvaluatedCandidates, problem.Objective, numberOfChildren, random, problem.SearchSpace, problem);
                parents = parentSolutions.Select(x => x.Candidate).ToArray();
                parentQualities = parentSolutions.Select(x => x.ObjectiveVector).ToArray();
            }
            else
            {
                var parentSolutions = selector.Select(previousState.Population.EvaluatedCandidates, problem.Objective, numberOfChildren * 2, random, problem.SearchSpace, problem);
                parents = crossover.Cross(parentSolutions.ToParents(problem.Objective), random, problem.SearchSpace, problem);
                parentQualities = parentSolutions.Where((_, i) => i % 2 == 0).Select(x => x.ObjectiveVector).ToArray();
            }

            var children = mutator.Mutate(parents, random, problem.SearchSpace, problem);
            if (refiner is not null)
            {
                children = refiner.Refine(children, random, problem.SearchSpace, problem);
            }

            var evaluatedChildren = children.ToEvaluated(evaluator.Evaluate(children, random, problem.SearchSpace, problem));

            if (mutator is IVariableStrengthMutatorInstance<TCandidate, TSearchSpace, TProblem> variableStrengthMutator)
            {
                var successes = parentQualities.Zip(evaluatedChildren).Count(
                    pair => pair.Second.ObjectiveVector.CompareTo(pair.First, problem.Objective) == DominanceRelation.Dominates);
                var successRate = successes / (double)populationSize;
                variableStrengthMutator.CurrentMutationStrength *= successRate switch
                {
                    > 0.2 => 1.5,
                    < 0.2 => 1 / 1.5,
                    _ => 1
                };
            }

            var population = Population.From(evaluatedChildren);
            var newPopulation = strategy switch
            {
                EvolutionStrategyType.Comma => ElitismReplacer.Replace(previousState.Population.EvaluatedCandidates, population.EvaluatedCandidates, problem.Objective, numberOfChildren, 0),
                EvolutionStrategyType.Plus => PlusSelectionReplacer.Replace(previousState.Population.EvaluatedCandidates, population.EvaluatedCandidates, problem.Objective, numberOfChildren),
                _ => throw new InvalidOperationException($"Unknown strategy {strategy}")
            };

            return Population.From(newPopulation).ToPopulationState();
        }
    }
}

public static class EvolutionStrategy
{
    public static EvolutionStrategyBuilder<TCandidate, TSearchSpace, TProblem> GetBuilder<TCandidate, TSearchSpace, TProblem>(ICreator<TCandidate, TSearchSpace, TProblem> creator, IMutator<TCandidate, TSearchSpace, TProblem> mutator)
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
