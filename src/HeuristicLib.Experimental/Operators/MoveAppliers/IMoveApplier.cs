using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveAppliers;

public interface IMoveApplier<TCandidate, in TMove> : IOperator
{
    ExecutionFactory<IMoveApplierExecution<TCandidate, TRunSearchSpace, TRunProblem, TMove>> CreateExecutionFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public interface IMoveApplierExecution<TCandidate, in TSearchSpace, in TProblem, in TMove>
    : IOperatorExecution
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    TCandidate Apply(
        TCandidate candidate,
        TMove move,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);
}

public static class MoveApplierResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public IMoveApplierExecution<TCandidate, TSearchSpace, TProblem, TMove> Resolve<TCandidate, TSearchSpace, TProblem, TMove>(IMoveApplier<TCandidate, TMove> moveApplier)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            scope.Resolve(moveApplier, static source => source.CreateExecutionFactory<TSearchSpace, TProblem>(), CreateWrapperFactory<TCandidate, TSearchSpace, TProblem, TMove>);

        [return: NotNullIfNotNull(nameof(moveApplier))]
        public IMoveApplierExecution<TCandidate, TSearchSpace, TProblem, TMove>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TMove>(IMoveApplier<TCandidate, TMove>? moveApplier)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            moveApplier is null ? null : scope.Resolve<TCandidate, TSearchSpace, TProblem, TMove>(moveApplier);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem, TMove>(
            IMoveApplier<TCandidate, TMove> moveApplier,
            [NotNullWhen(true)] out IMoveApplierExecution<TCandidate, TSearchSpace, TProblem, TMove>? execution,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
        {
            try
            {
                execution = scope.Resolve<TCandidate, TSearchSpace, TProblem, TMove>(moveApplier);
                reason = null;
                return true;
            }
            catch (InvalidOperationException exception)
            {
                execution = null;
                reason = exception.Message;
                return false;
            }
        }
    }

    extension<TCandidate, TSearchSpace, TProblem>(ResolutionScope<TCandidate, TSearchSpace, TProblem> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IMoveApplierExecution<TCandidate, TSearchSpace, TProblem, TMove> Resolve<TMove>(IMoveApplier<TCandidate, TMove> moveApplier) =>
            scope.Scope.Resolve<TCandidate, TSearchSpace, TProblem, TMove>(moveApplier);

        [return: NotNullIfNotNull(nameof(moveApplier))]
        public IMoveApplierExecution<TCandidate, TSearchSpace, TProblem, TMove>? ResolveOptional<TMove>(IMoveApplier<TCandidate, TMove>? moveApplier) =>
            moveApplier is null ? null : scope.Resolve(moveApplier);

        public bool TryResolve<TMove>(
            IMoveApplier<TCandidate, TMove> moveApplier,
            [NotNullWhen(true)] out IMoveApplierExecution<TCandidate, TSearchSpace, TProblem, TMove>? execution,
            [NotNullWhen(false)] out string? reason) =>
            scope.Scope.TryResolve(moveApplier, out execution, out reason);
    }

    private static WrapperExecutionFactory<IMoveApplierExecution<TCandidate, TSearchSpace, TProblem, TMove>> CreateWrapperFactory<TCandidate, TSearchSpace, TProblem, TMove>(Advice advice, IMoveApplier<TCandidate, TMove> configuration)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        => throw new InvalidOperationException($"{advice.GetType().Name} is not a single-child wrapper for {typeof(IMoveApplier<TCandidate, TMove>).Name} producing {typeof(IMoveApplierExecution<TCandidate, TSearchSpace, TProblem, TMove>).Name}.");
}
