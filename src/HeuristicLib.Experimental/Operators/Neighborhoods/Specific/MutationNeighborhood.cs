namespace HEAL.HeuristicLib.Operators.Neighborhoods;

using Mutators;
using Problems;
using Random;
using SearchSpaces;

public record MutationNeighborhood<TG, TS, TP>(StatelessMutator<TG, TS, TP> mutator) : Neighborhood<TG, TS, TP, int>
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
{
    public override TG Apply(TG genotype, int move, IRandomNumberGenerator random, TS searchSpace, TP problem)
        => mutator.Mutate([genotype], random.Fork(move), searchSpace, problem)[0];

    public override IEnumerable<int> Moves(TG genotype, IRandomNumberGenerator random, TS searchSpace, TP problem)
    {
        while (true)
        {
            yield return random.NextInt();
        }
    }
}
