using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.OperatorRolePrototype;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace OperatorRoleConsumer;

public interface IChecksumExecution : IExecutionNode
{
    int Read();
}

// This descriptor and its adapter are compiled in the consumer, never in the backend.
public sealed class Checksum : IOperation<Checksum>
{
    public static OperationBinding<TCandidate, Checksum, TSearchSpace, TProblem, TRunSearchState>
        CreateBinding<TCandidate, TSearchSpace, TProblem, TRunSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState =>
        new ChecksumBinding<TCandidate, TSearchSpace, TProblem, TRunSearchState>();
}

public sealed class ChecksumBinding<TCandidate, TSearchSpace, TProblem, TRunSearchState>
    : OperationBinding<TCandidate, Checksum, TSearchSpace, TProblem, TRunSearchState, IChecksumExecution>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TRunSearchState : class, ISearchState
{
    public override bool SupportsCandidateWork => false;

    protected override IChecksumExecution CreateSuccessExecution<THook>(IChecksumExecution child, THook hook) =>
        new SuccessExecution<THook>(child, hook);

    protected override IChecksumExecution CreateHooksExecution<THooks, TToken>(IChecksumExecution child, THooks hooks) =>
        new HooksExecution<THooks, TToken>(child, hooks);

    private sealed class HooksExecution<THooks, TToken>(IChecksumExecution child, THooks hooks) : IChecksumExecution
        where THooks : struct, ICallHooks<TToken>
    {
        public int Read()
        {
            var frame = new CallFrame<THooks, TToken>(hooks);
            try
            {
                int result;
                try { result = child.Read(); }
                catch (Exception failure)
                {
                    frame.Fault(failure);
                    throw;
                }
                frame.Success(0);
                return result;
            }
            finally { frame.Finally(); }
        }
    }

    private sealed class SuccessExecution<THook>(IChecksumExecution child, THook hook) : IChecksumExecution
        where THook : struct, ISuccessHook
    {
        public int Read()
        {
            var result = child.Read();
            hook.Success(0);
            return result;
        }
    }
}

public static class ChecksumResolutionExtensions
{
    public static IChecksumExecution Resolve<TCandidate, TSearchSpace, TProblem>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem> scope, IOperator<TCandidate, Checksum> source)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        scope.Scope.ResolveOperator<TCandidate, Checksum, TSearchSpace, TProblem, ISearchState,
            IChecksumExecution>(source, Checksum.CreateBinding<TCandidate, TSearchSpace, TProblem, ISearchState>()).Operation;
}

public sealed class ChecksumRecommendation : IRecommends<IOperator<RealVector, Checksum>>
{
    public bool TryCreateRecommendedOperator(
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out IOperator<RealVector, Checksum>? recommendation)
    {
        recommendation = new SharedOperations(new ProbeLog());
        return true;
    }
}

// An external generic topology uses public preparation APIs without enumerating operations.
public sealed record Forward<TCandidate, TOperation>(IOperator<TCandidate, TOperation> ChildOperator)
    : IOperator<TCandidate, TOperation> where TOperation : IOperation<TOperation>
{
    public IOperatorContract<TCandidate>? CandidateContract => ChildOperator.CandidateContract;
    public bool Fits(ExecutionSignature execution) => ChildOperator.Fits(execution);
    public PreparedOperator Prepare() => new Prepared(ChildOperator);

    private sealed class Prepared(IOperator<TCandidate, TOperation> child) : PreparedOperator
    {
        public override PreparedFactory Bind<TBoundCandidate, TBoundOperation, TSearchSpace, TProblem, TRunSearchState>(
            OperationBinding<TBoundCandidate, TBoundOperation, TSearchSpace, TProblem, TRunSearchState> binding) =>
            child is IOperator<TBoundCandidate, TBoundOperation> typed
                ? binding.Forward(typed)
                : throw new InvalidOperationException("Forward requested with a different candidate or operation.");
    }
}
