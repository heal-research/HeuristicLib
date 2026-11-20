using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

/// <remarks>
/// Derive directly from this base when the replacer needs direct control over its execution structure.
/// Use <see cref="StatelessReplacer{TCandidate,TSearchSpace,TProblem}"/> when no mutable execution data is needed.
/// <para>
/// The type arguments are the search space and problem this replacer is written for. The base bridges to whatever a
/// run requests, and a request the replacer was not written for is reported when the execution graph is built. A
/// replacer that owns children stays agnostic and derives from <see cref="WrappingOperator{Replacement, TCandidate}"/> or
/// <see cref="MultiOperator{Replacement, TCandidate}"/> instead.
/// </para>
/// </remarks>
public abstract record Replacer<TCandidate, TSearchSpace, TProblem>
    : Operator<Replacement, TCandidate, TSearchSpace, TProblem, IReplacementExecutor<TCandidate>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>;

public abstract record Replacer<TCandidate, TSearchSpace>
    : Replacer<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>;

public abstract record Replacer<TCandidate>
    : Replacer<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>;

public abstract class ReplacementExecutor<TCandidate>
    : IReplacementExecutor<TCandidate>
{
    public abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, int count, IRandomNumberGenerator random);
}
