using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

/// <summary>
/// Genetic algorithm variant that maintains diversity by adding randomly generated immigrants at a fixed generation interval.
/// </summary>
/// <remarks>
/// Reference: Grefenstette, J. J. (1992). Genetic algorithms for changing environments. In R. Männer &amp;
/// B. Manderick (Eds.), <i>Parallel problem solving from nature 2</i> (pp. 139-146). North-Holland.
/// </remarks>
public record RandomImmigrantGeneticAlgorithm<TCandidate, TSearchSpace, TProblem>
    : RandomImmigrantGeneticAlgorithmBase<RandomImmigrantGeneticAlgorithm<TCandidate, TSearchSpace, TProblem>,
        TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    private readonly int immigrantInterval = 1;

    /// <summary>
    /// Gets the generation interval at which immigrants are injected. The initial population is not an immigrant injection.
    /// </summary>
    public int ImmigrantInterval
    {
        get => immigrantInterval;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            immigrantInterval = value;
        }
    }

    protected override RandomImmigrantGeneticAlgorithmExecution<TCandidate, TSearchSpace, TProblem> CreateRandomImmigrantExecution(
        RandomImmigrantExecutionState state,
        IInterceptorExecution<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? interceptor,
        IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> evaluator,
        ICreatorExecution<TCandidate, TSearchSpace, TProblem> creator,
        ICreatorExecution<TCandidate, TSearchSpace, TProblem> immigrantCreator,
        ICrossoverExecution<TCandidate, TSearchSpace, TProblem> crossover,
        IMutatorExecution<TCandidate, TSearchSpace, TProblem> mutator,
        ISelectorExecution<TCandidate, TSearchSpace, TProblem> selector,
        ITerminatorExecution<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? terminator,
        int immigrantCount) =>
        new Execution(state, interceptor, evaluator, creator, immigrantCreator, crossover, mutator, selector, terminator,
            PopulationSize, MaximumGenerations, Elites, immigrantCount, ImmigrantInterval, MutationRate);

    private sealed class Execution(
        RandomImmigrantExecutionState state,
        IInterceptorExecution<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? interceptor,
        IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> evaluator,
        ICreatorExecution<TCandidate, TSearchSpace, TProblem> creator,
        ICreatorExecution<TCandidate, TSearchSpace, TProblem> immigrantCreator,
        ICrossoverExecution<TCandidate, TSearchSpace, TProblem> crossover,
        IMutatorExecution<TCandidate, TSearchSpace, TProblem> mutator,
        ISelectorExecution<TCandidate, TSearchSpace, TProblem> selector,
        ITerminatorExecution<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? terminator,
        int populationSize,
        int? maximumGenerations,
        int elites,
        int immigrantCount,
        int immigrantInterval,
        double mutationRate)
        : RandomImmigrantGeneticAlgorithmExecution<TCandidate, TSearchSpace, TProblem>(
            state, interceptor, evaluator, creator, immigrantCreator, crossover, mutator, selector, terminator,
            populationSize, maximumGenerations, elites, immigrantCount, mutationRate)
    {
        protected override bool ShouldInjectImmigrants(TProblem problem) =>
            GeneratedGenerationCount > 0 && GeneratedGenerationCount % immigrantInterval == 0;
    }
}

