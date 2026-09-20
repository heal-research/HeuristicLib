using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// The resolve-time check an observation applies, because the configuration it observes names fewer types than the
/// observation reads.
/// </summary>
/// <remarks>
/// It is the rule the authoring bases apply, so an observation fits exactly the runs that an operator written for the
/// same search space, problem and search state would fit, and a mismatch reads the same as theirs.
/// </remarks>
internal static class ObservationSignature
{
    public static bool Fits<TSearchSpace, TProblem>(ExecutionSignature execution) =>
        execution.SearchSpace.IsAssignableTo(typeof(TSearchSpace)) && execution.Problem.IsAssignableTo(typeof(TProblem));

    public static bool Fits<TSearchSpace, TProblem, TSearchState>(ExecutionSignature execution) =>
        Fits<TSearchSpace, TProblem>(execution) && execution.SearchState.IsAssignableTo(typeof(TSearchState));

    public static void Require<TSearchSpace, TProblem, TRunSearchSpace, TRunProblem>(object observation)
    {
        if (!typeof(TRunSearchSpace).IsAssignableTo(typeof(TSearchSpace)) || !typeof(TRunProblem).IsAssignableTo(typeof(TProblem)))
        {
            throw ExecutionSignature.Mismatch(
                observation,
                ExecutionSignature.Describe(typeof(TSearchSpace), typeof(TProblem)),
                ExecutionSignature.Describe(typeof(TRunSearchSpace), typeof(TRunProblem)));
        }
    }

    public static void Require<TSearchSpace, TProblem, TSearchState, TRunSearchSpace, TRunProblem, TRunSearchState>(object observation)
    {
        if (!typeof(TRunSearchSpace).IsAssignableTo(typeof(TSearchSpace))
            || !typeof(TRunProblem).IsAssignableTo(typeof(TProblem))
            || !typeof(TRunSearchState).IsAssignableTo(typeof(TSearchState)))
        {
            throw ExecutionSignature.Mismatch(
                observation,
                ExecutionSignature.Describe(typeof(TSearchSpace), typeof(TProblem), typeof(TSearchState)),
                ExecutionSignature.Describe(typeof(TRunSearchSpace), typeof(TRunProblem), typeof(TRunSearchState)));
        }
    }
}
