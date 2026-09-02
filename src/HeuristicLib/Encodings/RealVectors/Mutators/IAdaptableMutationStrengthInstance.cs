using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Encodings.RealVectors;

/// <summary>
/// A mutator instance whose strength an algorithm may adapt over the course of a run.
/// </summary>
/// <remarks>
/// This is a capability of an execution instance, not a role, which is why it has no configuration counterpart: the
/// configuration is immutable and shared across runs, so a strength that changes during a run can only live here. Any
/// mutator may offer it, by returning an instance that implements it.
/// <para>
/// An algorithm that adapts strength, such as <c>EvolutionStrategy</c>, tests its resolved mutator for this interface
/// and leaves the strength alone when the mutator does not offer one. The configuration's own strength stays the value
/// every instance starts from.
/// </para>
/// </remarks>
public interface IAdaptableMutationStrengthInstance<TCandidate, in TSearchSpace, in TProblem>
    : IMutatorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    /// <summary>Gets or sets the run scoped strength without changing the reusable mutator configuration.</summary>
    double CurrentMutationStrength { get; set; }
}
