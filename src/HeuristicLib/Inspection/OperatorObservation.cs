using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>One call of an operator for the operation <typeparamref name="TOperation"/>, with what the call returned.</summary>
/// <remarks>
/// One observation type serves every operation, including operations defined outside the library. Each library
/// operation adds properties reading its arguments and result, such as <c>Offspring</c> for crossover and mutation. The
/// search space and problem are read at the candidate's interfaces, so an observation fits every run over the
/// candidate.
/// </remarks>
public sealed record OperatorObservation<TOperation, TCandidate> : Observation<IProblem<TCandidate, ISearchSpace<TCandidate>>>
    where TOperation : IOperationDescriptor<TOperation>
{
    internal OperatorObservation(IOperator<TOperation, TCandidate> source, object invocation, object? result,
        ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem)
        : base(problem)
    {
        Operator = source;
        Invocation = invocation;
        Result = result;
        SearchSpace = searchSpace;
    }

    /// <summary>The observed operator configuration.</summary>
    public IOperator<TOperation, TCandidate> Operator { get; }

    /// <summary>The call, as the operation's invocation; it carries the call's arguments.</summary>
    /// <remarks>An operation reads its arguments from here by the interface its invocation implements.</remarks>
    public object Invocation { get; }

    /// <summary>What the call returned.</summary>
    public object? Result { get; }

    /// <summary>The search space passed to the call.</summary>
    public ISearchSpace<TCandidate> SearchSpace { get; }
}

/// <summary>Reports every call to each operator its pointcut selects, for one operation.</summary>
/// <param name="observe">Receives each observation on the thread that called the operator, after the call returned.</param>
public sealed class OperatorObservationAdvice<TOperation, TCandidate>(Action<OperatorObservation<TOperation, TCandidate>> observe)
    : OperatorAdvice<TOperation, TCandidate>
    where TOperation : IOperationDescriptor<TOperation>
{
    public override IExecutor Wrap<TSearchSpace, TProblem, TSearchState>(IExecutor executor, OperationExecutorAdapter operation,
        IOperator<TOperation, TCandidate> source) =>
        operation.WrapInvocation(executor, new ObservingWrapper(source, observe));

    private readonly struct ObservingWrapper(IOperator<TOperation, TCandidate> source, Action<OperatorObservation<TOperation, TCandidate>> observe)
        : IInvocationWrapper
    {
        // The run's search space and problem implement the candidate's interfaces, which the operation's constraints guarantee.
        public TResult Invoke<TInvocation, TResult>(TInvocation invocation) where TInvocation : struct, IInvocation<TResult>
        {
            var result = invocation.Proceed();
            observe(new OperatorObservation<TOperation, TCandidate>(source, invocation, result,
                (ISearchSpace<TCandidate>)invocation.SearchSpace, (IProblem<TCandidate, ISearchSpace<TCandidate>>)invocation.Problem));
            return result;
        }
    }
}

public static class OperatorObservations
{
    extension(Aspect)
    {
        /// <summary>Observes every call to one chosen operator.</summary>
        public static Aspect Observe<TOperation, TCandidate>(IOperator<TOperation, TCandidate> source,
            Action<OperatorObservation<TOperation, TCandidate>> observe)
            where TOperation : IOperationDescriptor<TOperation> =>
            Aspect.Create(Pointcut.Reference(source), new OperatorObservationAdvice<TOperation, TCandidate>(observe));
    }

    extension<TCandidate>(OperatorObservation<Creation, TCandidate> observation)
    {
        /// <summary>The number of candidates requested.</summary>
        public int Count => ((ICreationArguments)observation.Invocation).Count;

        /// <summary>The created candidates.</summary>
        public IReadOnlyList<TCandidate> Candidates => (IReadOnlyList<TCandidate>)observation.Result!;
    }

