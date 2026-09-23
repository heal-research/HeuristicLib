namespace HEAL.HeuristicLib.Problems.Dynamic;

/// <summary>
/// How much work a dynamic problem lets finish before it applies an environment update an epoch asked for.
/// </summary>
/// <remarks>
/// A due update is postponed past the named unit of work and applied when the next one begins, so an update falling due
/// after the last unit is never applied. Change during one evaluation is not supported: that objective value would
/// describe neither environment.
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
    /// <see cref="DynamicProblem{TSelf, TCandidate, TSearchSpace}.CreateIterationUpdateModule"/>, which this policy needs.
    /// </summary>
    AfterEachIteration
}