/// <summary>
/// Genetic algorithm variant that injects random immigrants in the first generated population after an observed dynamic problem epoch change.
/// </summary>
public record ChangeTriggeredRandomImmigrantGeneticAlgorithm<TCandidate, TSearchSpace, TProblem>
    : RandomImmigrantGeneticAlgorithmBase<ChangeTriggeredRandomImmigrantGeneticAlgorithm<TCandidate, TSearchSpace,
        TProblem>, TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : DynamicProblem<TProblem, TCandidate, TSearchSpace>
{
    protected override RandomImmigrantGeneticAlgorithmExecution<TCandidate, TSearchSpace, TProblem> CreateRandomImmigrantExecution(
        RandomImmigrantExecutionState state,
        IInterceptorExecution<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? interceptor,
        IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> evaluator,
        ICreatorExecution<TCandidate, TSearchSpace, TProblem> creator,
        ICreatorExecution<TCandidate, TSearchSpace, TProblem> immigrantCreator,
        ICrossoverExecution<TCandidate, TSearchSpace, TProblem> crossover,
        IMutatorExecution<TCandidate, TSearchSpace, TProblem> mutator,
        ISelectorExecution<TCandidate, TSearchSpace, TProblem> selector,
        ITerminatorExecution<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? terminator,
        int immigrantCount) =>
        new Execution(state, interceptor, evaluator, creator, immigrantCreator, crossover, mutator, selector, terminator,
            PopulationSize, MaximumGenerations, Elites, immigrantCount, MutationRate);

    private sealed class Execution(
        RandomImmigrantExecutionState state,
        IInterceptorExecution<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? interceptor,
        IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> evaluator,
        ICreatorExecution<TCandidate, TSearchSpace, TProblem> creator,
        ICreatorExecution<TCandidate, TSearchSpace, TProblem> immigrantCreator,
        ICrossoverExecution<TCandidate, TSearchSpace, TProblem> crossover,
        IMutatorExecution<TCandidate, TSearchSpace, TProblem> mutator,
        ISelectorExecution<TCandidate, TSearchSpace, TProblem> selector,
        ITerminatorExecution<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? terminator,
        int populationSize,
        int? maximumGenerations,
        int elites,
        int immigrantCount,
        double mutationRate)
        : RandomImmigrantGeneticAlgorithmExecution<TCandidate, TSearchSpace, TProblem>(
            state, interceptor, evaluator, creator, immigrantCreator, crossover, mutator, selector, terminator,
            populationSize, maximumGenerations, elites, immigrantCount, mutationRate)
    {

        protected override bool ShouldInjectImmigrants(TProblem problem)
        {
            var currentEpoch = problem.CurrentEpoch;
            if (state.LastObservedEpoch is null)
            {
                state.LastObservedEpoch = currentEpoch;
                return false;
            }

            if (currentEpoch == state.LastObservedEpoch)
                return false;

            state.LastObservedEpoch = currentEpoch;
            return true;
        }
    }
}

public record RandomImmigrantGeneticAlgorithm<TCandidate, TSearchSpace>
    : RandomImmigrantGeneticAlgorithm<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>;

public record RandomImmigrantGeneticAlgorithm<TCandidate>
    : RandomImmigrantGeneticAlgorithm<TCandidate, ISearchSpace<TCandidate>>;

public static class RandomImmigrantGeneticAlgorithm
{
    public static RandomImmigrantGeneticAlgorithm<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace,
        TProblem>(
        ICreator<TCandidate> creator,
        ICrossover<TCandidate> crossover,
        IMutator<TCandidate> mutator,
        double mutationRate,
        double immigrantRate,
        ISelector<TCandidate> selector,
        int populationSize,
        IEvaluator<TCandidate> evaluator,
        int elites = 1,
        int immigrantInterval = 1,
        ICreator<TCandidate>? immigrantCreator = null,
        IInterceptor<TCandidate>? interceptor = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        return new()
        {
            Creator = creator,
            ImmigrantCreator = immigrantCreator,
            Crossover = crossover,
            Mutator = mutator,
            MutationRate = mutationRate,
            ImmigrantRate = immigrantRate,
            ImmigrantInterval = immigrantInterval,
            Selector = selector,
            Elites = elites,
            PopulationSize = populationSize,
            Evaluator = evaluator,
            Interceptor = interceptor
        };
    }
}

public static class ChangeTriggeredRandomImmigrantGeneticAlgorithm
{
    public static ChangeTriggeredRandomImmigrantGeneticAlgorithm<TCandidate, TSearchSpace, TProblem> Create<TCandidate,
        TSearchSpace, TProblem>(
        ICreator<TCandidate> creator,
        ICrossover<TCandidate> crossover,
        IMutator<TCandidate> mutator,
        double mutationRate,
        double immigrantRate,
        ISelector<TCandidate> selector,
        int populationSize,
        IEvaluator<TCandidate> evaluator,
        int elites = 1,
        ICreator<TCandidate>? immigrantCreator = null,
        IInterceptor<TCandidate>? interceptor = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : DynamicProblem<TProblem, TCandidate, TSearchSpace>
    {
        return new()
        {
            Creator = creator,
            ImmigrantCreator = immigrantCreator,
            Crossover = crossover,
            Mutator = mutator,
            MutationRate = mutationRate,
            ImmigrantRate = immigrantRate,
            Selector = selector,
            Elites = elites,
            PopulationSize = populationSize,
            Evaluator = evaluator,
            Interceptor = interceptor
        };
    }
}

public abstract record RandomImmigrantGeneticAlgorithmBase<TSelf, TCandidate, TSearchSpace, TProblem>
    : IterativeAlgorithm<TSelf, TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>
    where TSelf : RandomImmigrantGeneticAlgorithmBase<TSelf, TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public required int PopulationSize { get; init; }
    public required ICreator<TCandidate> Creator { get; init; }

