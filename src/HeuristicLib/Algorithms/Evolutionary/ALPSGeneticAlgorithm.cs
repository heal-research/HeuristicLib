using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary;

public record AlpsState<TCandidate> : SearchState
{
    public required IReadOnlyList<Population<TCandidate>> Population { get; init; }
    public required IReadOnlyList<IReadOnlyList<int>> Ages { get; init; }
}

public record AlpsGeneticAlgorithm<TCandidate, TSearchSpace, TProblem>
  : IterativeAlgorithm<TCandidate, TSearchSpace, TProblem, AlpsState<TCandidate>, AlpsGeneticAlgorithm<TCandidate, TSearchSpace, TProblem>.ExecutionState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public new sealed class ExecutionState
      : IterativeAlgorithm<TCandidate, TSearchSpace, TProblem, AlpsState<TCandidate>, ExecutionState>.ExecutionState
    {
        public required ICreatorInstance<TCandidate, TSearchSpace, TProblem> Creator { get; init; }
        public required ICrossoverInstance<TCandidate, TSearchSpace, TProblem> Crossover { get; init; }
        public required IMutatorInstance<TCandidate, TSearchSpace, TProblem> Mutator { get; init; }
        public required ISelectorInstance<TCandidate, TSearchSpace, TProblem> Selector { get; init; }
    }

    public required int PopulationSize { get; init; }
    public required ICreator<TCandidate, TSearchSpace, TProblem> Creator { get; init; }
    public required ICrossover<TCandidate, TSearchSpace, TProblem> Crossover { get; init; }
    public required IMutator<TCandidate, TSearchSpace, TProblem> Mutator { get; init; }
    public required ISelector<TCandidate, TSearchSpace, TProblem> Selector { get; init; }

    public int Elites { get; init; }
    public int? MaximumGenerations
    {
        get;
        init => field = value is null or > 0
          ? value
          : throw new ArgumentOutOfRangeException(nameof(MaximumGenerations), "MaximumGenerations must be positive when set.");
    }

    public double MutationRate
    {
        get;
        init => field = value is >= 0.0 and <= 1.0
          ? value
          : throw new ArgumentOutOfRangeException(nameof(MutationRate), "MutationRate must be in [0, 1].");
    } = 0.1;

    protected override ExecutionState CreateInitialExecutionState(IExecutionInstanceResolver resolver)
    {
        var effectiveMutator = MutationRate >= 1.0 ? Mutator : Mutator.WithRate(MutationRate);

        return new ExecutionState
        {
            Evaluator = resolver.Resolve(Evaluator),
            Interceptor = Interceptor is not null ? resolver.Resolve(Interceptor) : null,
            Creator = resolver.Resolve(Creator),
            Crossover = resolver.Resolve(Crossover),
            Mutator = resolver.Resolve(effectiveMutator),
            Selector = resolver.Resolve(Selector)
        };
    }

    protected override bool HasCompleted(
      int yieldedStateCount,
      AlpsState<TCandidate>? previousState,
      ExecutionState executionState,
      TProblem problem)
    {
        return MaximumGenerations is not null && yieldedStateCount >= MaximumGenerations.Value;
    }

    protected override AlpsState<TCandidate> ExecuteStep(
      AlpsState<TCandidate>? previousState,
      ExecutionState executionState,
      TProblem problem,
      IRandomNumberGenerator random)
    {
        var searchSpace = problem.SearchSpace;

        if (previousState is null)
        {
            var initialLayerPopulation = executionState.Creator.Create(PopulationSize, random, searchSpace, problem);
            var initialFitnesses = executionState.Evaluator.Evaluate(initialLayerPopulation, random, searchSpace, problem);

            return new AlpsState<TCandidate>
            {
                Population = [Population.From(initialLayerPopulation, initialFitnesses)],
                Ages = [Enumerable.Repeat(0, PopulationSize).ToArray()]
            };
        }

        var offspringCount = PopulationSize;
        var oldPopulation = previousState.Population[0].ToArray();
        var selectedParents = executionState.Selector.Select(oldPopulation, problem.Objective, offspringCount * 2, random, searchSpace, problem);

        var parentPairs = new IParents<TCandidate>[offspringCount];
        var offspringAges = new int[offspringCount];
        var nextAge = previousState.Ages[0].DefaultIfEmpty(0).Max() + 1;
        for (int i = 0, j = 0; i < offspringCount; i++, j += 2)
        {
            parentPairs[i] = new Parents<TCandidate>(selectedParents[j].Candidate, selectedParents[j + 1].Candidate);
            offspringAges[i] = nextAge;
        }

        var offspring = executionState.Crossover.Cross(parentPairs, random, searchSpace, problem);
        offspring = executionState.Mutator.Mutate(offspring, random, searchSpace, problem);

        var fitnesses = executionState.Evaluator.Evaluate(offspring, random, searchSpace, problem);
        var offspringPopulation = Population.From(offspring, fitnesses).EvaluatedCandidates;
        var newPopulation = ElitismReplacer<TCandidate>.Replace(oldPopulation, offspringPopulation, problem.Objective, offspringCount, Elites);

        return new AlpsState<TCandidate>
        {
            Population = [Population.From(newPopulation)],
            Ages = [offspringAges] // ToDo: track ages through replacement once ALPS layer behavior is fully implemented.
        };
    }
}
