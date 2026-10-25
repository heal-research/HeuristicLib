using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Execution;

public partial class ResolutionScope
{
    /// <summary>Builds an algorithm's executor for the run's types and applies the matching algorithm advice.</summary>
    /// <remarks>
    /// The algorithm must produce the requested search state and fit the run, which it answers for its children too. A
    /// mismatch names every configuration responsible, such as an operator written for another search space.
    /// </remarks>
    public IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate> source)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        if (source is not IAlgorithm<TCandidate, TSearchState> algorithm)
            throw new InvalidOperationException($"{ExecutionSignature.Name(source.GetType())} produces {ProducedSearchStates(source)}, " +
                $"and cannot run producing {ExecutionSignature.Name(typeof(TSearchState))}.");
        var execution = ExecutionSignature.For<TSearchSpace, TProblem, TSearchState>();
        if (!algorithm.Fits(execution))
            throw SearchConfigurationValidation.Mismatch(algorithm, execution);
        if (!state.Constructing.Add(algorithm))
            throw new InvalidOperationException($"Recursive construction of {algorithm.GetType().Name}.");
        try
        {
            var owner = state.GetOrAddOwner(algorithm);
            var executor = algorithm.CreateExecutor(WithState(owner).For<TCandidate, TSearchSpace, TProblem, TSearchState>());
            foreach (var (pointcut, advice) in Advices())
                if (pointcut.Matches(algorithm))
                    executor = advice switch
                    {
                        AlgorithmAdvice<TCandidate, TSearchState> typed => typed.Wrap(executor, algorithm),
                        // Advice for another search state of a configuration producing several.
                        IAlgorithmAdvice => executor,
                        _ => throw new InvalidOperationException($"{advice.GetType().FullName} cannot wrap algorithm {algorithm.GetType().Name}. " +
                            "Algorithm advice derives from AlgorithmAdvice<TCandidate, TSearchState>.")
                    };
            return executor;
        }
        finally
        {
            state.Constructing.Remove(algorithm);
        }
    }

    [return: NotNullIfNotNull(nameof(algorithm))]
    public IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate>? algorithm)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        algorithm is null ? null : Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm);

    /// <remarks>Invalid-operation failures become reasons; other exceptions propagate. Failed attempts may be retried.</remarks>
    public bool TryResolve<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate> algorithm,
        [NotNullWhen(true)] out IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>? executor, [NotNullWhen(false)] out string? reason)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        Attempt(() => Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm), out executor, out reason);

    private static string ProducedSearchStates(IConfigurationNode source) =>
        string.Join(" or ", source.GetType().GetInterfaces()
            .Where(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IAlgorithm<,>))
            .Select(contract => ExecutionSignature.Name(contract.GetGenericArguments()[1]))
            .DefaultIfEmpty("no search state"));
}
