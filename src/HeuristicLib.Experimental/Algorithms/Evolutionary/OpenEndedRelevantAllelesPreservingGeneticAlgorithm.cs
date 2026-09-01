using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

public record OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TCandidate, TSearchSpace, TProblem>
    : IterativeAlgorithm<OpenEndedRelevantAllelesPreservingGeneticAlgorithm<TCandidate, TSearchSpace, TProblem>, TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    private double Strictness { get; } = 1.0;

    public required int PopulationSize { get; init; }
    public required ICreator<TCandidate, TSearchSpace, TProblem> Creator { get; init; }
    public required ICrossover<TCandidate, TSearchSpace, TProblem> Crossover { get; init; }
    public required IMutator<TCandidate> Mutator { get; init; }
    public required ISelector<TCandidate, TSearchSpace, TProblem> Selector { get; init; }
    public IEvaluator<TCandidate, TSearchSpace, TProblem> Evaluator { get; init; } = new ProblemEvaluator<TCandidate, TSearchSpace, TProblem>();
    public int Elites { get; init; } = 1;
    public required int MaxEffort { get; init; }
    public IRefiner<TCandidate, TSearchSpace, TProblem>? Refiner { get; init; }

    /// <summary>
    /// Gets the generation limit, or <see langword="null"/> for no limit. The expected value is positive.
    /// </summary>
    /// <remarks>A nonpositive limit completes before the first generation is produced.</remarks>
    public int? MaximumGenerations { get; init; }

    protected override IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry, IInterceptorInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? resolvedInterceptor) =>
        new Instance(resolvedInterceptor, instanceRegistry.Resolve(Evaluator), instanceRegistry.Resolve(Creator), instanceRegistry.Resolve(Crossover), instanceRegistry.Resolve<TCandidate, TSearchSpace, TProblem>(Mutator), instanceRegistry.Resolve(Selector), instanceRegistry.ResolveOptional(Refiner), PopulationSize, Elites, MaxEffort, MaximumGenerations, Strictness);

    private sealed class Instance(
        IInterceptorInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? interceptor,
        IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> evaluator,
        ICreatorInstance<TCandidate, TSearchSpace, TProblem> creator,
        ICrossoverInstance<TCandidate, TSearchSpace, TProblem> crossover,
        IMutatorInstance<TCandidate, TSearchSpace, TProblem> mutator,
        ISelectorInstance<TCandidate, TSearchSpace, TProblem> selector,
        IRefinerInstance<TCandidate, TSearchSpace, TProblem>? refiner,
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
                if (refiner is not null)
                {
                    initialSolutions = refiner.Refine(initialSolutions, random, problem.SearchSpace, problem);
                }

                var initialPopulation = initialSolutions.ToEvaluated(evaluator.Evaluate(initialSolutions, random, problem.SearchSpace, problem));
                return Population.From(initialPopulation).ToPopulationState();
            }

            var oldPopulation = previousState.Population.EvaluatedCandidates;
            IReadOnlyList<EvaluatedCandidate<TCandidate>> newPop;

            if (oldPopulation.Count <= 0)
            {
                var initialCandidates = creator.Create(populationSize, random, problem.SearchSpace, problem);
                if (refiner is not null)
                {
                    initialCandidates = refiner.Refine(initialCandidates, random, problem.SearchSpace, problem);
                }

                newPop = initialCandidates.ToEvaluated(evaluator.Evaluate(initialCandidates, random, problem.SearchSpace, problem));
            }
            else
            {
                var selected = selector.Select(oldPopulation, problem.Objective, maxEffort * 2, random, problem.SearchSpace, problem);
                var population = crossover.Cross(selected.ToParents(problem.Objective), random, problem.SearchSpace, problem);
                population = mutator.Mutate(population, random, problem.SearchSpace, problem);
                if (refiner is not null)
                {
                    population = refiner.Refine(population, random, problem.SearchSpace, problem);
                }

                newPop = population.ToEvaluated(evaluator.Evaluate(population, random, problem.SearchSpace, problem)).Zip(selected.ToEvaluatedCandidatesPairs())
                    .Where(pair => pair.Item1.ObjectiveVector.Dominates(Combine(pair.Item2, problem.Objective, strictness), problem.Objective))
                    .Select(pair => pair.Item1).ToArray();
            }

            var targetPopulationSize = elites + newPop.Count;
            var newPopulation = ElitismReplacer.Replace(oldPopulation, newPop, problem.Objective, targetPopulationSize, elites);

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
