using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Neighborhoods;

public record MutationNeighborhood<TG, TS, TP>(StatelessMutator<TG, TS, TP> mutator) : Neighborhood<TG, TS, TP, int>
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
{
    public override TG Apply(TG genotype, int move, IRandomNumberGenerator random, TS searchSpace, TP problem)
        => mutator.Mutate([genotype], random.Fork(move), searchSpace, problem)[0];

#pragma warning disable S2190
    public override IEnumerable<int> Moves(TG genotype, IRandomNumberGenerator random, TS searchSpace, TP problem)
#pragma warning restore S2190
    {
        while (true)
        {
            yield return random.NextInt();
        }
    }
}
