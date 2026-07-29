using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
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
    : IterativeAlgorithm<OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TCandidate, TSearchSpace, TProblem>, TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    private double Strictness { get; } = 1.0;

    public required int PopulationSize { get; init; }
    public required ICreator<TCandidate, TSearchSpace, TProblem> Creator { get; init; }
    public required ICrossover<TCandidate, TSearchSpace, TProblem> Crossover { get; init; }
    public required IMutator<TCandidate, TSearchSpace, TProblem> Mutator { get; init; }
    public required ISelector<TCandidate, TSearchSpace, TProblem> Selector { get; init; }
    public IEvaluator<TCandidate, TSearchSpace, TProblem> Evaluator { get; init; } = new DirectEvaluator<TCandidate>();
    public int Elites { get; init; } = 1;
    public required int MaxEffort { get; init; }
    public int? MaximumGenerations
    {
        get;
        init => field = value is null or > 0
          ? value
          : throw new ArgumentOutOfRangeException(nameof(MaximumGenerations), "MaximumGenerations must be positive when set.");
    }

    protected override IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>> CreateIterativeAlgorithmInstance(ExecutionInstanceRegistry registry, IInterceptorInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? resolvedInterceptor) =>
        new Instance(resolvedInterceptor, registry.Resolve(Evaluator), registry.Resolve(Creator), registry.Resolve(Crossover), registry.Resolve(Mutator), registry.Resolve(Selector), PopulationSize, Elites, MaxEffort, MaximumGenerations, Strictness);

    private sealed class Instance(
        IInterceptorInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? interceptor,
        IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> evaluator,
        ICreatorInstance<TCandidate, TSearchSpace, TProblem> creator,
        ICrossoverInstance<TCandidate, TSearchSpace, TProblem> crossover,
        IMutatorInstance<TCandidate, TSearchSpace, TProblem> mutator,
        ISelectorInstance<TCandidate, TSearchSpace, TProblem> selector,
        int populationSize,
        int elites,
        int maxEffort,
        int? maximumGenerations,
        double strictness)
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

            var oldPopulation = previousState.Population.EvaluatedCandidates;
            IReadOnlyList<EvaluatedCandidate<TCandidate>> newPop;

            if (oldPopulation.Length <= 0)
            {
                var initialSolutions = creator.Create(populationSize, random, problem.SearchSpace, problem);
                var initialFitnesses = evaluator.Evaluate(initialSolutions, random, problem.SearchSpace, problem);
                newPop = Population.From(initialSolutions, initialFitnesses).EvaluatedCandidates;
            }
            else
            {
                var selected = selector.Select(oldPopulation, problem.Objective, maxEffort * 2, random, problem.SearchSpace, problem);
                var population = crossover.Cross(selected.ToParents(problem.Objective), random, problem.SearchSpace, problem);
                population = mutator.Mutate(population, random, problem.SearchSpace, problem);
                var fitnesses = evaluator.Evaluate(population, random, problem.SearchSpace, problem);
                newPop = Population.From(population, fitnesses).EvaluatedCandidates.Zip(selected.ToSolutionPairs())
                    .Where(pair => pair.Item1.ObjectiveVector.Dominates(Combine(pair.Item2, problem.Objective, strictness), problem.Objective))
                    .Select(pair => pair.Item1).ToArray();
            }

            var targetPopulationSize = elites + newPop.Count;
            var newPopulation = ElitismReplacer.Replace(oldPopulation, newPop, problem.Objective, targetPopulationSize, elites);

            return Population.From(newPopulation).ToPopulationState();
        }

        private static ObjectiveVector Combine((EvaluatedCandidate<TCandidate>, EvaluatedCandidate<TCandidate>) parents, ObjectiveDirections problemObjective, double strictness)
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
        return new()
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
