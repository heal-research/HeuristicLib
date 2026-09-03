using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <remarks>
/// A terminator written for a search space, problem or state the run does not supply is reported when the execution
/// graph is built.
/// </remarks>
public interface ITerminator<TCandidate> : IOperator
{
    ITerminatorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> CreateExecutionInstance<TRunSearchSpace, TRunProblem, TRunSearchState>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
        where TRunSearchState : class, ISearchState;
}

public interface ITerminatorInstance<TCandidate, in TSearchSpace, in TProblem, in TSearchState>
  : IOperatorInstance
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : class, ISearchState
{
    bool IsTerminalState(TSearchState state, TSearchSpace searchSpace, TProblem problem);
}

public static class TerminatorResolverExtensions
{
    extension(ExecutionInstanceRegistry registry)
    {
        public ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(ITerminator<TCandidate> terminator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState =>
            registry.Resolve(terminator, static (creationTarget, childRegistry) => creationTarget.CreateExecutionInstance<TSearchSpace, TProblem, TSearchState>(childRegistry));

        public ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TSearchState>(ITerminator<TCandidate>? terminator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState =>
            terminator is null ? null : registry.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(terminator);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem, TSearchState>(
            ITerminator<TCandidate> terminator,
            [NotNullWhen(true)] out ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? instance,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState
        {
            try
            {
                instance = registry.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(terminator);
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

    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(ExecutionInstanceResolver<TCandidate, TSearchSpace, TProblem, TSearchState> resolver)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        /// <remarks>
        /// Nothing is named at the call site: all four come from the resolver, which is why this role's resolver form
        /// takes the quadruple rather than the triple the other seven use.
        /// </remarks>
        public ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> Resolve(ITerminator<TCandidate> terminator) =>
            resolver.Registry.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(terminator);

        public ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? ResolveOptional(ITerminator<TCandidate>? terminator) =>
            terminator is null ? null : resolver.Registry.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(terminator);

        public bool TryResolve(
            ITerminator<TCandidate> terminator,
            [NotNullWhen(true)] out ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? instance,
            [NotNullWhen(false)] out string? reason) =>
            resolver.Registry.TryResolve(terminator, out instance, out reason);
    }
}
