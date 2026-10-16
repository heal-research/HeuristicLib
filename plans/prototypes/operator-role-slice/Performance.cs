using System.Diagnostics;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using RoleSlice;

public sealed class DirectCounter(IMutatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> inner, CountAccumulator counter)
    : IMutatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
{
    public IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random, BoundedRealVectorSearchSpace space, TestFunctionProblem problem)
    {
        var result = inner.Mutate(parents, random, space, problem);
        counter.IncrementBy(1);
        return result;
    }
}
public static class Performance
{
    private static readonly TestFunctionProblem Problem = new(new SphereFunction(3));
    private static readonly IRandomNumberGenerator Random = RandomNumberGenerator.Create(42);
    private static int sink;
    private static long Loop(IMutatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> execution, IReadOnlyList<RealVector> parents, int count)
    {
        var result = 0;
        var start = Stopwatch.GetTimestamp();
        for (var i = 0; i < count; i++)
            result += execution.Mutate(parents, Random, Problem.SearchSpace, Problem).Count;
        var elapsed = Stopwatch.GetTimestamp() - start;
        sink = result;
        return elapsed;
    }
    private static void Measure(string name, IMutatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> execution, IReadOnlyList<RealVector> parents, int count)
    {
        for (var i = 0; i < 8; i++)
            Loop(execution, parents, Math.Max(1, count / 4));
        var samples = new double[7];
        var bytes = new long[7];
        for (var i = 0; i < samples.Length; i++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            samples[i] = Loop(execution, parents, count) * 1e9 / Stopwatch.Frequency / count;
            bytes[i] = GC.GetAllocatedBytesForCurrentThread() - before;
        }
        Array.Sort(samples);
        Console.WriteLine($"{name}: median={samples[3]:F2} ns/batch; range={samples[0]:F2}..{samples[6]:F2}; bytes/batch={bytes.Average() / count:F2}; checksum={sink}");
    }
    private static (IMutatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> Raw, IMutatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> Named,
        IMutatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> Direct, IMutatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> Hooks,
        IMutatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> Lifecycle, IMutatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> Policy) Paths(IOperator<RealVector, MutationRole> source)
    {
        var binding = new MutationBinding<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();
        var root = ResolutionScope.Create();
        var raw = binding.Resolve(root, source).Operation;
        var named = binding.Resolve(root, source.Named("forwarding")).Operation;
        var hooks = binding.Resolve(ResolutionScope.Create(source.CountCalls(new CountAccumulator())), source).Operation;
        var lifecycle = binding.Resolve(ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), new HookAdvice<RealVector, MutationRole, SuccessfulCallHooks, Unit>(new(new CountAccumulator())))), source).Operation;
        var policy = binding.Resolve(ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), new PolicyAdvice<RealVector, MutationRole>(new CountingPolicy(new CountAccumulator())))), source).Operation;
        return (raw, named, new DirectCounter(raw, new CountAccumulator()), hooks, lifecycle, policy);
    }
    public static void Main()
    {
        var cheap = Paths(new IdentityMutator<RealVector>());
        RealVector[] parents = [new(1.0, 2.0, 3.0)];
        var extractions = PreparedFactory.Extractions;
        Measure("cheap/raw", cheap.Raw, parents, 500_000);
        Measure("cheap/named", cheap.Named, parents, 500_000);
        Measure("cheap/direct-success-counter", cheap.Direct, parents, 500_000);
        Measure("cheap/shared-success-hooks", cheap.Hooks, parents, 500_000);
        Measure("cheap/shared-full-lifecycle-hooks", cheap.Lifecycle, parents, 500_000);
        Measure("cheap/shared-around-policy", cheap.Policy, parents, 500_000);
        if (PreparedFactory.Extractions != extractions)
            throw new Exception("Operation-time factory extraction.");
        var gaussian = Paths(new RoleSlice.GaussianMutator(1, .3));
        var batch = Enumerable.Repeat(parents[0], 32).ToArray();
        Measure("Gaussian32/raw", gaussian.Raw, batch, 200);
        Measure("Gaussian32/direct-success-counter", gaussian.Direct, batch, 200);
        Measure("Gaussian32/shared-success-hooks", gaussian.Hooks, batch, 200);
        Measure("Gaussian32/shared-around-policy", gaussian.Policy, batch, 200);
        Console.WriteLine("PASS factory erasure and controls envelope add no operation-time extraction; timings are local illustrative probes, not production guarantees");
    }
}
