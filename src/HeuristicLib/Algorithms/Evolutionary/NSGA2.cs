using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary;

#pragma warning disable S101
public record NSGA2<TCandidate, TSearchSpace, TProblem>
#pragma warning restore S101
  : IterativeAlgorithm<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>, NSGA2<TCandidate, TSearchSpace, TProblem>.ExecutionState>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public new sealed class ExecutionState
      : IterativeAlgorithm<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>, ExecutionState>.ExecutionState
    {
        public required ICreatorInstance<TCandidate, TSearchSpace, TProblem> Creator { get; init; }
        public required ICrossoverInstance<TCandidate, TSearchSpace, TProblem> Crossover { get; init; }
        public required IMutatorInstance<TCandidate, TSearchSpace, TProblem> Mutator { get; init; }
        public required ISelectorInstance<TCandidate, TSearchSpace, TProblem> Selector { get; init; }
        public required IReplacerInstance<TCandidate, TSearchSpace, TProblem> Replacer { get; init; }
    }

    public required int PopulationSize { get; init; }
    public required ICreator<TCandidate, TSearchSpace, TProblem> Creator { get; init; }
    public required ICrossover<TCandidate, TSearchSpace, TProblem> Crossover { get; init; }
    public required IMutator<TCandidate, TSearchSpace, TProblem> Mutator { get; init; }
    public required ISelector<TCandidate, TSearchSpace, TProblem> Selector { get; init; }
    public required IReplacer<TCandidate, TSearchSpace, TProblem> Replacer { get; init; }
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
            Selector = resolver.Resolve(Selector),
            Replacer = resolver.Resolve(Replacer)
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

        var offspringCount = PopulationSize;
        var parents = executionState.Selector.Select(previousState.Population.EvaluatedCandidates, problem.Objective, offspringCount * 2, random, problem.SearchSpace, problem).ToParents(problem.Objective);
        var children = executionState.Crossover.Cross(parents, random, problem.SearchSpace, problem);
        var mutants = executionState.Mutator.Mutate(children, random, problem.SearchSpace, problem);
        var newPopulation = Population.From(mutants, executionState.Evaluator.Evaluate(mutants, random, problem.SearchSpace, problem));
        var nextPopulation = executionState.Replacer.Replace(previousState.Population.EvaluatedCandidates, newPopulation.EvaluatedCandidates, problem.Objective, PopulationSize, random, problem.SearchSpace, problem);

        return new PopulationState<TCandidate>
        {
            Population = Population.From(nextPopulation)
        };
    }
}

#pragma warning disable S101
public static class NSGA2
#pragma warning restore S101
{
    public static NSGA2Builder<TCandidate, TSearchSpace, TProblem> GetBuilder<TCandidate, TSearchSpace, TProblem>(
      ICreator<TCandidate, TSearchSpace, TProblem> creator,
      ICrossover<TCandidate, TSearchSpace, TProblem> crossover,
      IMutator<TCandidate, TSearchSpace, TProblem> mutator, bool dominateOnEquals = true)
      where TSearchSpace : class, ISearchSpace<TCandidate> where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        return new NSGA2Builder<TCandidate, TSearchSpace, TProblem>
        {
            Mutator = mutator,
            Crossover = crossover,
            Creator = creator,
            Selector = new ParetoCrowdingTournamentSelector<TCandidate>(dominateOnEquals)
        };
    }
}
