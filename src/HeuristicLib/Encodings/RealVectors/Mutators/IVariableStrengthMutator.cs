using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Encodings.RealVectors;

public interface IVariableStrengthMutator<TCandidate, in TSearchSpace, in TProblem>
    : IMutator<TCandidate>, IOperator<IVariableStrengthMutatorInstance<TCandidate, TSearchSpace, TProblem>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    /// <summary>Gets the value used to initialize each execution instance's current mutation strength.</summary>
    double MutationStrength { get; }
}

public interface IVariableStrengthMutatorInstance<TCandidate, in TSearchSpace, in TProblem>
    : IMutatorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    /// <summary>Gets or sets the run scoped strength without changing the reusable mutator configuration.</summary>
    double CurrentMutationStrength { get; set; }
}
