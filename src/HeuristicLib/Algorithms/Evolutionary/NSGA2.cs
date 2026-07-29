using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
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
    : IterativeAlgorithm<NSGA2<TCandidate, TSearchSpace, TProblem>, TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public required int PopulationSize { get; init; }
    public required ICreator<TCandidate, TSearchSpace, TProblem> Creator { get; init; }
    public required ICrossover<TCandidate, TSearchSpace, TProblem> Crossover { get; init; }
    public required IMutator<TCandidate, TSearchSpace, TProblem> Mutator { get; init; }
    public required ISelector<TCandidate, TSearchSpace, TProblem> Selector { get; init; }
    public required IReplacer<TCandidate, TSearchSpace, TProblem> Replacer { get; init; }
    public IEvaluator<TCandidate, TSearchSpace, TProblem> Evaluator { get; init; } = new DirectEvaluator<TCandidate>();
    public int? MaximumGenerations
    {
        get;
        init => field = value is null or > 0
          ? value
          : throw new ArgumentOutOfRangeException(nameof(MaximumGenerations), "MaximumGenerations must be positive when set.");
    }

    protected override IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>> CreateIterativeAlgorithmInstance(ExecutionInstanceRegistry registry, IInterceptorInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? resolvedInterceptor) =>
        new Instance(resolvedInterceptor, registry.Resolve(Evaluator), registry.Resolve(Creator), registry.Resolve(Crossover), registry.Resolve(Mutator), registry.Resolve(Selector), registry.Resolve(Replacer), PopulationSize, MaximumGenerations);

    private sealed class Instance(
        IInterceptorInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? interceptor,
        IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> evaluator,
        ICreatorInstance<TCandidate, TSearchSpace, TProblem> creator,
        ICrossoverInstance<TCandidate, TSearchSpace, TProblem> crossover,
        IMutatorInstance<TCandidate, TSearchSpace, TProblem> mutator,
        ISelectorInstance<TCandidate, TSearchSpace, TProblem> selector,
        IReplacerInstance<TCandidate, TSearchSpace, TProblem> replacer,
        int populationSize,
        int? maximumGenerations)
        : IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>(interceptor)
    {
        protected override bool HasCompleted(int yieldedStateCount, PopulationState<TCandidate>? previousState, TProblem problem) =>
            maximumGenerations is not null && yieldedStateCount >= maximumGenerations.Value;

        protected override PopulationState<TCandidate> ExecuteStep(PopulationState<TCandidate>? previousState, TProblem problem, IRandomNumberGenerator random)
        {
            if (previousState is null)
            {
                var initialSolutions = creator.Create(populationSize, random, problem.SearchSpace, problem);
                var initialFitnesses = evaluator.Evaluate(initialSolutions, random, problem.SearchSpace, problem);
                return Population.From(initialSolutions, initialFitnesses).ToPopulationState();
            }

            var parents = selector.Select(previousState.Population.EvaluatedCandidates, problem.Objective, populationSize * 2, random, problem.SearchSpace, problem).ToParents(problem.Objective);
            var children = crossover.Cross(parents, random, problem.SearchSpace, problem);
            var mutants = mutator.Mutate(children, random, problem.SearchSpace, problem);
            var newPopulation = Population.From(mutants, evaluator.Evaluate(mutants, random, problem.SearchSpace, problem));
            var nextPopulation = replacer.Replace(previousState.Population.EvaluatedCandidates, newPopulation.EvaluatedCandidates, problem.Objective, populationSize, random, problem.SearchSpace, problem);

            return Population.From(nextPopulation).ToPopulationState();
        }
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
        return new()
        {
            Mutator = mutator,
            Crossover = crossover,
            Creator = creator,
            Selector = new ParetoCrowdingTournamentSelector<TCandidate>(dominateOnEquals)
        };
    }
}
