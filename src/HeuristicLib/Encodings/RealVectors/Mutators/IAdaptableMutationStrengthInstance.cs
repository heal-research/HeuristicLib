using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Encodings.RealVectors;

/// <remarks>
/// A capability, not a role, so it has no configuration counterpart. Any mutator may offer it by returning an instance
/// that implements it, and an algorithm that adapts strength tests its resolved mutator for it.
/// </remarks>
public interface IAdaptableMutationStrengthInstance<TCandidate, in TSearchSpace, in TProblem>
    : IMutatorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    /// <summary>Gets or sets the run scoped strength without changing the reusable mutator configuration.</summary>
    double CurrentMutationStrength { get; set; }
}
