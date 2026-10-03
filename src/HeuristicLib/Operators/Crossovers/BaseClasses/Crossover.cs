using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

/// <remarks>
/// Derive directly from this base when the crossover owns child execution nodes or needs direct control over its execution structure.
/// Use <see cref="StatelessCrossover{TCandidate,TSearchSpace,TProblem}"/> when no mutable execution data is needed.
/// Use <see cref="StatefulCrossover{TCandidate,TSearchSpace,TProblem,TState}"/> when only ordinary execution data is needed.
/// <para>
/// The type arguments are the search space and problem this crossover is written for. The base bridges to whatever a
/// run requests, and a request the crossover was not written for is reported when the execution graph is built. A
/// crossover that owns children stays agnostic and derives from <see cref="WrappingCrossover{TCandidate}"/> or
/// <see cref="MultiCrossover{TCandidate}"/> instead.
/// </para>
/// </remarks>
public abstract record Crossover<TCandidate, TSearchSpace, TProblem>
    : ICrossover<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public abstract ExecutionFactory<ICrossoverExecution<TCandidate, TSearchSpace, TProblem>> CreateExecutionFactory();

    public bool Fits(ExecutionSignature execution) => execution.SearchSpace.IsAssignableTo(typeof(TSearchSpace)) && execution.Problem.IsAssignableTo(typeof(TProblem));

    ExecutionFactory<ICrossoverExecution<TCandidate, TRunSearchSpace, TRunProblem>> ICrossover<TCandidate>.CreateExecutionFactory<TRunSearchSpace, TRunProblem>()
    {
        if (!typeof(TRunSearchSpace).IsAssignableTo(typeof(TSearchSpace)) || !typeof(TRunProblem).IsAssignableTo(typeof(TProblem)))
        {
            throw ExecutionSignature.Mismatch(
                this,
                ExecutionSignature.Describe(typeof(TSearchSpace), typeof(TProblem)),
                ExecutionSignature.Describe(typeof(TRunSearchSpace), typeof(TRunProblem)));
        }

        return (ExecutionFactory<ICrossoverExecution<TCandidate, TRunSearchSpace, TRunProblem>>)CreateExecutionFactory();
    }
}

public abstract record Crossover<TCandidate, TSearchSpace>
    : Crossover<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>;

public abstract record Crossover<TCandidate>
    : Crossover<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>;

public abstract class CrossoverExecution<TCandidate, TSearchSpace, TProblem>
    : ICrossoverExecution<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract class CrossoverExecution<TCandidate, TSearchSpace>
    : ICrossoverExecution<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace);

    IReadOnlyList<TCandidate> ICrossoverExecution<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>.Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) =>
        Cross(parents, random, searchSpace);
}

public abstract class CrossoverExecution<TCandidate>
    : ICrossoverExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    public abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random);

    IReadOnlyList<TCandidate> ICrossoverExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>.Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        Cross(parents, random);
}
