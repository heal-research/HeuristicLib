using HEAL.HeuristicLib.Encodings.BoolVectors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Usage;

/// <summary>
/// Flips one position. Needs nothing of its input and promises only that the result has the same length.
/// </summary>
internal sealed record FlipOneBitMutator : SingleCandidateMutator<BoolVector>
{
    public override BoolVector MutateCandidate(BoolVector parent, IRandomNumberGenerator random)
    {
        if (parent.Count == 0)
        {
            return parent;
        }

        var elements = parent.ToArray();
        var index = random.NextInt(elements.Length);
        elements[index] = !elements[index];
        return BoolVector.FromOwnedArray(elements);
    }
}

/// <summary>
/// Moves the second set element to a randomly chosen cleared position. Requires at least two set elements, and
/// promises that the result has the same length and the same number of set elements.
/// </summary>
/// <remarks>
/// This operator exists to show an input requirement rather than to be a good mutation. It is the mirror image of
/// <see cref="FlipOneBitMutator"/>: it always keeps a candidate inside a fixed cardinality space, and it cannot be
/// used over unconstrained bool vectors, where a candidate may have fewer than two set elements.
/// </remarks>
internal sealed record SwapSecondTrueMutator : SingleCandidateMutator<BoolVector>
{
    public override BoolVector MutateCandidate(BoolVector parent, IRandomNumberGenerator random)
    {
        var setPositions = Positions(parent, value: true);
        if (setPositions.Count < 2)
        {
            throw new ArgumentException("At least two set elements are required.", nameof(parent));
        }

        var clearedPositions = Positions(parent, value: false);
        if (clearedPositions.Count == 0)
        {
            return parent;
        }

        var elements = parent.ToArray();
        elements[setPositions[1]] = false;
        elements[clearedPositions[random.NextInt(clearedPositions.Count)]] = true;
        return BoolVector.FromOwnedArray(elements);
    }

    private static List<int> Positions(BoolVector candidate, bool value)
    {
        var positions = new List<int>();
        for (var i = 0; i < candidate.Count; i++)
        {
            if (candidate[i] == value)
            {
                positions.Add(i);
            }
        }

        return positions;
    }
}

/// <summary>
/// The target table for operator and search space compatibility. Three operators over two search spaces that share
/// one candidate representation, with the two checks that decide usability kept apart: does the operator accept
/// everything the space contains, and does everything it returns land back inside the space.
/// </summary>
/// <remarks>
/// These assertions are observed behavior, established exhaustively rather than by sampling: both spaces are small
/// enough to enumerate completely. They are therefore ground truth, and they are the acceptance criterion for a
/// declared invariant mechanism, which must reproduce this table without running any operator.
/// <para>
/// The two failures fail different checks. <see cref="FlipOneBitMutator"/> accepts everything and returns candidates
/// outside the constrained space; <see cref="SwapSecondTrueMutator"/> returns valid candidates and rejects inputs the
/// unconstrained space contains. Neither check would find the other's failure, which is why one test cannot replace
/// the pair.
/// </para>
/// </remarks>
public class SearchSpaceCompatibilitySpecs
{
    private const int Length = 4;
    private const int Cardinality = 2;

    private static BoolVectorSearchSpace Unconstrained => new(Length);

    private static FixedCardinalityBoolVectorSearchSpace Constrained => new(Length, Cardinality);

    /// <summary>
    /// Applying a mutator over a space needs the mutator invoked the way that space allows, which is itself part of
    /// the finding: <see cref="BitSwapMutator"/> names the constrained space in its type, so reaching it over
    /// unconstrained bool vectors goes through its cardinality parameterized entry point with the candidate's own
    /// count. That is what "preserves the input's cardinality" means, and it is stronger than the unconstrained
    /// space requires.
    /// </summary>
    private static Func<BoolVector, IRandomNumberGenerator, BoolVector> BitSwapOverAnyBoolVector() =>
        (candidate, random) => BitSwapMutator.Mutate(candidate, random, candidate.Count(element => element));

    private static Func<BoolVector, IRandomNumberGenerator, BoolVector> BitSwapOverFixedCardinality(
        FixedCardinalityBoolVectorSearchSpace space) =>
        (candidate, random) => new BitSwapMutator().MutateCandidate(candidate, random, space);

