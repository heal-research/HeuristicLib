using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.Docs.Architecture;

#region operation-executor
public interface IEstimationExecutor<TCandidate> : IExecutor
{
    IReadOnlyList<double> Estimate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random);
}
#endregion operation-executor

#region operation-type
public sealed class Estimation : IItemOperation<Estimation>
{
    static bool IOperation<Estimation>.IsExecutor<TCandidate, TSearchState>(IExecutor executor) =>
        executor is IEstimationExecutor<TCandidate>;

    static IExecutor IOperation<Estimation>.Wrap<TCandidate, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper) =>
        new WrappedExecutor<TCandidate, TWrapper>(executor, wrapper);

    static IExecutor IItemOperation<Estimation>.Route<TCandidate, TSearchState>(WeightedRouter<IExecutor> children) =>
        new RoutedExecutor<TCandidate>(children);

    private readonly struct Invocation<TCandidate>(IEstimationExecutor<TCandidate> executor,
        IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random) : IInvocation<IReadOnlyList<double>>
    {
        private readonly IEstimationExecutor<TCandidate> executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;

        public IReadOnlyList<double> Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((IEstimationExecutor<TCandidate>?)executor ?? this.executor).Estimate(candidates, random ?? this.random);
    }

    private sealed class WrappedExecutor<TCandidate, TWrapper>(IExecutor executor, TWrapper wrapper)
        : WrappingExecutor<IEstimationExecutor<TCandidate>, TWrapper>(executor, wrapper), IEstimationExecutor<TCandidate>
        where TWrapper : struct, IInvocationWrapper
    {
        public IReadOnlyList<double> Estimate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random) =>
            Wrapper.Invoke<Invocation<TCandidate>, IReadOnlyList<double>>(new(Executor, candidates, random));
    }

    private sealed class RoutedExecutor<TCandidate>(WeightedRouter<IExecutor> children)
        : RoutingExecutor<IEstimationExecutor<TCandidate>>(children), IEstimationExecutor<TCandidate>
    {
        public IReadOnlyList<double> Estimate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random) =>
            Children.Route(candidates, random, (child, batch) => child.Estimate(batch, random));
    }
}
#endregion operation-type

#region operation-resolution
public static class EstimationResolutionExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(ResolutionScope<TCandidate, TSearchSpace, TProblem> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IEstimationExecutor<TCandidate> Resolve(IOperator<Estimation, TCandidate> source) =>
            (IEstimationExecutor<TCandidate>)scope.ResolveOperator(source);

        public bool TryResolve(IOperator<Estimation, TCandidate> source,
            [NotNullWhen(true)] out IEstimationExecutor<TCandidate>? executor, [NotNullWhen(false)] out string? reason) =>
            scope.TryResolveOperator(source, out executor, out reason);
    }
}
#endregion operation-resolution

#region operation-observation
public sealed record EstimationObservation<TCandidate>(
    IOperator<Estimation, TCandidate> Estimator,
    IReadOnlyList<TCandidate> Candidates,
    IReadOnlyList<double> Estimates,
    ISearchSpace<TCandidate> SearchSpace,
    IProblem<TCandidate, ISearchSpace<TCandidate>> Problem)
    : IInvocationObservation<Estimation, TCandidate, EstimationObservation<TCandidate>>
{
    static IExecutor IInvocationObservation<Estimation, TCandidate, EstimationObservation<TCandidate>>.Wrap<TSearchSpace, TProblem, TSearchState>(
        IExecutor executor, IOperator<Estimation, TCandidate> source, Action<EstimationObservation<TCandidate>> record, TProblem problem) =>
        new Executor((IEstimationExecutor<TCandidate>)executor, source, record, problem);

    private sealed class Executor(IEstimationExecutor<TCandidate> executor,
        IOperator<Estimation, TCandidate> estimator, Action<EstimationObservation<TCandidate>> observe,
        IProblem<TCandidate, ISearchSpace<TCandidate>> problem)
        : IEstimationExecutor<TCandidate>
    {
        public IReadOnlyList<double> Estimate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random)
        {
            var estimates = executor.Estimate(candidates, random);
            observe(new EstimationObservation<TCandidate>(estimator, candidates, estimates, problem.SearchSpace, problem));
            return estimates;
        }
    }
}

public static class EstimationObservations
{
    extension<TCandidate>(IOperator<Estimation, TCandidate> estimator)
    {
        public InvocationSource<Estimation, TCandidate, EstimationObservation<TCandidate>> Invocations =>
            ObservationSource.Create<Estimation, TCandidate, EstimationObservation<TCandidate>>(estimator);

        public Observed<Estimation, TCandidate, EstimationObservation<TCandidate>> Observed(IObservationReceiver<EstimationObservation<TCandidate>> receiver) =>
            new(estimator, receiver);
    }
}
#endregion operation-observation

/// <summary>Estimates each candidate as its first coordinate times a fixed scale.</summary>
public sealed record ScaledEstimator(double Scale) : Operator<Estimation, RealVector>
{
    protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
        ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope) =>
        new Executor(Scale);

    private sealed class Executor(double scale) : IEstimationExecutor<RealVector>
    {
        public IReadOnlyList<double> Estimate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random) =>
            [.. candidates.Select(candidate => scale * candidate[0])];
    }
}

public sealed class OperationDefinitionDocumentationSpecs
{
    [Fact]
    public void AnOperationOfYourOwnIsResolvedChosenPerItemAndObserved()
    {
        var searchSpace = new BoundedRealVectorSearchSpace(1, -10.0, 10.0);
        var problem = FuncProblem.Create((RealVector candidate) => candidate[0] * candidate[0], searchSpace, SingleObjective.Minimize);
        IOperator<Estimation, RealVector> estimator = ChoosePerItem.Create<Estimation, RealVector>(new ScaledEstimator(1.0), new ScaledEstimator(10.0));
        var counts = estimator.Invocations.Trace(observation => observation.Estimates.Count);
        var root = ResolutionScope.Create();
        var scope = root.For<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>(problem);
        RealVector[] candidates = [new(1.0), new(2.0), new(3.0)];

        var estimates = scope.Resolve(estimator.Substitute(counts)).Estimate(candidates, RandomNumberGenerator.Create(42));

        estimates.Count.ShouldBe(candidates.Length);
        for (var i = 0; i < candidates.Length; i++)
            estimates[i].ShouldBeOneOf(candidates[i][0], 10.0 * candidates[i][0]);
        root.Results.Result(counts).Snapshot().Select(entry => entry.Value).ShouldBe([3]);
    }
}
