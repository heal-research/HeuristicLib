using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.Execution;

public sealed class CacheConcurrencyTests
{
    [Fact]
    public async Task ConcurrentCacheMissesEvaluateOutsideTheSharedStoreAndPublishSafely()
    {
        using var overlap = new Barrier(2);
        var source = new OverlappingEvaluator(overlap).Cached();
        var root = ResolutionScope.Create();
        root.Share(source);
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var scope = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(problem);
        var first = scope.Resolve(source);
        var second = scope.CreateChildScope().Resolve(source);
        var candidate = new RealVector(2.0);
        var results = await Task.WhenAll(
            Task.Run(() => first.Evaluate([candidate], RandomNumberGenerator.Create(1)), TestContext.Current.CancellationToken),
            Task.Run(() => second.Evaluate([candidate], RandomNumberGenerator.Create(2)), TestContext.Current.CancellationToken));
        results.Select(result => result[0][0]).ShouldBe([4.0, 4.0]);
        // No third participant reaches the barrier: this call must be a cache hit.
        first.Evaluate([candidate], RandomNumberGenerator.Create(3))[0][0].ShouldBe(4.0);
    }

    private sealed record OverlappingEvaluator(Barrier Gate) : Evaluator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        protected override IEvaluationExecutor<RealVector> CreateExecutor<TRunSearchState>(ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, TRunSearchState> scope) => new Executor(Gate, scope.Problem);

        private sealed class Executor(Barrier gate, TestFunctionProblem problem) : EvaluationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(problem)
        {
            public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random)
            {
                if (!gate.SignalAndWait(TimeSpan.FromSeconds(10)))
                    throw new InvalidOperationException("Concurrent evaluations did not overlap.");
                return candidates.Select(candidate => new ObjectiveVector(candidate[0] * candidate[0])).ToArray();
            }
        }
    }
}
