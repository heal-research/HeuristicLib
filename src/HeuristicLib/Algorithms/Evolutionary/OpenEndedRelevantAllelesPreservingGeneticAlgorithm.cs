using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary;

public record OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TCandidate, TSearchSpace, TProblem>
  : IterativeAlgorithm<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>, OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TCandidate, TSearchSpace, TProblem>.ExecutionState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public new sealed class ExecutionState
      : IterativeAlgorithm<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>, ExecutionState>.ExecutionState
    {
        public required ICreatorInstance<TCandidate, TSearchSpace, TProblem> Creator { get; init; }
        public required ICrossoverInstance<TCandidate, TSearchSpace, TProblem> Crossover { get; init; }
        public required IMutatorInstance<TCandidate, TSearchSpace, TProblem> Mutator { get; init; }
        public required ISelectorInstance<TCandidate, TSearchSpace, TProblem> Selector { get; init; }
    }

    private double Strictness { get; } = 1.0;

    public required int PopulationSize { get; init; }
    public required ICreator<TCandidate, TSearchSpace, TProblem> Creator { get; init; }
    public required ICrossover<TCandidate, TSearchSpace, TProblem> Crossover { get; init; }
    public required IMutator<TCandidate, TSearchSpace, TProblem> Mutator { get; init; }
    public required ISelector<TCandidate, TSearchSpace, TProblem> Selector { get; init; }
    public int Elites { get; init; } = 1;
    public required int MaxEffort { get; init; }
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
            Crossover = resolver.Resolve(Crossover),
            Mutator = resolver.Resolve(Mutator),
            Selector = resolver.Resolve(Selector)
        };
    }

    protected override bool HasCompleted(
      int yieldedStateCount,
      PopulationState<TCandidate>? previousState,
      ExecutionState executionState,
      TProblem problem)
    {
        return MaximumGenerations is not null && yieldedStateCount >= MaximumGenerations.Value;
    }

    protected override PopulationState<TCandidate> ExecuteStep(
      PopulationState<TCandidate>? previousState,
      ExecutionState executionState,
      TProblem problem,
      IRandomNumberGenerator random)
    {
        if (previousState is null)
        {
            var initialSolutions = executionState.Creator.Create(PopulationSize, random, problem.SearchSpace, problem);
            var initialFitnesses = executionState.Evaluator.Evaluate(initialSolutions, random, problem.SearchSpace, problem);
            return new PopulationState<TCandidate>
            {
                Population = Population.From(initialSolutions, initialFitnesses)
            };
        }

        var oldPopulation = previousState.Population.EvaluatedCandidates;

        IReadOnlyList<EvaluatedCandidate<TCandidate>> newPop;
        if (oldPopulation.Length <= 0)
        {
            var initialSolutions = executionState.Creator.Create(PopulationSize, random, problem.SearchSpace, problem);
            var initialFitnesses = executionState.Evaluator.Evaluate(initialSolutions, random, problem.SearchSpace, problem);
            newPop = Population.From(initialSolutions, initialFitnesses).EvaluatedCandidates;
        }
        else
        {
            var selected = executionState.Selector.Select(oldPopulation, problem.Objective, MaxEffort * 2, random, problem.SearchSpace, problem);
            var population = executionState.Crossover.Cross(selected.ToParents(problem.Objective), random, problem.SearchSpace, problem);
            population = executionState.Mutator.Mutate(population, random, problem.SearchSpace, problem);
            var fitnesses = executionState.Evaluator.Evaluate(population, random, problem.SearchSpace, problem);

            newPop = Population
                     .From(population, fitnesses)
                     .EvaluatedCandidates
                     .Zip(selected.ToSolutionPairs())
                     .Where(f => f.Item1.ObjectiveVector.Dominates(Combine(f.Item2, problem.Objective, Strictness), problem.Objective))
                     .Select(f => f.Item1)
                     .ToArray();
        }

        var targetPopsize = Elites + newPop.Count;
        var newPopulation = ElitismReplacer<TCandidate>.Replace(oldPopulation, newPop, problem.Objective, targetPopsize, Elites);

        return new PopulationState<TCandidate>
        {
            Population = Population.From(newPopulation)
        };
    }

    private static ObjectiveVector Combine((EvaluatedCandidate<TCandidate>, EvaluatedCandidate<TCandidate>) parents, ObjectiveDirections problemObjective, double strictness = 1.0)
    {
        var o1 = parents.Item1.ObjectiveVector;
        var o2 = parents.Item2.ObjectiveVector;
        if (o2.Dominates(o1, problemObjective))
        {
            (o1, o2) = (o2, o1);
        }

        return strictness switch
        {
            >= 1.0 => o1,
            <= 0.0 => o2,
            _ => new ObjectiveVector(o1.Zip(o2).Select(pair => pair.Item1 * strictness + pair.Item2 * (1.0 - strictness)).ToArray())
        };
    }
}

// ReSharper disable once IdentifierTypo
public record OerapgaBuildBuilder<TCandidate, TSearchSpace, TProblem>
  : AlgorithmBuilder<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>, OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TCandidate, TSearchSpace, TProblem>>,
    IBuilderWithCreator<TCandidate, TSearchSpace, TProblem>,
    IBuilderWithSelector<TCandidate, TSearchSpace, TProblem>,
    IBuilderWithCrossover<TCandidate, TSearchSpace, TProblem>,
    IBuilderWithMutator<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public double MutationRate { get; set; } = 0.05;
    public int Elites { get; set; } = 1;
    public required int MaxEffort { get; set; }
    public required ICreator<TCandidate, TSearchSpace, TProblem> Creator { get; set; }
    public required ICrossover<TCandidate, TSearchSpace, TProblem> Crossover { get; set; }
    public required IMutator<TCandidate, TSearchSpace, TProblem> Mutator { get; set; }
    public int PopulationSize { get; set; } = 100;
    public ISelector<TCandidate, TSearchSpace, TProblem> Selector { get; set; } = new TournamentSelector<TCandidate>(2);

    public override OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TCandidate, TSearchSpace, TProblem> Build()
    {
        return new OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TCandidate, TSearchSpace, TProblem>()
        {
            PopulationSize = PopulationSize,
            Creator = Creator,
            Crossover = Crossover,
            Selector = Selector,
            Evaluator = Evaluator,
            Interceptor = Interceptor,
            Mutator = Mutator.WithRate(MutationRate),
            MaxEffort = MaxEffort
        };
    }
}
