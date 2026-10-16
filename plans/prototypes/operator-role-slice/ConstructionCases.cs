using System.Reflection;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Problems.TestFunctions;
using RoleSlice;
using GaussianMutator = RoleSlice.GaussianMutator;
using GeneticAlgorithm = RoleSlice.GeneticAlgorithm;
using HillClimber = RoleSlice.HillClimber;

namespace ConstructionCases;

public static class Program
{
    private static int assertions;
    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition)
            throw new Exception(message);
    }

    public static async Task Main()
    {
        var concurrency = ExecutionConcurrency.Concurrent(2);
        var originalMutation = new GaussianMutator(1, 0.3);
        // These assignments require exact concrete return types in a separate assembly.
        GaussianMutator mutation = originalMutation.WithConcurrency(concurrency).WithMutationStrength(0.2).WithMutationRate(0.8);
        ScaleMutator scale = new ScaleMutator(0.5).WithConcurrency(concurrency);
        UniformCreator creator = new UniformCreator().WithConcurrency(concurrency);
        Check(originalMutation.MutationStrength == 0.3 && originalMutation.Concurrency.Kind == ExecutionConcurrencyKind.Sequential, "source mutated");
        Check(mutation == (originalMutation with { Concurrency = concurrency, MutationStrength = 0.2, MutationRate = 0.8 }), "leaf configuration differs");
        Check(!ReferenceEquals(originalMutation, mutation), "configuration reference reused");
        Check(scale.Factor == 0.5 && scale.Concurrency == concurrency && creator.Concurrency == concurrency, "shared setting lost concrete data");

        var crossover = new FirstParentCrossover<RealVector>();
        var evaluator = new RoleSlice.ProblemEvaluator<RealVector>().Named("fitness");
        var selector = new TournamentSelector<RealVector>(3);
        var requiredOnly = GeneticAlgorithm.Create(creator, crossover, mutation);
        var direct = new RoleSlice.GeneticAlgorithm<RealVector>
        {
            Creator = creator,
            Crossover = crossover,
            Mutator = mutation,
            Evaluator = evaluator,
            Selector = selector,
            PopulationSize = 6,
            MaximumGenerations = 3,
            Elites = 2,
            MutationRate = 0.6
        };
        var factory = GeneticAlgorithm.Create(creator, crossover, mutation, evaluator, selector,
            populationSize: 6, maximumGenerations: 3, elites: 2, mutationRate: 0.6);
        RoleSlice.GeneticAlgorithm<RealVector> fluent = requiredOnly
            .WithCreator(creator).WithCrossover(crossover).WithMutator(mutation).WithEvaluator(evaluator).WithSelector(selector)
            .WithPopulationSize(6).WithMaximumGenerations(3).WithElites(2).WithMutationRate(0.6);
        Check(direct == factory && factory == fluent, "construction paths disagree");
        Check(requiredOnly.PopulationSize == 8 && requiredOnly.MaximumGenerations == 5 && requiredOnly.Elites == 1 && requiredOnly.MutationRate == 1, "default configuration mutated");
        Check(ReferenceEquals(fluent.Creator, creator) && ReferenceEquals(fluent.Evaluator, evaluator), "child references cloned");
        Check(requiredOnly == new RoleSlice.GeneticAlgorithm<RealVector> { Creator = creator, Crossover = crossover, Mutator = mutation }, "factory defaults drifted");

        var originalHill = HillClimber.Create(creator, mutation);
        var hillFactory = HillClimber.Create(creator, mutation, evaluator, maximumIterations: 2);
        RoleSlice.HillClimber<RealVector> hill = originalHill.WithCreator(creator).WithMutator(mutation).WithEvaluator(evaluator).WithMaximumIterations(2);
        Check(hillFactory == hill && originalHill.MaximumIterations == 3, "hill configuration differs");
        var cycle = fluent.CycleWith(hill, maximumCycles: 2, scopePolicy: ChildScopePolicy.Retained);
        var editedCycle = fluent.CycleWith(hill).WithAlgorithms([fluent, hill]).WithMaximumCycles(2).WithScopePolicy(ChildScopePolicy.Retained);
        Check(cycle == editedCycle, "fluent cycle options differ");
        var mutableChildren = new List<IOperator<RealVector, AlgorithmRole<PopulationState<RealVector>>>> { fluent, hill };
        var snapshot = cycle.WithAlgorithms(mutableChildren);
        mutableChildren.Clear();
        Check(snapshot.Algorithms.Count == 2 && ReferenceEquals(snapshot.Algorithms[0], fluent), "child collection not snapshotted");

        var named = mutation.Named("old");
        var renamed = named.WithName("new");
        Check(named.Name == "old" && renamed.Name == "new" && ReferenceEquals(renamed.ChildOperator, mutation), "renaming added a wrapper or changed source");
        Check(ReferenceEquals(renamed.WithChildOperator(originalMutation).ChildOperator, originalMutation), "named replacement failed");
        var retry = mutation.Retry(2);
        Check(retry.WithAttempts(3).Attempts == 3 && retry.Attempts == 2, "retry source changed");
        Check(ReferenceEquals(retry.WithChildOperator(originalMutation).ChildOperator, originalMutation), "retry replacement failed");

        // Compare actual runs, with seeded RNG and no configuration syntax escape in the fluent path.
        var problem = new TestFunctionProblem(new SphereFunction(2));
        var expected = await Outcomes(direct.PrepareRun(problem, RandomNumberGenerator.Create(73)).Stream());
        var actual = await Outcomes(fluent.PrepareRun(problem, RandomNumberGenerator.Create(73)).Stream());
        Check(expected.SequenceEqual(actual), "fluent GA changes execution results");
        var cycleResults = await Outcomes(cycle.Named("hybrid").PrepareRun(problem, RandomNumberGenerator.Create(73)).Stream());
        Check(cycleResults.Count == 10, "fully configured meta algorithm did not execute both cycles");
        var invalid = requiredOnly.WithPopulationSize(0);
        Check(requiredOnly.PopulationSize == 8 && invalid.PopulationSize == 0, "setting prematurely changed or validated");
        try
        {
            await Outcomes(invalid.PrepareRun(problem, RandomNumberGenerator.Create(73)).Stream());
            throw new Exception("invalid configuration executed");
        }
        catch (InvalidOperationException exception)
        {
            Check(exception.Message.Contains("PopulationSize"), "wrong preparation error");
        }

        // A scoped surface check catches a new setting omitted from this proposed fluent API.
        foreach (var type in new[] { typeof(RoleSlice.GeneticAlgorithm<RealVector>), typeof(RoleSlice.HillClimber<RealVector>),
            typeof(Cycle<RealVector, PopulationState<RealVector>>), typeof(GaussianMutator), typeof(UniformCreator),
            typeof(Named<RealVector, MutationRole>), typeof(Retry<RealVector, MutationRole>) })
            CheckFluentCoverage(type);
        CheckFactoryCoverage(typeof(RoleSlice.GeneticAlgorithm<RealVector>), typeof(GeneticAlgorithm));
        CheckFactoryCoverage(typeof(RoleSlice.HillClimber<RealVector>), typeof(HillClimber));
        Console.WriteLine($"Fluent construction: {assertions} assertions passed; exact types, all settings in seven families, defaults, identity, snapshots, preparation and seeded execution.");
    }

    private static async Task<List<string>> Outcomes(IAsyncEnumerable<PopulationState<RealVector>> stream)
    {
        var values = new List<string>();
        await foreach (var state in stream)
            values.Add(string.Join(";", state.Population.EvaluatedCandidates.Select(candidate => string.Join(",", candidate.Candidate.Select(value => value.ToString("R", System.Globalization.CultureInfo.InvariantCulture))))));
        return values;
    }

    private static void CheckFluentCoverage(Type configuration)
    {
        var methods = typeof(ConfigurationExtensions).Assembly.GetExportedTypes().Where(type => type.IsAbstract && type.IsSealed)
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(method => method.IsDefined(typeof(ExtensionAttribute), false) && ReceiverMatches(method.GetParameters()[0].ParameterType, configuration));
        var names = methods.Select(method => method.Name).ToHashSet();
        foreach (var property in configuration.GetProperties().Where(property => property.SetMethod?.IsPublic == true))
            Check(names.Contains("With" + property.Name), $"No fluent path for {configuration.Name}.{property.Name}");
    }

    private static bool ReceiverMatches(Type receiver, Type configuration) => receiver.IsGenericParameter
        ? receiver.GetGenericParameterConstraints().All(constraint => constraint.IsAssignableFrom(configuration))
        : receiver.IsGenericType && configuration.IsGenericType
            ? receiver.GetGenericTypeDefinition() == configuration.GetGenericTypeDefinition()
            : receiver == configuration;

    private static void CheckFactoryCoverage(Type configuration, Type companion)
    {
        var parameters = companion.GetMethod("Create")!.GetParameters().Select(parameter => parameter.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var property in configuration.GetProperties().Where(property => property.SetMethod?.IsPublic == true))
            Check(parameters.Contains(property.Name), $"No factory path for {configuration.Name}.{property.Name}");
    }
}
