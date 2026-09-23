using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Execution;

/// <summary>
/// A read-only capture at a defined algorithm or operator boundary.
/// </summary>
public abstract record Observation;

/// <summary>
/// A capture at a boundary of a run over <typeparamref name="TProblem"/>.
/// </summary>
/// <remarks>
/// Every boundary the library observes is reached by calling an operator with the run's problem, so every observation
/// carries it. Declaring that once here rather than in each observation is what lets analysis reach the run's
/// objective through the problem that defines it, instead of being handed an objective from somewhere else.
/// </remarks>
public abstract record Observation<TProblem>(TProblem Problem) : Observation
    where TProblem : class, IProblem;
