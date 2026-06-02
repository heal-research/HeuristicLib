using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.Partial;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.APIs.RoarNet;

public interface IRoarNetMove<TG> : Move
{
    IRoarNetSolution<TG> Apply(IRoarNetSolution<TG> solution);
}

public interface IRoarNetNeighborhood<TG> : Neighbourhood
{
    IEnumerable<IRoarNetMove<TG>> moves(TG solution);
}

public interface IRoarNetSolution<TG> : Solution
{
    TG Genotype { get; }
    IRoarNetSolution<TG> Copy();
};

public interface IRoarNetProblem<TG> : Problem
{
    IRoarNetNeighborhood<TG> ConstructionNeighbourhood { get; }
    IRoarNetNeighborhood<TG> DestructionNeighbourhood { get; }
    IRoarNetNeighborhood<TG> LocalNeighbourhood { get; }
}

public readonly struct RoarNetProblem<TG, TS, TP, TM1, TM2, TM3>(
    TP problem,
    INeighborhood<TG, TS, TP, TM1> constructionNeighbourhood,
    INeighborhood<TG, TS, TP, TM2> destructionNeighbourhood,
    INeighborhood<TG, TS, TP, TM3> localNeighbourhood) : IRoarNetProblem<TG>
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
{
    public IRoarNetNeighborhood<TG> ConstructionNeighbourhood { get; } = new RoarNetNeighborhood<TG, TS, TP, TM1>(constructionNeighbourhood, problem);
    public IRoarNetNeighborhood<TG> DestructionNeighbourhood { get; } = new RoarNetNeighborhood<TG, TS, TP, TM2>(destructionNeighbourhood, problem);
    public IRoarNetNeighborhood<TG> LocalNeighbourhood { get; } = new RoarNetNeighborhood<TG, TS, TP, TM3>(localNeighbourhood, problem);
}

public readonly struct RoarNetSolution<TG>(TG genotype) : IRoarNetSolution<TG>
{
    public TG Genotype { get; } = genotype;
    public IRoarNetSolution<TG> Copy() => new RoarNetSolution<TG>(Genotype);
}

public readonly struct RoarNetMove<TG, TS, TP, TM>(TM move, INeighborhood<TG, TS, TP, TM> neighborhood, TP problem) : IRoarNetMove<TG>
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
{
    public IRoarNetSolution<TG> Apply(IRoarNetSolution<TG> solution) => new RoarNetSolution<TG>(neighborhood.ApplyMove(solution.Genotype, move, problem.SearchSpace, problem));
}

public readonly struct RoarNetNeighborhood<TG, TS, TP, TM>(INeighborhood<TG, TS, TP, TM> neighborhood, TP problem) : IRoarNetNeighborhood<TG>
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
{
    public IEnumerable<IRoarNetMove<TG>> moves(TG solution)
    {
        var n = neighborhood;
        var p = problem;
        return n.Moves(solution, null!, p.SearchSpace, p).Select(x => (IRoarNetMove<TG>)new RoarNetMove<TG, TS, TP, TM>(x, n, p));
    }
}

public abstract record BaseOperations<TG>() : Operations<TG>
{
    #region typeless
    public Solution apply_move(Move move, Solution solution) => apply_move((IRoarNetMove<TG>)move, (IRoarNetSolution<TG>)solution);

    public Neighbourhood construction_neighbourhood(Problem problem) => construction_neighbourhood((IRoarNetProblem<TG>)problem);

    public Solution copy_solution(Solution solution) => copy_solution((IRoarNetSolution<TG>)solution);

    public Neighbourhood destruction_neighbourhood(Problem problem) => throw new NotImplementedException();

    public Solution empty_solution(Problem problem) => throw new NotImplementedException();

    public Solution? heuristic_solution(Problem problem) => throw new NotImplementedException();

    public Neighbourhood local_neighbourhood(Problem problem) => throw new NotImplementedException();

    public double? lower_bound(Solution solution) => throw new NotImplementedException();

    public double? lower_bound_increment(Move move, Solution solution) => throw new NotImplementedException();

    public IEnumerable<Move> moves(Neighbourhood neighbourhood, Solution solution) => throw new NotImplementedException();

    public double? objective_value(Solution solution) => throw new NotImplementedException();

    public double? objective_value_increment(Move move, Solution solution) => throw new NotImplementedException();

    public Move? random_move(Neighbourhood neighbourhood, Solution solution) => throw new NotImplementedException();

    public IEnumerable<Move> random_moves_without_replacement(Neighbourhood neighbourhood, Solution solution) => throw new NotImplementedException();

    public Solution random_solution(Problem problem) => throw new NotImplementedException();

    public Solution revert_move(Move move, Solution solution) => throw new NotImplementedException();
    #endregion

    //from here typed operations

    public IRoarNetSolution<TG> apply_move(IRoarNetMove<TG> roarNetMove, IRoarNetSolution<TG> solution) => roarNetMove.Apply(solution);

    public IRoarNetNeighborhood<TG> construction_neighbourhood(IRoarNetProblem<TG> problem) => problem.ConstructionNeighbourhood;

    public IRoarNetSolution<TG> copy_solution(IRoarNetSolution<TG> solution) => solution.Copy(solution);

    public IRoarNetNeighborhood<TG> destruction_neighbourhood(IRoarNetProblem<TG> problem) => throw new NotImplementedException();

    public IRoarNetSolution<TG> empty_solution(IRoarNetProblem<TG> problem) => throw new NotImplementedException();

    public IRoarNetSolution<TG>? heuristic_solution(IRoarNetProblem<TG> problem) => throw new NotImplementedException();

    public IRoarNetNeighborhood<TG> local_neighbourhood(IRoarNetProblem<TG> problem) => throw new NotImplementedException();

    public double? lower_bound(IRoarNetSolution<TG> solution) => throw new NotImplementedException();

    public double? lower_bound_increment(IRoarNetMove<TG> move, IRoarNetSolution<TG> solution) => throw new NotImplementedException();

    public IEnumerable<IRoarNetMove<TG>> moves(IRoarNetNeighborhood<TG> neighbourhood, IRoarNetSolution<TG> solution) => throw new NotImplementedException();

    public IEnumerable<TMove> moves<TMove>(IRoarNetNeighborhood<TG> neighbourhood, IRoarNetSolution<TG> solution) => throw new NotImplementedException();

    public double? objective_value(IRoarNetSolution<TG> solution) => throw new NotImplementedException();

    public double? objective_value_increment(IRoarNetMove<TG> move, IRoarNetSolution<TG> solution) => throw new NotImplementedException();

    public IRoarNetMove<TG>? random_move(IRoarNetNeighborhood<TG> neighbourhood, IRoarNetSolution<TG> solution) => throw new NotImplementedException();

    public IEnumerable<IRoarNetMove<TG>> random_moves_without_replacement(IRoarNetNeighborhood<TG> neighbourhood, IRoarNetSolution<TG> solution) => throw new NotImplementedException();

    public IRoarNetSolution<TG> random_solution(IRoarNetProblem<TG> problem) => throw new NotImplementedException();

    public IRoarNetSolution<TG> revert_move(IRoarNetMove<TG> move, IRoarNetSolution<TG> solution) => throw new NotImplementedException();
}