    extension<TCandidate>(OperatorObservation<Crossover, TCandidate> observation)
    {
        public IReadOnlyList<Parents<TCandidate>> Parents => ((ICrossoverArguments<TCandidate>)observation.Invocation).Parents;

        public IReadOnlyList<TCandidate> Offspring => (IReadOnlyList<TCandidate>)observation.Result!;
    }

    extension<TCandidate>(OperatorObservation<Mutation, TCandidate> observation)
    {
        public IReadOnlyList<TCandidate> Parents => ((IMutationArguments<TCandidate>)observation.Invocation).Parents;

        public IReadOnlyList<TCandidate> Offspring => (IReadOnlyList<TCandidate>)observation.Result!;
    }

    extension<TCandidate>(OperatorObservation<Evaluation, TCandidate> observation)
    {
        public IReadOnlyList<TCandidate> Candidates => ((IEvaluationArguments<TCandidate>)observation.Invocation).Candidates;

        /// <summary>The objective vectors of the candidates, in their order.</summary>
        public IReadOnlyList<ObjectiveVector> ObjectiveVectors => (IReadOnlyList<ObjectiveVector>)observation.Result!;
    }

    extension<TCandidate>(OperatorObservation<Refinement, TCandidate> observation)
    {
        public IReadOnlyList<TCandidate> Candidates => ((IRefinementArguments<TCandidate>)observation.Invocation).Candidates;

        public IReadOnlyList<TCandidate> Refined => (IReadOnlyList<TCandidate>)observation.Result!;
    }

    extension<TCandidate>(OperatorObservation<Selection, TCandidate> observation)
    {
        public IReadOnlyList<EvaluatedCandidate<TCandidate>> Population => ((ISelectionArguments<TCandidate>)observation.Invocation).Population;

        public ObjectiveDirections Objective => ((ISelectionArguments<TCandidate>)observation.Invocation).Objective;

        /// <summary>The number of candidates requested.</summary>
        public int Count => ((ISelectionArguments<TCandidate>)observation.Invocation).Count;

        public IReadOnlyList<EvaluatedCandidate<TCandidate>> Selected => (IReadOnlyList<EvaluatedCandidate<TCandidate>>)observation.Result!;
    }

    extension<TCandidate>(OperatorObservation<Replacement, TCandidate> observation)
    {
        public IReadOnlyList<EvaluatedCandidate<TCandidate>> PreviousPopulation => ((IReplacementArguments<TCandidate>)observation.Invocation).PreviousPopulation;

        public IReadOnlyList<EvaluatedCandidate<TCandidate>> OffspringPopulation => ((IReplacementArguments<TCandidate>)observation.Invocation).OffspringPopulation;

        public ObjectiveDirections Objective => ((IReplacementArguments<TCandidate>)observation.Invocation).Objective;

        /// <summary>The number of candidates requested.</summary>
        public int Count => ((IReplacementArguments<TCandidate>)observation.Invocation).Count;

        public IReadOnlyList<EvaluatedCandidate<TCandidate>> Replaced => (IReadOnlyList<EvaluatedCandidate<TCandidate>>)observation.Result!;
    }

    extension<TCandidate>(OperatorObservation<Termination, TCandidate> observation)
    {
        /// <summary>The search state the terminator checked.</summary>
        public ISearchState State => ((ITerminationArguments)observation.Invocation).State;

        public bool IsTerminal => (bool)observation.Result!;
    }

    extension<TCandidate, TSearchState>(OperatorObservation<Interception<TSearchState>, TCandidate> observation)
        where TSearchState : class, ISearchState
    {
        /// <summary>The search state the interceptor returned.</summary>
        public TSearchState State => (TSearchState)observation.Result!;

        /// <summary>The search state passed to the interceptor.</summary>
        public TSearchState UntransformedState => ((IInterceptionArguments<TSearchState>)observation.Invocation).CurrentState;

        public TSearchState? PreviousState => ((IInterceptionArguments<TSearchState>)observation.Invocation).PreviousState;
    }
}
