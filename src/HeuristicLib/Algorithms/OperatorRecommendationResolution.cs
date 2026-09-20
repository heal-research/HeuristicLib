using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

/// <summary>
/// Collects operator parameters from supplied values, problem recommendations and search space recommendations.
/// </summary>
public sealed class OperatorRecommendationResolution
{
    private readonly IProblem? problem;
    private readonly ISearchSpace searchSpace;
    private List<string>? missingParameters;

    public OperatorRecommendationResolution(IProblem? problem, ISearchSpace searchSpace)
    {
        this.problem = problem;
        this.searchSpace = searchSpace;
    }

    /// <summary>
    /// Returns the supplied operator, otherwise tries the problem and then the search space. Records the parameter
    /// when neither source recommends an operator.
    /// </summary>
    public TOperator? GetOrRecommend<TOperator>(string parameterName, TOperator? supplied)
        where TOperator : class, IOperator
    {
        var resolved = supplied;
        if (resolved is null
            && problem is IRecommends<TOperator> problemRecommendations
            && problemRecommendations.TryCreateRecommendedOperator(out var problemRecommendation))
        {
            resolved = problemRecommendation;
        }

        if (resolved is null
            && searchSpace is IRecommends<TOperator> searchSpaceRecommendations
            && searchSpaceRecommendations.TryCreateRecommendedOperator(out var searchSpaceRecommendation))
        {
            resolved = searchSpaceRecommendation;
        }

        if (resolved is null)
        {
            missingParameters ??= [];
            missingParameters.Add(parameterName);
        }

        return resolved;
    }

    /// <summary>Throws one exception naming every operator parameter that a preceding resolution left unresolved.</summary>
    public void ThrowIfIncomplete(string algorithmName)
    {
        if (missingParameters is null)
        {
            return;
        }

        var parameterLabel = missingParameters.Count == 1 ? "parameter" : "parameters";
        var sources = problem is null
            ? searchSpace.GetType().Name
            : $"{problem.GetType().Name} and {searchSpace.GetType().Name}";
        throw new InvalidOperationException(
            $"Cannot create {algorithmName}. No value or recommendation was found for required {parameterLabel}: " +
            $"{string.Join(", ", missingParameters)}. Consulted {sources}.");
    }
}
