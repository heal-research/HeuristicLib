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
    : IterativeAlgorithm<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public required int PopulationSize { get; init; }
    public required int NumberOfChildren { get; init; }
    public required EvolutionStrategyType Strategy { get; init; }
    public required ICreator<TCandidate, TSearchSpace, TProblem> Creator { get; init; }
    public required IMutator<TCandidate, TSearchSpace, TProblem> Mutator { get; init; }
    public required ICrossover<TCandidate, TSearchSpace, TProblem>? Crossover { get; init; }
    public IEvaluator<TCandidate, TSearchSpace, TProblem> Evaluator { get; init; } = new DirectEvaluator<TCandidate>();
    public required ISelector<TCandidate, TSearchSpace, TProblem> Selector { get; init; }
    public int? MaximumGenerations
    {
        get;
        init => field = value is null or > 0
          ? value
          : throw new ArgumentOutOfRangeException(nameof(MaximumGenerations), "MaximumGenerations must be positive when set.");
    }

    protected override IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>> CreateIterativeAlgorithmInstance(ExecutionInstanceRegistry registry, IInterceptorInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? resolvedInterceptor)
    {
        var mutator = registry.Resolve(Mutator);
        return new Instance(resolvedInterceptor, registry.Resolve(Evaluator), registry.Resolve(Creator), mutator, registry.Resolve(Selector), Crossover is null ? null : registry.Resolve(Crossover), PopulationSize, NumberOfChildren, Strategy, MaximumGenerations);
    }

    private sealed class Instance(
        IInterceptorInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? interceptor,
        IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> evaluator,
        ICreatorInstance<TCandidate, TSearchSpace, TProblem> creator,
        IMutatorInstance<TCandidate, TSearchSpace, TProblem> mutator,
        ISelectorInstance<TCandidate, TSearchSpace, TProblem> selector,
        ICrossoverInstance<TCandidate, TSearchSpace, TProblem>? crossover,
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
                var objectives = evaluator.Evaluate(initialPopulation, random, problem.SearchSpace, problem);
                return new PopulationState<TCandidate>
                {
                    Population = Population.From(initialPopulation, objectives)
                };
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
            var fitnesses = evaluator.Evaluate(children, random, problem.SearchSpace, problem);

            if (mutator is IVariableStrengthMutatorInstance<TCandidate, TSearchSpace, TProblem> variableStrengthMutator)
            {
                var successes = parentQualities.Zip(fitnesses).Count(t => t.Second.CompareTo(t.First, problem.Objective) == DominanceRelation.Dominates);
                var successRate = successes / (double)populationSize;
                variableStrengthMutator.CurrentMutationStrength *= successRate switch
                {
                    > 0.2 => 1.5,
                    < 0.2 => 1 / 1.5,
                    _ => 1
                };
            }

            var population = Population.From(children, fitnesses);
            var newPopulation = strategy switch
            {
                EvolutionStrategyType.Comma => ElitismReplacer<TCandidate>.Replace(previousState.Population.EvaluatedCandidates, population.EvaluatedCandidates, problem.Objective, numberOfChildren, 0),
                EvolutionStrategyType.Plus => PlusSelectionReplacer<TCandidate>.Replace(previousState.Population.EvaluatedCandidates, population.EvaluatedCandidates, problem.Objective, numberOfChildren),
                _ => throw new InvalidOperationException($"Unknown strategy {strategy}")
            };

            return new PopulationState<TCandidate>
            {
                Population = Population.From(newPopulation)
            };
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
