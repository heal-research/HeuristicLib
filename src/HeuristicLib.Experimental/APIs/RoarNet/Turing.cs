namespace HEAL.HeuristicLib.APIs.RoarNet;

public record TuringMove(int State, int Symbol, bool Right) : Move;

public record TuringSolution(int State, int Head, List<int> Tape, int Steps) : Solution
{
    public TuringSolution Apply(TuringMove move)
    {
        if (Head == Tape.Count)
            Tape.Add(move.Symbol);
        else
            Tape[Head] = move.Symbol;

        return new TuringSolution(move.State, Head + (move.Right ? 1 : -1), Tape, Steps + 1);
    }
}

public record TuringProblem(TuringMove[,] Transitions) : Operations, Problem, Neighbourhood
{
    public Solution apply_move(Move move, Solution solution) => ((TuringSolution)solution).Apply((TuringMove)move);
    public Neighbourhood construction_neighbourhood(Problem problem) => this;
    public Solution copy_solution(Solution solution) => (TuringSolution)solution with { Tape = ((TuringSolution)solution).Tape.ToList() };
    public Neighbourhood destruction_neighbourhood(Problem problem) => this;
    public Solution empty_solution(Problem problem) => new TuringSolution(0, 0, [], 0);
    public Solution heuristic_solution(Problem problem) => new TuringSolution(0, 0, [], 0);
    public Neighbourhood local_neighbourhood(Problem problem) => this;
    public double? lower_bound(Solution solution) => int.MinValue;
    public double? lower_bound_increment(Move move, Solution solution) => 0;

    public IEnumerable<Move> moves(Neighbourhood neighbourhood, Solution solution)
    {
        var m = random_move((TuringProblem)neighbourhood, solution);
        if (m != null)
            yield return m;
    }

    public double? objective_value(Solution solution) => -((TuringSolution)solution).Steps;
    public double? objective_value_increment(Move move, Solution solution) => -1;

    public Move? random_move(Neighbourhood neighbourhood, Solution solution)
    {
        TuringSolution tempQualifier = (TuringSolution)solution;
        var read = tempQualifier.Head == tempQualifier.Tape.Count ? 0 : tempQualifier.Tape[tempQualifier.Head];
        return tempQualifier.State < 0 ? null : Transitions[tempQualifier.State, read];
    }

    public IEnumerable<Move> random_moves_without_replacement(Neighbourhood neighbourhood, Solution solution) => moves(neighbourhood, solution);
    public Solution random_solution(Problem problem) => new TuringSolution(0, 0, [], 0);
    public Solution revert_move(Move move, Solution solution) => throw new NotSupportedException();
}
