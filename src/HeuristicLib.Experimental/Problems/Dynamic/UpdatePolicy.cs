namespace HEAL.HeuristicLib.Problems.Dynamic;

/// <summary>
/// How much work a dynamic problem lets finish before it applies an environment update an epoch asked for.
/// </summary>
/// <remarks>
/// Evaluation and batch policies apply a due update when the next unit begins, leaving the completed results readable
/// in the environment that scored them. Iteration updates apply after the named algorithm yields its completed state.
/// Change during one evaluation is not supported: that objective value would describe neither environment.
/// </remarks>
public enum UpdatePolicy
{
    /// <summary>
    /// After one candidate, so a population ends up scored by several environments.
    /// </summary>
    AfterEachEvaluation,

    /// <summary>
    /// After one batch of candidates, so every candidate of a batch is scored by one environment.
    /// </summary>
    AfterEachBatchEvaluation,

    /// <summary>
    /// After one iteration of the algorithm named by
    /// <see cref="DynamicProblem{TSelf, TCandidate, TSearchSpace}.CreateIterationUpdateAspect"/>, which this policy needs.
    /// </summary>
    AfterEachIteration
}
