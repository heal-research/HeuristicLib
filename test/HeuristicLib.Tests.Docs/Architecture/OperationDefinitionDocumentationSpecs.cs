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
public interface IEstimationExecutor<TCandidate, in TSearchSpace, in TProblem> : IExecutor
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IReadOnlyList<double> Estimate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}
#endregion operation-executor

#region operation-type
public sealed class Estimation : IItemOperation<Estimation>
{
    static bool IOperation<Estimation>.IsExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(IExecutor executor) =>
        executor is IEstimationExecutor<TCandidate, TSearchSpace, TProblem>;

    static IExecutor IOperation<Estimation>.Wrap<TCandidate, TSearchSpace, TProblem, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper) =>
        new WrappedExecutor<TCandidate, TSearchSpace, TProblem, TWrapper>(executor, wrapper);

    static IExecutor IItemOperation<Estimation>.Route<TCandidate, TSearchSpace, TProblem, TSearchState>(WeightedRouter<IExecutor> children) =>
        new RoutedExecutor<TCandidate, TSearchSpace, TProblem>(children);

    private readonly struct Invocation<TCandidate, TSearchSpace, TProblem>(IEstimationExecutor<TCandidate, TSearchSpace, TProblem> executor,
        IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) : IInvocation<IReadOnlyList<double>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        private readonly IEstimationExecutor<TCandidate, TSearchSpace, TProblem> executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;
        public ISearchSpace SearchSpace => searchSpace;
        public IProblem Problem => problem;

        public IReadOnlyList<double> Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((IEstimationExecutor<TCandidate, TSearchSpace, TProblem>?)executor ?? this.executor).Estimate(candidates, random ?? this.random, searchSpace, problem);
    }

    private sealed class WrappedExecutor<TCandidate, TSearchSpace, TProblem, TWrapper>(IExecutor executor, TWrapper wrapper)
        : WrappingExecutor<IEstimationExecutor<TCandidate, TSearchSpace, TProblem>, TWrapper>(executor, wrapper), IEstimationExecutor<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TWrapper : struct, IInvocationWrapper
    {
        public IReadOnlyList<double> Estimate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            Wrapper.Invoke<Invocation<TCandidate, TSearchSpace, TProblem>, IReadOnlyList<double>>(new(Executor, candidates, random, searchSpace, problem));
    }

    private sealed class RoutedExecutor<TCandidate, TSearchSpace, TProblem>(WeightedRouter<IExecutor> children)
        : RoutingExecutor<IEstimationExecutor<TCandidate, TSearchSpace, TProblem>>(children), IEstimationExecutor<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IReadOnlyList<double> Estimate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            Children.Route(candidates, random, (child, batch) => child.Estimate(batch, random, searchSpace, problem));
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
        public IEstimationExecutor<TCandidate, TSearchSpace, TProblem> Resolve(IOperator<Estimation, TCandidate> source) =>
            (IEstimationExecutor<TCandidate, TSearchSpace, TProblem>)scope.ResolveOperator(source);

        public bool TryResolve(IOperator<Estimation, TCandidate> source,
            [NotNullWhen(true)] out IEstimationExecutor<TCandidate, TSearchSpace, TProblem>? executor, [NotNullWhen(false)] out string? reason) =>
            ResolutionScope.TryResolve(() => scope.Resolve(source), out executor, out reason);
    }
}
#endregion operation-resolution

#region operation-observation
public sealed record EstimationObservation<TCandidate>(
    IOperator<Estimation, TCandidate> Estimator,
    IReadOnlyList<TCandidate> Candidates,
    IReadOnlyList<double> Estimates,
    ISearchSpace<TCandidate> SearchSpace,
    IProblem<TCandidate, ISearchSpace<TCandidate>> Problem);

file sealed class EstimationObservationAdvice<TCandidate>(Action<EstimationObservation<TCandidate>> observe)
    : OperatorAdvice<Estimation, TCandidate>
{
    public override IExecutor Wrap<TSearchSpace, TProblem, TSearchState>(IExecutor executor, OperationInfo operation,
        IOperator<Estimation, TCandidate> source) =>
        new Executor<TSearchSpace, TProblem>((IEstimationExecutor<TCandidate, TSearchSpace, TProblem>)executor, source, observe);

    private sealed class Executor<TSearchSpace, TProblem>(IEstimationExecutor<TCandidate, TSearchSpace, TProblem> executor,
        IOperator<Estimation, TCandidate> estimator, Action<EstimationObservation<TCandidate>> observe)
        : IEstimationExecutor<TCandidate, TSearchSpace, TProblem>, IExecutorControlProvider
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IReadOnlyList<double> Estimate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var estimates = executor.Estimate(candidates, random, searchSpace, problem);
            observe(new EstimationObservation<TCandidate>(estimator, candidates, estimates, searchSpace, problem));
            return estimates;
        }

        public TControl? Get<TControl>() where TControl : class => ExecutorControls.Find<TControl>(executor);
    }
}

public static class EstimationObservations
{
    extension<TCandidate>(IOperator<Estimation, TCandidate> estimator)
    {
        public ObservationSource<EstimationObservation<TCandidate>> Calls =>
            new(estimator, observe => Aspect.Create(Pointcut.Reference(estimator), new EstimationObservationAdvice<TCandidate>(observe)));
    }
}
#endregion operation-observation

/// <summary>Estimates each candidate as its first coordinate times a fixed scale.</summary>
public sealed record ScaledEstimator(double Scale) : Operator<Estimation, RealVector>
{
    protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
        ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope) =>
        new Executor<TSearchSpace, TProblem>(Scale);

    private sealed class Executor<TSearchSpace, TProblem>(double scale) : IEstimationExecutor<RealVector, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<RealVector>
        where TProblem : class, IProblem<RealVector, TSearchSpace>
    {
        public IReadOnlyList<double> Estimate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random,
            TSearchSpace searchSpace, TProblem problem) =>
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
        var estimator = ChoosePerItem.Create<Estimation, RealVector>(new ScaledEstimator(1.0), new ScaledEstimator(10.0));
        var counts = Analyzer.Trace(estimator.Calls, observation => observation.Estimates.Count);
        var scope = ResolutionScope.Create(counts).For<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>();
        RealVector[] candidates = [new(1.0), new(2.0), new(3.0)];

        var estimates = scope.Resolve(estimator).Estimate(candidates, RandomNumberGenerator.Create(42), searchSpace, problem);

        estimates.Count.ShouldBe(candidates.Length);
        for (var i = 0; i < candidates.Length; i++)
            estimates[i].ShouldBeOneOf(candidates[i][0], 10.0 * candidates[i][0]);
        counts.Snapshot().Select(entry => entry.Value).ShouldBe([3]);
    }
}
