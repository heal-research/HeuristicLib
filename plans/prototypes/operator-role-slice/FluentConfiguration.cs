namespace RoleSlice;

public abstract record OperatorConfiguration;

public interface IExecutionConcurrencyConfiguration
{
    ExecutionConcurrency Concurrency { get; init; }
}

public static class ConfigurationExtensions
{
    public static TConfiguration WithConcurrency<TConfiguration>(this TConfiguration configuration, ExecutionConcurrency concurrency)
        where TConfiguration : OperatorConfiguration, IExecutionConcurrencyConfiguration => configuration with { Concurrency = concurrency };
}

internal static class ConstructionDefaults
{
    internal const int PopulationSize = 8;
    internal const int MaximumGenerations = 5;
    internal const int Elites = 1;
    internal const double MutationRate = 1;
    internal const int MaximumIterations = 3;
    internal const int MaximumCycles = 1;
}

public static class GaussianMutatorExtensions
{
    public static GaussianMutator WithMutationRate(this GaussianMutator configuration, double mutationRate) => configuration with { MutationRate = mutationRate };
    public static GaussianMutator WithMutationStrength(this GaussianMutator configuration, double mutationStrength) => configuration with { MutationStrength = mutationStrength };
}

public static class GeneticAlgorithmExtensions
{
    public static GeneticAlgorithm<TCandidate> WithCreator<TCandidate>(this GeneticAlgorithm<TCandidate> configuration, IOperator<TCandidate, CreationRole> creator) => configuration with { Creator = creator };
    public static GeneticAlgorithm<TCandidate> WithCrossover<TCandidate>(this GeneticAlgorithm<TCandidate> configuration, IOperator<TCandidate, CrossoverRole> crossover) => configuration with { Crossover = crossover };
    public static GeneticAlgorithm<TCandidate> WithMutator<TCandidate>(this GeneticAlgorithm<TCandidate> configuration, IOperator<TCandidate, MutationRole> mutator) => configuration with { Mutator = mutator };
    public static GeneticAlgorithm<TCandidate> WithEvaluator<TCandidate>(this GeneticAlgorithm<TCandidate> configuration, IOperator<TCandidate, EvaluationRole> evaluator) => configuration with { Evaluator = evaluator };
    public static GeneticAlgorithm<TCandidate> WithSelector<TCandidate>(this GeneticAlgorithm<TCandidate> configuration, IOperator<TCandidate, SelectionRole> selector) => configuration with { Selector = selector };
    public static GeneticAlgorithm<TCandidate> WithPopulationSize<TCandidate>(this GeneticAlgorithm<TCandidate> configuration, int populationSize) => configuration with { PopulationSize = populationSize };
    public static GeneticAlgorithm<TCandidate> WithMaximumGenerations<TCandidate>(this GeneticAlgorithm<TCandidate> configuration, int maximumGenerations) => configuration with { MaximumGenerations = maximumGenerations };
    public static GeneticAlgorithm<TCandidate> WithElites<TCandidate>(this GeneticAlgorithm<TCandidate> configuration, int elites) => configuration with { Elites = elites };
    public static GeneticAlgorithm<TCandidate> WithMutationRate<TCandidate>(this GeneticAlgorithm<TCandidate> configuration, double mutationRate) => configuration with { MutationRate = mutationRate };
}

public static class HillClimberExtensions
{
    public static HillClimber<TCandidate> WithCreator<TCandidate>(this HillClimber<TCandidate> configuration, IOperator<TCandidate, CreationRole> creator) => configuration with { Creator = creator };
    public static HillClimber<TCandidate> WithMutator<TCandidate>(this HillClimber<TCandidate> configuration, IOperator<TCandidate, MutationRole> mutator) => configuration with { Mutator = mutator };
    public static HillClimber<TCandidate> WithEvaluator<TCandidate>(this HillClimber<TCandidate> configuration, IOperator<TCandidate, EvaluationRole> evaluator) => configuration with { Evaluator = evaluator };
    public static HillClimber<TCandidate> WithMaximumIterations<TCandidate>(this HillClimber<TCandidate> configuration, int maximumIterations) => configuration with { MaximumIterations = maximumIterations };
}

public static class CycleExtensions
{
    public static Cycle<TCandidate, TState> WithAlgorithms<TCandidate, TState>(this Cycle<TCandidate, TState> configuration, IReadOnlyList<IOperator<TCandidate, AlgorithmRole<TState>>> algorithms)
        where TState : class, ISearchState => configuration with { Algorithms = algorithms.ToValueArray() };
    public static Cycle<TCandidate, TState> WithMaximumCycles<TCandidate, TState>(this Cycle<TCandidate, TState> configuration, int maximumCycles)
        where TState : class, ISearchState => configuration with { MaximumCycles = maximumCycles };
    public static Cycle<TCandidate, TState> WithScopePolicy<TCandidate, TState>(this Cycle<TCandidate, TState> configuration, ChildScopePolicy scopePolicy)
        where TState : class, ISearchState => configuration with { ScopePolicy = scopePolicy };
}

public static class NamedExtensions
{
    public static Named<TCandidate, TRole> WithName<TCandidate, TRole>(this Named<TCandidate, TRole> configuration, string name)
        where TRole : IRole<TRole> => configuration with { Name = name };
    public static Named<TCandidate, TRole> WithChildOperator<TCandidate, TRole>(this Named<TCandidate, TRole> configuration, IOperator<TCandidate, TRole> childOperator)
        where TRole : IRole<TRole> => configuration with { ChildOperator = childOperator };
}

public static class RetryExtensions
{
    public static Retry<TCandidate, TRole> WithAttempts<TCandidate, TRole>(this Retry<TCandidate, TRole> configuration, int attempts)
        where TRole : ISynchronousRole<TRole> => configuration with { Attempts = attempts };
    public static Retry<TCandidate, TRole> WithChildOperator<TCandidate, TRole>(this Retry<TCandidate, TRole> configuration, IOperator<TCandidate, TRole> childOperator)
        where TRole : ISynchronousRole<TRole> => configuration with { ChildOperator = childOperator };
}