    private static Func<BoolVector, IRandomNumberGenerator, BoolVector> FlipOneBit() =>
        (candidate, random) => new FlipOneBitMutator().MutateCandidate(candidate, random);

    private static Func<BoolVector, IRandomNumberGenerator, BoolVector> SwapSecondTrue() =>
        (candidate, random) => new SwapSecondTrueMutator().MutateCandidate(candidate, random);

    [Fact]
    public void FlipOneBit_IsUsableOverUnconstrainedBoolVectorsOnly()
    {
        Check(FlipOneBit(), Unconstrained).ShouldBe((Accepts: true, StaysInside: true));
        Check(FlipOneBit(), Constrained).ShouldBe((Accepts: true, StaysInside: false));
    }

    [Fact]
    public void BitSwap_IsUsableOverBothSpaces()
    {
        Check(BitSwapOverAnyBoolVector(), Unconstrained).ShouldBe((Accepts: true, StaysInside: true));
        Check(BitSwapOverFixedCardinality(Constrained), Constrained).ShouldBe((Accepts: true, StaysInside: true));
    }

    [Fact]
    public void SwapSecondTrue_IsUsableOverTheFixedCardinalitySpaceOnly()
    {
        Check(SwapSecondTrue(), Unconstrained).ShouldBe((Accepts: false, StaysInside: true));
        Check(SwapSecondTrue(), Constrained).ShouldBe((Accepts: true, StaysInside: true));
    }

    /// <summary>
    /// The same six results as one table, in the form a declared invariant mechanism has to predict without running
    /// anything.
    /// </summary>
    [Fact]
    public void TheCompatibilityTable_IsTheTargetForAnyDeclaredInvariantMechanism()
    {
        var constrained = Constrained;

        var cells = new (string Operator, string Space, Func<BoolVector, IRandomNumberGenerator, BoolVector> Mutate,
            ISearchSpace<BoolVector> SearchSpace)[]
        {
            ("FlipOneBit", "Unconstrained", FlipOneBit(), Unconstrained),
            ("FlipOneBit", "Constrained", FlipOneBit(), constrained),
            ("BitSwap", "Unconstrained", BitSwapOverAnyBoolVector(), Unconstrained),
            ("BitSwap", "Constrained", BitSwapOverFixedCardinality(constrained), constrained),
            ("SwapSecondTrue", "Unconstrained", SwapSecondTrue(), Unconstrained),
            ("SwapSecondTrue", "Constrained", SwapSecondTrue(), constrained)
        };

        var table = cells
            .Select(cell =>
            {
                var (accepts, staysInside) = Check(cell.Mutate, cell.SearchSpace);
                return $"{cell.Operator} on {cell.Space}: accepts={accepts} staysInside={staysInside} " +
                       $"usable={accepts && staysInside}";
            })
            .ToArray();

        table.ShouldBe([
            "FlipOneBit on Unconstrained: accepts=True staysInside=True usable=True",
            "FlipOneBit on Constrained: accepts=True staysInside=False usable=False",
            "BitSwap on Unconstrained: accepts=True staysInside=True usable=True",
            "BitSwap on Constrained: accepts=True staysInside=True usable=True",
            "SwapSecondTrue on Unconstrained: accepts=False staysInside=True usable=False",
            "SwapSecondTrue on Constrained: accepts=True staysInside=True usable=True"
        ]);
    }

    /// <summary>
    /// Applies the mutator to every member of the space under several seeds, reporting the two checks separately.
    /// </summary>
    private static (bool Accepts, bool StaysInside) Check(
        Func<BoolVector, IRandomNumberGenerator, BoolVector> mutate,
        ISearchSpace<BoolVector> space)
    {
        var accepts = true;
        var staysInside = true;

        foreach (var candidate in AllBoolVectorsOfLength(Length).Where(space.Contains))
        {
            for (var seed = 0; seed < 8; seed++)
            {
                try
                {
                    staysInside &= space.Contains(mutate(candidate, RandomNumberGenerator.Create(seed)));
                }
                catch (ArgumentException)
                {
                    accepts = false;
                }
            }
        }

        return (accepts, staysInside);
    }

    private static IEnumerable<BoolVector> AllBoolVectorsOfLength(int length) =>
        Enumerable
            .Range(0, 1 << length)
            .Select(mask => BoolVector.Create(
                Enumerable.Range(0, length).Select(bit => (mask & (1 << bit)) != 0)));
}
