using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

public record OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TCandidate>
    : IterativeAlgorithm<OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TCandidate>, TCandidate, PopulationState<TCandidate>>
{
    private double Strictness { get; } = 1.0;

    public required int PopulationSize { get; init; }
    public required IOperator<Creation, TCandidate> Creator { get; init; }
    public required IOperator<Crossover, TCandidate> Crossover { get; init; }
    public required IOperator<Mutation, TCandidate> Mutator { get; init; }
    public required IOperator<Selection, TCandidate> Selector { get; init; }
    public IOperator<Evaluation, TCandidate> Evaluator { get; init; } = new ProblemEvaluator<TCandidate>();
    public int Elites { get; init; } = 1;
    public required int MaxEffort { get; init; }
    public IOperator<Refinement, TCandidate>? Refiner { get; init; }
    /// <summary>
    /// Gets the generation limit, or <see langword="null"/> for no limit. The expected value is positive.
    /// </summary>
    /// <remarks>A nonpositive limit completes before the first generation is produced.</remarks>
    public int? MaximumGenerations { get; init; }

    public static Parameter<OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TCandidate>, int> PopulationSizeParameter { get; } =
        new(nameof(PopulationSize), static algorithm => algorithm.PopulationSize, static (algorithm, value) => algorithm with { PopulationSize = value });

    public static Parameter<OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TCandidate>, IOperator<Creation, TCandidate>> CreatorParameter { get; } =
        new(nameof(Creator), static algorithm => algorithm.Creator, static (algorithm, value) => algorithm with { Creator = value });

    public static Parameter<OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TCandidate>, IOperator<Crossover, TCandidate>> CrossoverParameter { get; } =
        new(nameof(Crossover), static algorithm => algorithm.Crossover, static (algorithm, value) => algorithm with { Crossover = value });

    public static Parameter<OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TCandidate>, IOperator<Mutation, TCandidate>> MutatorParameter { get; } =
        new(nameof(Mutator), static algorithm => algorithm.Mutator, static (algorithm, value) => algorithm with { Mutator = value });

    public static Parameter<OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TCandidate>, IOperator<Selection, TCandidate>> SelectorParameter { get; } =
        new(nameof(Selector), static algorithm => algorithm.Selector, static (algorithm, value) => algorithm with { Selector = value });

    public static Parameter<OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TCandidate>, IOperator<Evaluation, TCandidate>> EvaluatorParameter { get; } =
        new(nameof(Evaluator), static algorithm => algorithm.Evaluator, static (algorithm, value) => algorithm with { Evaluator = value });

    public static Parameter<OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TCandidate>, int> ElitesParameter { get; } =
        new(nameof(Elites), static algorithm => algorithm.Elites, static (algorithm, value) => algorithm with { Elites = value });

    public static Parameter<OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TCandidate>, int> MaxEffortParameter { get; } =
        new(nameof(MaxEffort), static algorithm => algorithm.MaxEffort, static (algorithm, value) => algorithm with { MaxEffort = value });

    public static Parameter<OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TCandidate>, IOperator<Refinement, TCandidate>?> RefinerParameter { get; } =
        new(nameof(Refiner), static algorithm => algorithm.Refiner, static (algorithm, value) => algorithm with { Refiner = value });

    public static Parameter<OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TCandidate>, int?> MaximumGenerationsParameter { get; } =
        new(nameof(MaximumGenerations), static algorithm => algorithm.MaximumGenerations, static (algorithm, value) => algorithm with { MaximumGenerations = value });

    public new static IReadOnlyList<IParameter> Parameters { get; } = [PopulationSizeParameter, CreatorParameter, CrossoverParameter, MutatorParameter, SelectorParameter, EvaluatorParameter, ElitesParameter, MaxEffortParameter, RefinerParameter, MaximumGenerationsParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override IterativeAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>> scope)
    {
        var interceptor = scope.ResolveOptional(Interceptor);
        var evaluator = scope.Resolve(Evaluator);
        var creator = scope.Resolve(Creator);
        var crossover = scope.Resolve(Crossover);
        var mutator = scope.Resolve(Mutator);
        var selector = scope.Resolve(Selector);
        var refiner = scope.ResolveOptional(Refiner);
        return new Executor<TSearchSpace, TProblem>(interceptor, evaluator, creator, crossover, mutator, selector, refiner,
            PopulationSize, Elites, MaxEffort, MaximumGenerations, Strictness, scope.Problem);
    }

    private sealed class Executor<TSearchSpace, TProblem>(
        IInterceptionExecutor<PopulationState<TCandidate>>? interceptor,
        IEvaluationExecutor<TCandidate> evaluator,
        ICreationExecutor<TCandidate> creator,
        ICrossoverExecutor<TCandidate> crossover,
        IMutationExecutor<TCandidate> mutator,
        ISelectionExecutor<TCandidate> selector,
        IRefinementExecutor<TCandidate>? refiner,
        int populationSize,
        int elites,
        int maxEffort,
        int? maximumGenerations,
        double strictness, TProblem problem)
        : IterativeAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>(interceptor, problem)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        protected override bool HasCompleted(int yieldedStateCount, PopulationState<TCandidate>? previousState) =>
            maximumGenerations is not null && yieldedStateCount >= maximumGenerations.Value;

        protected override PopulationState<TCandidate> ExecuteStep(PopulationState<TCandidate>? previousState, IRandomNumberGenerator random)
        {
            if (previousState is null)
            {
                var initialSolutions = creator.Create(populationSize, random);
                if (refiner is not null)
                {
                    initialSolutions = refiner.Refine(initialSolutions, random);
                }

                var initialPopulation = initialSolutions.ToEvaluated(evaluator.Evaluate(initialSolutions, random));
                return Population.From(initialPopulation).ToPopulationState();
            }

            var oldPopulation = previousState.Population.EvaluatedCandidates;
            IReadOnlyList<EvaluatedCandidate<TCandidate>> newPop;

            if (oldPopulation.Count <= 0)
            {
                var initialCandidates = creator.Create(populationSize, random);
                if (refiner is not null)
                {
                    initialCandidates = refiner.Refine(initialCandidates, random);
                }

                newPop = initialCandidates.ToEvaluated(evaluator.Evaluate(initialCandidates, random));
            }
            else
            {
                var selected = selector.Select(oldPopulation, Problem.Objective, maxEffort * 2, random);
                var population = crossover.Cross(selected.ToParents(Problem.Objective), random);
                population = mutator.Mutate(population, random);
                if (refiner is not null)
                {
                    population = refiner.Refine(population, random);
                }

                newPop = population.ToEvaluated(evaluator.Evaluate(population, random)).Zip(selected.ToEvaluatedCandidatesPairs())
                    .Where(pair => pair.Item1.ObjectiveVector.Dominates(Combine(pair.Item2, Problem.Objective, strictness), Problem.Objective))
                    .Select(pair => pair.Item1).ToArray();
            }

            var targetPopulationSize = elites + newPop.Count;
            var newPopulation = ElitismReplacer.Replace(oldPopulation, newPop, Problem.Objective, targetPopulationSize, elites);

            return Population.From(newPopulation).ToPopulationState();
        }

        private static ObjectiveVector Combine(Parents<EvaluatedCandidate<TCandidate>> parents, ObjectiveDirections problemObjective, double strictness)
        {
            var o1 = parents.Parent1.ObjectiveVector;
            var o2 = parents.Parent2.ObjectiveVector;
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
