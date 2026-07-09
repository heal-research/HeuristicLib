using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
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

public record EvolutionStrategyState<TCandidate> : PopulationState<TCandidate>
{
    public required double MutationStrength { get; init; }
}

public record EvolutionStrategy<TCandidate, TSearchSpace, TProblem>
  : IterativeAlgorithm<TCandidate, TSearchSpace, TProblem, EvolutionStrategyState<TCandidate>, EvolutionStrategy<TCandidate, TSearchSpace, TProblem>.ExecutionState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public new sealed class ExecutionState
      : IterativeAlgorithm<TCandidate, TSearchSpace, TProblem, EvolutionStrategyState<TCandidate>, ExecutionState>.ExecutionState
    {
        public required ICreatorInstance<TCandidate, TSearchSpace, TProblem> Creator { get; init; }
        public required IMutatorInstance<TCandidate, TSearchSpace, TProblem> Mutator { get; init; }
        public required ISelectorInstance<TCandidate, TSearchSpace, TProblem> Selector { get; init; }
        public ICrossoverInstance<TCandidate, TSearchSpace, TProblem>? Crossover { get; init; }
        public IVariableStrengthMutator<TCandidate, TSearchSpace, TProblem>? VariableStrengthMutator { get; init; }
    }

    public required int PopulationSize { get; init; }
    public required int NumberOfChildren { get; init; }
    public required EvolutionStrategyType Strategy { get; init; }
    public required ICreator<TCandidate, TSearchSpace, TProblem> Creator { get; init; }
    public required IMutator<TCandidate, TSearchSpace, TProblem> Mutator { get; init; }
    public required ICrossover<TCandidate, TSearchSpace, TProblem>? Crossover { get; init; }
    public double InitialMutationStrength { get; init; } = 1.0;
    public required ISelector<TCandidate, TSearchSpace, TProblem> Selector { get; init; }
    public int? MaximumGenerations
    {
        get;
        init => field = value is null or > 0
          ? value
          : throw new ArgumentOutOfRangeException(nameof(MaximumGenerations), "MaximumGenerations must be positive when set.");
    }

    protected override ExecutionState CreateInitialExecutionState(IExecutionInstanceResolver resolver)
    {
        return new ExecutionState
        {
            Evaluator = resolver.Resolve(Evaluator),
            Interceptor = Interceptor is not null ? resolver.Resolve(Interceptor) : null,
            Creator = resolver.Resolve(Creator),
            Mutator = resolver.Resolve(Mutator),
            Selector = resolver.Resolve(Selector),
            Crossover = Crossover is not null ? resolver.Resolve(Crossover) : null,
            VariableStrengthMutator = Mutator as IVariableStrengthMutator<TCandidate, TSearchSpace, TProblem>
        };
    }

    protected override bool HasCompleted(
      int yieldedStateCount,
      EvolutionStrategyState<TCandidate>? previousState,
      ExecutionState executionState,
      TProblem problem)
    {
        return MaximumGenerations is not null && yieldedStateCount >= MaximumGenerations.Value;
    }

    protected override EvolutionStrategyState<TCandidate> ExecuteStep(
      EvolutionStrategyState<TCandidate>? previousState,
      ExecutionState executionState,
      TProblem problem,
      IRandomNumberGenerator random)
    {
        if (previousState is null)
        {
            var initialPopulation = executionState.Creator.Create(PopulationSize, random, problem.SearchSpace, problem);
            var objectives = executionState.Evaluator.Evaluate(initialPopulation, random, problem.SearchSpace, problem);
            return new EvolutionStrategyState<TCandidate>
            {
                Population = Population.From(initialPopulation, objectives),
                MutationStrength = InitialMutationStrength
            };
        }

        IReadOnlyList<TCandidate> parents;
        IReadOnlyList<ObjectiveVector> parentQualities;

        if (executionState.Crossover is null)
        {
            var parentSolutions = executionState.Selector.Select(
              previousState.Population.EvaluatedCandidates,
              problem.Objective,
              NumberOfChildren,
              random,
              problem.SearchSpace,
              problem);
            parents = parentSolutions.Select(x => x.Candidate).ToArray();
            parentQualities = parentSolutions.Select(x => x.ObjectiveVector).ToArray();
        }
        else
        {
            var parentSolutions = executionState.Selector.Select(
              previousState.Population.EvaluatedCandidates,
              problem.Objective,
              NumberOfChildren * 2,
              random,
              problem.SearchSpace,
              problem);
            parents = executionState.Crossover.Cross(parentSolutions.ToParents(problem.Objective), random, problem.SearchSpace, problem);
            parentQualities = parentSolutions.Where((_, i) => i % 2 == 0).Select(x => x.ObjectiveVector).ToArray();
        }

        var children = executionState.Mutator.Mutate(parents, random, problem.SearchSpace, problem);
        var fitnesses = executionState.Evaluator.Evaluate(children, random, problem.SearchSpace, problem);

        var newMutationStrength = previousState.MutationStrength;
        if (executionState.VariableStrengthMutator is not null)
        {
            var successes = parentQualities.Zip(fitnesses)
              .Count(t => t.Second.CompareTo(t.First, problem.Objective) == DominanceRelation.Dominates);
            var successRate = successes / (double)PopulationSize;
            newMutationStrength *= successRate switch
            {
                > 0.2 => 1.5,
                < 0.2 => 1 / 1.5,
                _ => 1
            };
            executionState.VariableStrengthMutator.MutationStrength = newMutationStrength;
        }

        var population = Population.From(children, fitnesses);
        var newPopulation = Strategy switch
        {
            EvolutionStrategyType.Comma => ElitismReplacer<TCandidate>.Replace(previousState.Population.EvaluatedCandidates, population.EvaluatedCandidates, problem.Objective, NumberOfChildren, 0),
            EvolutionStrategyType.Plus => PlusSelectionReplacer<TCandidate>.Replace(previousState.Population.EvaluatedCandidates, population.EvaluatedCandidates, problem.Objective, NumberOfChildren),
            _ => throw new InvalidOperationException($"Unknown strategy {Strategy}")
        };

        return new EvolutionStrategyState<TCandidate>
        {
            Population = Population.From(newPopulation),
            MutationStrength = newMutationStrength
        };
    }
}

public static class EvolutionStrategy
{
    public static EvolutionStrategyBuilder<TCandidate, TSearchSpace, TProblem> GetBuilder<TCandidate, TSearchSpace, TProblem>(
      ICreator<TCandidate, TSearchSpace, TProblem> creator,
      IMutator<TCandidate, TSearchSpace, TProblem> mutator)
      where TSearchSpace : class, ISearchSpace<TCandidate> where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        return new()
        {
            Mutator = mutator,
            InitialMutationStrength = (mutator as IVariableStrengthMutator<TCandidate, TSearchSpace, TProblem>)?.MutationStrength ?? 0,
            Creator = creator
        };
    }
}
