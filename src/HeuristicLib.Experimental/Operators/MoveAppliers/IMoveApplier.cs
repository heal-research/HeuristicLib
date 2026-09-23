using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveAppliers;

public interface IMoveApplier<TCandidate, in TMove> : IOperator
{
    IMoveApplierInstance<TCandidate, TRunSearchSpace, TRunProblem, TMove> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ResolutionScope scope)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public interface IMoveApplierInstance<TCandidate, in TSearchSpace, in TProblem, in TMove>
    : IOperatorInstance
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
        public IMoveApplierInstance<TCandidate, TSearchSpace, TProblem, TMove> Resolve<TCandidate, TSearchSpace, TProblem, TMove>(IMoveApplier<TCandidate, TMove> moveApplier)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            scope.Resolve(moveApplier, static (creationTarget, childScope) => creationTarget.CreateExecutionInstance<TSearchSpace, TProblem>(childScope));

        public IMoveApplierInstance<TCandidate, TSearchSpace, TProblem, TMove>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TMove>(IMoveApplier<TCandidate, TMove>? moveApplier)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            moveApplier is null ? null : scope.Resolve<TCandidate, TSearchSpace, TProblem, TMove>(moveApplier);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem, TMove>(
            IMoveApplier<TCandidate, TMove> moveApplier,
            [NotNullWhen(true)] out IMoveApplierInstance<TCandidate, TSearchSpace, TProblem, TMove>? instance,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
        {
            try
            {
                instance = scope.Resolve<TCandidate, TSearchSpace, TProblem, TMove>(moveApplier);
                reason = null;
                return true;
            }
            catch (InvalidOperationException exception)
            {
                instance = null;
                reason = exception.Message;
                return false;
            }
        }
    }

    extension<TCandidate, TSearchSpace, TProblem>(ResolutionScope<TCandidate, TSearchSpace, TProblem> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IMoveApplierInstance<TCandidate, TSearchSpace, TProblem, TMove> Resolve<TMove>(IMoveApplier<TCandidate, TMove> moveApplier) =>
            scope.Scope.Resolve<TCandidate, TSearchSpace, TProblem, TMove>(moveApplier);

        public IMoveApplierInstance<TCandidate, TSearchSpace, TProblem, TMove>? ResolveOptional<TMove>(IMoveApplier<TCandidate, TMove>? moveApplier) =>
            moveApplier is null ? null : scope.Resolve(moveApplier);

        public bool TryResolve<TMove>(
            IMoveApplier<TCandidate, TMove> moveApplier,
            [NotNullWhen(true)] out IMoveApplierInstance<TCandidate, TSearchSpace, TProblem, TMove>? instance,
            [NotNullWhen(false)] out string? reason) =>
            scope.Scope.TryResolve(moveApplier, out instance, out reason);
    }
}
