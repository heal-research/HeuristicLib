namespace HEAL.HeuristicLib.APIs.RoarNet;

public record TuringOperations(TuringOperations.Move[,] Transitions) : Operations, Problem, Neighbourhood
{
    public RoarNet.Solution apply_move(RoarNet.Move move, RoarNet.Solution solution) => ((Solution)solution).Apply((Move)move);
    public Neighbourhood construction_neighbourhood(Problem problem) => this;
    public RoarNet.Solution copy_solution(RoarNet.Solution solution) => (Solution)solution with { Tape = ((Solution)solution).Tape.ToList() };
    public Neighbourhood destruction_neighbourhood(Problem problem) => this;
    public RoarNet.Solution empty_solution(Problem problem) => new Solution(0, 0, [], 0);
    public RoarNet.Solution heuristic_solution(Problem problem) => new Solution(0, 0, [], 0);
    public Neighbourhood local_neighbourhood(Problem problem) => this;
    public double? lower_bound(RoarNet.Solution solution) => int.MinValue;
    public double? lower_bound_increment(RoarNet.Move move, RoarNet.Solution solution) => 0;

    public IEnumerable<RoarNet.Move> moves(Neighbourhood neighbourhood, RoarNet.Solution solution)
    {
        var m = random_move((TuringOperations)neighbourhood, solution);
        if (m != null)
            yield return m;
    }

    public double? objective_value(RoarNet.Solution solution) => -((Solution)solution).Steps;
    public double? objective_value_increment(RoarNet.Move move, RoarNet.Solution solution) => -1;

    public RoarNet.Move? random_move(Neighbourhood neighbourhood, RoarNet.Solution solution)
    {
        Solution tempQualifier = (Solution)solution;
        var read = tempQualifier.Head == tempQualifier.Tape.Count ? 0 : tempQualifier.Tape[tempQualifier.Head];
        return tempQualifier.State < 0 ? null : Transitions[tempQualifier.State, read];
    }

    public IEnumerable<RoarNet.Move> random_moves_without_replacement(Neighbourhood neighbourhood, RoarNet.Solution solution) => moves(neighbourhood, solution);
    public RoarNet.Solution random_solution(Problem problem) => new Solution(0, 0, [], 0);
    public RoarNet.Solution revert_move(RoarNet.Move move, RoarNet.Solution solution) => throw new NotSupportedException();

    public record Move(int State, int Symbol, bool Right) : RoarNet.Move;

    public record Solution(int State, int Head, List<int> Tape, int Steps) : RoarNet.Solution
    {
        public Solution Apply(Move move)
        {
            if (Head == Tape.Count)
                Tape.Add(move.Symbol);
            else
                Tape[Head] = move.Symbol;
            return new Solution(move.State, Head + (move.Right ? 1 : -1), Tape, Steps + 1);
        }
    }
}
