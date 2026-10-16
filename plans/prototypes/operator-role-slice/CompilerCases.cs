using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Encodings.RealVectors;
using RoleSlice;

public sealed record DifferentState : SearchState;
public sealed class NoMetricRole : ISynchronousRole<NoMetricRole>
{
    public static RoleBinding<TCandidate, NoMetricRole, TSearchSpace, TProblem> CreateBinding<TCandidate, TSearchSpace, TProblem>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> => throw new NotSupportedException();
}
public static class CompilerCases
{
    public static void Main()
    {
        var mutation = new ScaleMutator(.5).Retry(2).Named("mutation");
        var algorithm = RoleSlice.GeneticAlgorithm.Create(new UniformCreator(), new FirstParentCrossover<RealVector>(), mutation).Named("algorithm");
#if WRONG_CANDIDATE
        IOperator<RealVector, MutationRole> invalid = new IdentityMutator<Permutation>().Named("wrong representation");
#endif
#if WRONG_ROLE
        IOperator<RealVector, MutationRole> invalid = new RoleSlice.ProblemEvaluator<RealVector>().Named("wrong role");
#endif
#if STREAMING_RETRY
        var invalid = algorithm.Retry(2);
#endif
#if WRONG_STATE
        IOperator<RealVector, AlgorithmRole<DifferentState>> invalid = algorithm;
#endif
#if WRONG_FLUENT_ROLE
        var invalid = RoleSlice.GeneticAlgorithm.Create(new UniformCreator(), new FirstParentCrossover<RealVector>(), mutation)
            .WithMutator(new RoleSlice.ProblemEvaluator<RealVector>());
#endif
#if UNSUPPORTED_CONCURRENCY
        var invalid = new IdentityMutator<RealVector>().WithConcurrency(ExecutionConcurrency.Concurrent(2));
#endif
#if WRONG_OBSERVER
        var invalid = Aspect.Create(Pointcut.Reference<IOperator<RealVector, MutationRole>>(mutation),
            new EvaluationObservationAdvice<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>>(_ => { }));
#endif
#if UNSUPPORTED_METRIC
        IOperator<RealVector, NoMetricRole> noMetric = null!;
        var invalid = noMetric.CountCandidates(new CountAccumulator());
#endif
        Console.WriteLine("PASS compiler positive: candidate and role inferred through Retry, Named and algorithm construction");
    }
}