    /// <summary>
    /// Gets the creator used for random immigrants. When unset, <see cref="Creator"/> is reused.
    /// </summary>
    public ICreator<TCandidate>? ImmigrantCreator { get; init; }

    public required ICrossover<TCandidate> Crossover { get; init; }
    public required IMutator<TCandidate> Mutator { get; init; }
    public ITerminator<TCandidate>? Terminator { get; init; }
    public IEvaluator<TCandidate> Evaluator { get; init; } = new ProblemEvaluator<TCandidate>();

    /// <summary>
    /// Gets the generation limit, or <see langword="null"/> for no limit. The expected value is positive.
    /// </summary>
    /// <remarks>A nonpositive limit completes before the first generation is produced.</remarks>
    public int? MaximumGenerations { get; init; }

    public int Elites { get; init; } = 1;

    /// <summary>
    /// Gets the probability that an offspring is mutated. The expected value is in <c>[0, 1]</c>.
    /// </summary>
    /// <remarks>
    /// The rate is applied as a threshold against a random value in <c>[0, 1)</c>. A value at most zero, negative
    /// infinity and <c>NaN</c> never mutate; a value at least one and positive infinity always mutate.
    /// </remarks>
    public double MutationRate { get; init; } = 0.1;

    /// <summary>
    /// Gets the share of the configured population size created as random immigrants when immigrant injection is active.
    /// </summary>
    /// <remarks>
    /// The value is rounded to the nearest integer count. Nonpositive values, negative infinity and <c>NaN</c>
    /// disable immigrants; positive infinity creates a full population of immigrants.
    /// </remarks>
    public double ImmigrantRate { get; init; } = 0.1;

    public required ISelector<TCandidate> Selector { get; init; }

    protected override ExecutionFactory<IterativeAlgorithmExecution<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>> CreateIterationFactory()
    {
        var state = new RandomImmigrantExecutionState();
        return scope => CreateRandomImmigrantExecution(
            state,
            scope.ResolveOptional<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>(Interceptor),
            scope.Resolve<TCandidate, TSearchSpace, TProblem>(Evaluator),
            scope.Resolve<TCandidate, TSearchSpace, TProblem>(Creator),
            scope.Resolve<TCandidate, TSearchSpace, TProblem>(ImmigrantCreator ?? Creator),
            scope.Resolve<TCandidate, TSearchSpace, TProblem>(Crossover),
            scope.Resolve<TCandidate, TSearchSpace, TProblem>(Mutator),
            scope.Resolve<TCandidate, TSearchSpace, TProblem>(Selector),
            scope.ResolveOptional<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>(Terminator),
            CalculateImmigrantCount(PopulationSize, ImmigrantRate));
    }

    protected abstract RandomImmigrantGeneticAlgorithmExecution<TCandidate, TSearchSpace, TProblem> CreateRandomImmigrantExecution(
        RandomImmigrantExecutionState state,
        IInterceptorExecution<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? interceptor,
        IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> evaluator,
        ICreatorExecution<TCandidate, TSearchSpace, TProblem> creator,
        ICreatorExecution<TCandidate, TSearchSpace, TProblem> immigrantCreator,
        ICrossoverExecution<TCandidate, TSearchSpace, TProblem> crossover,
        IMutatorExecution<TCandidate, TSearchSpace, TProblem> mutator,
        ISelectorExecution<TCandidate, TSearchSpace, TProblem> selector,
        ITerminatorExecution<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? terminator,
        int immigrantCount);

    private static int CalculateImmigrantCount(int populationSize, double immigrantRate)
    {
        if (populationSize <= 0 || immigrantRate <= 0 || double.IsNaN(immigrantRate))
            return 0;

        if (double.IsPositiveInfinity(immigrantRate))
            return populationSize;

        return Math.Clamp((int)Math.Round(populationSize * immigrantRate), 0, populationSize);
    }
}

public sealed class RandomImmigrantExecutionState
{
    public int GeneratedGenerationCount { get; set; }
    public int? LastObservedEpoch { get; set; }
}

