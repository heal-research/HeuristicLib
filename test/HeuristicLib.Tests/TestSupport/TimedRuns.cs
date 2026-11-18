using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Problems.TestFunctions;

namespace HEAL.HeuristicLib.Tests.TestSupport;

internal static class TimedRuns
{
    public static ExecutionStream<TSearchState> StreamWithTime<TSearchState>(
        this IAlgorithm<RealVector, TSearchState> algorithm, TestFunctionProblem problem,
        IRandomNumberGenerator random, TimeProvider timeProvider, CancellationToken ct = default)
        where TSearchState : class, ISearchState =>
        new AlgorithmRun<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, TSearchState>(
            algorithm, problem, random, timeProvider).Stream(cancellationToken: ct);
}
