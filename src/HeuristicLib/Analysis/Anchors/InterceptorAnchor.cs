using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// The inputs and outputs captured after one interceptor call.
/// </summary>
/// <remarks>
/// <see cref="State"/> is the transformed state the algorithm goes on to yield. <see cref="UntransformedState"/> is what
/// the interceptor received, which is useful when an analysis needs to measure what the transformation removed.
/// </remarks>
public sealed record InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>(
    IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> Interceptor,
    TSearchState State,
    TSearchState UntransformedState,
    TSearchState? PreviousState,
    TSearchSpace SearchSpace,
    TProblem Problem) : Observation<TProblem>(Problem)
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState;

/// <summary>
/// An anchor at every interceptor call.
/// </summary>
public sealed class InterceptorAnchor<TCandidate, TSearchSpace, TProblem, TSearchState>(IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> interceptor)
    : IAnchor<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> Interceptor { get; } = interceptor;

    public void Install(ExecutionInstanceResolverBuilder builder, IObservationRecorder<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>> recorder) =>
        builder.Decorate(Interceptor, current => new ObservingInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>(Interceptor, current, [recorder]));
}

/// <summary>
/// Delivers an observation after every interceptor call.
/// </summary>
internal sealed record ObservingInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    : WrappingInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    private readonly IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> anchor;
    private readonly ValueArray<IObservationRecorder<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>>> recorders;

    public ObservingInterceptor(
        IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> anchor,
        IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> childInterceptor,
        IReadOnlyList<IObservationRecorder<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>>> recorders)
        : base(childInterceptor)
    {
        this.anchor = anchor;
        this.recorders = recorders.ToValueArray();
    }

    protected override WrappingInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> childInterceptor) =>
        new Instance(anchor, childInterceptor, recorders);

    private sealed class Instance(
        IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> anchor,
        IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> childInterceptor,
        ValueArray<IObservationRecorder<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>>> recorders)
        : WrappingInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(childInterceptor)
    {
        public override TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var result = ChildInterceptor.Transform(currentState, previousState, random, searchSpace, problem);
            var observation = new InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>(
                anchor,
                result,
                currentState,
                previousState,
                searchSpace,
                problem);
            foreach (var recorder in recorders)
                recorder.Record(observation);

            return result;
        }
    }
}