public abstract class RandomImmigrantGeneticAlgorithmExecution<TCandidate, TSearchSpace, TProblem>(
    RandomImmigrantExecutionState state,
    IInterceptorExecution<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? interceptor,
    IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> evaluator,
    ICreatorExecution<TCandidate, TSearchSpace, TProblem> creator,
    ICreatorExecution<TCandidate, TSearchSpace, TProblem> immigrantCreator,
    ICrossoverExecution<TCandidate, TSearchSpace, TProblem> crossover,
    IMutatorExecution<TCandidate, TSearchSpace, TProblem> mutator,
    ISelectorExecution<TCandidate, TSearchSpace, TProblem> selector,
    ITerminatorExecution<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? terminator,
    int populationSize,
    int? maximumGenerations,
    int elites,
    int immigrantCount,
    double mutationRate)
    : IterativeAlgorithmExecution<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>(interceptor)
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected int GeneratedGenerationCount => state.GeneratedGenerationCount;
    private readonly WeightedRouter<IMutatorExecution<TCandidate, TSearchSpace, TProblem>?> mutationRouter = WeightedRouter.AtRate(mutator, mutationRate);

    protected override bool HasCompleted(int yieldedStateCount, PopulationState<TCandidate>? previousState,
                                         TProblem problem) =>
        maximumGenerations is not null && yieldedStateCount >= maximumGenerations.Value;

    protected override bool IsTerminalState(PopulationState<TCandidate> state, int yieldedStateCount,
                                            PopulationState<TCandidate>? previousState, TProblem problem) =>
        terminator?.IsTerminalState(state, problem.SearchSpace, problem) == true;

    protected override PopulationState<TCandidate> ExecuteStep(PopulationState<TCandidate>? previousState,
                                                               TProblem problem, IRandomNumberGenerator random)
    {
        PopulationState<TCandidate> result;
        if (previousState is null)
        {
            var initialSolutions = creator.Create(populationSize, random, problem.SearchSpace, problem);
            var initialFitnesses = evaluator.Evaluate(initialSolutions, random, problem.SearchSpace, problem);
            result = Population.From(initialSolutions.ToEvaluated(initialFitnesses)).ToPopulationState();
        }
        else
        {
            result = CreateNextGeneration(previousState, problem, random);
        }

        state.GeneratedGenerationCount++;
        return result;
    }

    protected abstract bool ShouldInjectImmigrants(TProblem problem);

    private PopulationState<TCandidate> CreateNextGeneration(PopulationState<TCandidate> previousState,
                                                             TProblem problem, IRandomNumberGenerator random)
    {
        var activeImmigrantCount = ShouldInjectImmigrants(problem) ? immigrantCount : 0;
        var oldPopulation = previousState.Population.EvaluatedCandidates;
        var survivorImmigrantCount = CalculateSurvivorImmigrantCount(populationSize, elites, activeImmigrantCount);
        var offspringSurvivorCount = populationSize - Math.Clamp(elites, 0, populationSize) - survivorImmigrantCount;
        var offspringSize = offspringSurvivorCount * 2;
        var parents = selector
                      .Select(oldPopulation, problem.Objective, offspringSize, random, problem.SearchSpace, problem)
                      .Select(x => x.Candidate)
                      .ToList();
        var offspring = crossover.Cross(parents.ToParentPairs(), random, problem.SearchSpace, problem);
        offspring = mutationRouter.Route(offspring, random, (selected, batch) => selected is null ? batch : selected.Mutate(batch, random, problem.SearchSpace, problem));
        var offspringFitnesses = evaluator.Evaluate(offspring, random, problem.SearchSpace, problem);
        var offspringPopulation = Population.From(offspring.ToEvaluated(offspringFitnesses)).EvaluatedCandidates;

        if (activeImmigrantCount == 0)
        {
            var nextPopulation = ElitismReplacer.Replace(
                oldPopulation,
                offspringPopulation,
                problem.Objective,
                populationSize,
                elites);
            return Population.From(nextPopulation).ToPopulationState();
        }

        var immigrants = immigrantCreator.Create(activeImmigrantCount, random, problem.SearchSpace, problem);
        var immigrantFitnesses = evaluator.Evaluate(immigrants, random, problem.SearchSpace, problem);
        var immigrantPopulation = Population.From(immigrants.ToEvaluated(immigrantFitnesses)).EvaluatedCandidates;
        var candidatePopulation = offspringPopulation.Take(offspringSurvivorCount)
                                                     .Concat(immigrantPopulation.Take(survivorImmigrantCount))
                                                     .ToArray();
        var newPopulation = ElitismReplacer.Replace(
            oldPopulation,
            candidatePopulation,
            problem.Objective,
            populationSize,
            elites);
        return Population.From(newPopulation).ToPopulationState();
    }

    private static int CalculateSurvivorImmigrantCount(int populationSize, int elites, int immigrantCount)
    {
        var nonEliteCount = populationSize - Math.Clamp(elites, 0, populationSize);
        return Math.Min(immigrantCount, nonEliteCount);
    }
}
