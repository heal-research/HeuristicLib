using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.OperatorRolePrototype;
using HEAL.HeuristicLib.OperatorRolePrototype.Authoring;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using Perfolizer.Horology;

namespace OperatorRoleBenchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        var config = ManualConfig.Create(DefaultConfig.Instance)
            .AddJob(Job.Default.WithId("Checkpoint3").WithLaunchCount(3).WithWarmupCount(6)
                .WithIterationCount(12).WithIterationTime(TimeInterval.FromMilliseconds(100)))
            .AddDiagnoser(MemoryDiagnoser.Default)
            .AddExporter(JsonExporter.Full);
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
    }
}

public class InspectionBenchmarks
{
    private readonly TestFunctionProblem problem = new(new SphereFunction(1));
    private readonly IRandomNumberGenerator random = RandomNumberGenerator.Create(42);
    private readonly RealVector[] parents = [new(1.0)];
    private IMutatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> directCount = null!;
    private IMutatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> sharedCount = null!;
    private IMutatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> directDuration = null!;
    private IMutatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> sharedDuration = null!;
    private IMutatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> directStack = null!;
    private IMutatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> sharedStack = null!;

    [GlobalSetup]
    public void Setup()
    {
        IOperator<RealVector, Mutation> source = new IdentityMutation();
        var raw = ResolutionScope.Create().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>().Resolve(source);
        directCount = new DirectCounter(raw, new CountAccumulator());
        sharedCount = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), new CallCountingAdvice(new CountAccumulator())))
            .For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>().Resolve(source);
        directDuration = new DirectDurationWrapper(raw, new DurationAccumulator(), TimeProvider.System);
        sharedDuration = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), new DurationAdvice(new DurationAccumulator(), TimeProvider.System)))
            .For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>().Resolve(source);
        directStack = new DirectCounter(new DirectDurationWrapper(new DirectCounter(raw, new CountAccumulator()),
            new DurationAccumulator(), TimeProvider.System), new CountAccumulator());
        sharedStack = ResolutionScope.Create(
            Aspect.Create(Pointcut.Reference(source), new CallCountingAdvice(new CountAccumulator())),
            Aspect.Create(Pointcut.Reference(source), new DurationAdvice(new DurationAccumulator(), TimeProvider.System)),
            Aspect.Create(Pointcut.Reference(source), new CallCountingAdvice(new CountAccumulator())))
            .For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>().Resolve(source);
    }

    // BenchmarkDotNet consumes the returned reference; both paths also perform the same atomic counter update.
    [Benchmark]
    public IReadOnlyList<RealVector> DirectCount() => directCount.Mutate(parents, random, problem.SearchSpace, problem);

    [Benchmark]
    public IReadOnlyList<RealVector> SharedCount() => sharedCount.Mutate(parents, random, problem.SearchSpace, problem);

    [Benchmark]
    public IReadOnlyList<RealVector> DirectDuration() => directDuration.Mutate(parents, random, problem.SearchSpace, problem);

    [Benchmark]
    public IReadOnlyList<RealVector> SharedDuration() => sharedDuration.Mutate(parents, random, problem.SearchSpace, problem);

    [Benchmark]
    public IReadOnlyList<RealVector> DirectStack() => directStack.Mutate(parents, random, problem.SearchSpace, problem);

    [Benchmark]
    public IReadOnlyList<RealVector> SharedStack() => sharedStack.Mutate(parents, random, problem.SearchSpace, problem);

    private sealed record IdentityMutation : StatelessMutator<RealVector>
    {
        public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random) => parents;
    }

    private sealed class DirectCounter(IMutatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> child,
        CountAccumulator counter) : IMutatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random,
            BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem)
        {
            var result = child.Mutate(parents, random, searchSpace, problem);
            counter.IncrementBy(1);
            return result;
        }
    }

    private sealed class DirectDurationWrapper(IMutatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> child,
        DurationAccumulator duration, TimeProvider time) : IMutatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random,
            BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem)
        {
            var start = time.GetTimestamp();
            try { return child.Mutate(parents, random, searchSpace, problem); }
            finally { duration.AddDuration(time.GetElapsedTime(start)); }
        }
    }
}
