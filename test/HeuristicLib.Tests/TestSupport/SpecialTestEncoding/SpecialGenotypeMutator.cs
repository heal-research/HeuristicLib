using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.TestSupport.SpecialTestEncoding;

public record SpecialGenotypeMutator : SingleCandidateMutator<SpecialGenotype, SpecialSearchSpace, SpecialProblem>
{
    public override SpecialGenotype MutateCandidate(SpecialGenotype candidate, IRandomNumberGenerator random, SpecialSearchSpace searchSpace, SpecialProblem problem)
    {
        var strength = (int)Math.Round(problem.Data);
        var offset = random.NextInt(-strength, strength + 1);
        return new SpecialGenotype(candidate.Value + offset);
    }
}
