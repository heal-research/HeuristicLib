//using HEAL.HeuristicLib.Execution;
//using HEAL.HeuristicLib.Genotypes.Vectors;
//using HEAL.HeuristicLib.Operators;
//using HEAL.HeuristicLib.Optimization;
//using HEAL.HeuristicLib.Random;
//using HEAL.HeuristicLib.SearchSpaces.Vectors;

//namespace HEAL.HeuristicLib.Problems.Partial.TSP;

//public sealed record Swap2Neighborhood
//    : StatelessReversibleNeighborhood<Permutation, PermutationSearchSpace, TravelingSalesmanMoveProblem, Swap2Neighborhood.Move>,
//      IIncrementalObjectiveNeighborhood<Permutation, PermutationSearchSpace, TravelingSalesmanMoveProblem, Swap2Neighborhood.Move>,
//      IIncrementalObjectiveNeighborhoodInstance<Permutation, PermutationSearchSpace, TravelingSalesmanMoveProblem, Swap2Neighborhood.Move>
//{
//    public readonly record struct Move(int IndexA, int IndexB);

//    public override IEnumerable<Move> Moves(
//        Permutation genotype,
//        IRandomNumberGenerator random,
//        PermutationSearchSpace searchSpace,
//        TravelingSalesmanMoveProblem problem)
//    {
//        for (var i = 0; i < genotype.Count - 1; i++)
//        {
//            for (var j = i + 1; j < genotype.Count; j++)
//                yield return new Move(i, j);
//        }
//    }

//    public override bool RandomMove(
//        Permutation genotype,
//        IRandomNumberGenerator random,
//        PermutationSearchSpace searchSpace,
//        TravelingSalesmanMoveProblem problem, out Move move)
//    {
//        if (genotype.Count < 2)
//        {
//            move = default;
//            return false;
//        }

//        var i = random.NextInt(genotype.Count);
//        var j = random.NextInt(genotype.Count - 1);

//        if (j >= i)
//            j++;

//        move = i < j ? new Move(i, j) : new Move(j, i);
//        return true;
//    }

//    public override Permutation ApplyMove(
//        Permutation genotype,
//        Move move,
//        PermutationSearchSpace searchSpace,
//        TravelingSalesmanMoveProblem problem)
//    {
//        var values = genotype.ToArray();
//        (values[move.IndexA], values[move.IndexB]) = (values[move.IndexB], values[move.IndexA]);
//        return new Permutation(values);
//    }

//    public override Permutation RevertMove(
//        Permutation genotype,
//        Move move,
//        PermutationSearchSpace searchSpace,
//        TravelingSalesmanMoveProblem problem)
//        => ApplyMove(genotype, move, searchSpace, problem);

//    ObjectiveVector IIncrementalObjectiveNeighborhoodInstance<Permutation, PermutationSearchSpace, TravelingSalesmanMoveProblem, Move>.EvaluateIncrement(
//        Permutation genotype,
//        Move move,
//        IRandomNumberGenerator random,
//        PermutationSearchSpace searchSpace,
//        TravelingSalesmanMoveProblem problem)
//    {
//        var before = problem.TourLength(genotype);
//        var after = problem.TourLength(ApplyMove(genotype, move, searchSpace, problem));
//        return after - before;
//    }

//    IIncrementalObjectiveNeighborhoodInstance<Permutation, PermutationSearchSpace, TravelingSalesmanMoveProblem, Move> IIncrementalObjectiveNeighborhood<Permutation, PermutationSearchSpace, TravelingSalesmanMoveProblem, Move>.CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;
//}


