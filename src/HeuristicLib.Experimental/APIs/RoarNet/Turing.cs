namespace HEAL.HeuristicLib.APIs.RoarNet;

internal record Turing(Turing.Move[,] Transitions) : BaseOperations<Turing.Solution, Turing.Move, Turing, Turing>, Problem, Neighbourhood
{
    public record Move(int State, int Symbol, bool Right) : RoarNet.Move;

    public record Solution(int State, int Head, List<int> Tape, int Steps) : RoarNet.Solution
    {
        public Solution Apply(Move move) => new(move.State, Head + (move.Right ? 1 : -1), Tape, Steps + 1);

        private void Write(Move move)
        {
            if (Head == Tape.Count) Tape.Add(move.Symbol);
            else Tape[Head] = move.Symbol;
        }

        public Move? NextMove(Move[,] transitions) => State < 0 ? null : transitions[State, Head == Tape.Count ? 0 : Tape[Head]];
    }

    #region Operations
    public override Solution apply_move(Move move, Solution solution) => solution.Apply(move);

    public override Turing construction_neighbourhood(Turing problem) => this;

    public override Solution copy_solution(Solution solution) => solution with { Tape = solution.Tape.ToList() };

    public override Turing destruction_neighbourhood(Turing problem) => this;

    public override Solution empty_solution(Turing problem) => new(0, 0, [], 0);

    public override Solution heuristic_solution(Turing problem) => new(0, 0, [], 0);

    public override Turing local_neighbourhood(Turing problem) => this;

    public override double? lower_bound(Solution solution) => int.MinValue;

    public override double? lower_bound_increment(Move move, Solution solution) => 0;

    public override IEnumerable<Move> moves(Turing neighbourhood, Solution solution)
    {
        var m = random_move(neighbourhood, solution);
        if (m != null) yield return m;
    }

    public override double? objective_value(Solution solution) => -solution.Steps;

    public override double? objective_value_increment(Move move, Solution solution) => -1;

    public override Move? random_move(Turing neighbourhood, Solution solution) => solution.NextMove(Transitions);

    public override IEnumerable<Move> random_moves_without_replacement(Turing neighbourhood, Solution solution) => moves(neighbourhood, solution);

    public override Solution random_solution(Turing problem) => new(0, 0, [], 0);

    public override Solution revert_move(Move move, Solution solution) => throw new NotSupportedException();
    #endregion
}
