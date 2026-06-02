using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.APIs.RoarNet;

/// <summary>
/// Complete-solution-only ROAR-NET compatibility adapter.
/// Represents HLib mutation as a stochastic ROAR-NET local move.
/// Does not support partial solutions, construction/destruction,
/// finite neighborhood enumeration, true move uniqueness, or reversion.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="TS"></typeparam>
/// <typeparam name="TP"></typeparam>
/// <param name="Creator"></param>
/// <param name="Mutator"></param>
/// <param name="SearchSpace"></param>
/// <param name="Problem"></param>
/// <param name="rng"></param>
public record SimpleProblemOperations<T, TS, TP>(StatelessCreator<T, TS, TP> Creator, StatelessMutator<T, TS, TP> Mutator, TS SearchSpace, TP Problem, IRandomNumberGenerator rng) :
    ProblemOperations<T, TS, TP>(Creator, Mutator, SearchSpace, Problem, rng) where TS : class, ISearchSpace<T> where TP : class, IProblem<T, TS>, Problem
{
    public override MutationNeighborhood construction_neighbourhood(TP problem) => throw new NotSupportedException();

    public override MutationNeighborhood destruction_neighbourhood(TP problem) => throw new NotSupportedException();

    public override LazySolution<T> empty_solution(TP problem) => throw new NotSupportedException();
}
