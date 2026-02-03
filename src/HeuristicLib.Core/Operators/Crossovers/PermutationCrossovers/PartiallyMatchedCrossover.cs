using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Crossovers.PermutationCrossovers;

public class PartiallyMatchedCrossover : BatchCrossover<Permutation, PermutationSearchSpace>
{
  public override IReadOnlyList<Permutation> Cross(IReadOnlyList<IParents<Permutation>> parents, IRandomNumberGenerator random, PermutationSearchSpace searchSpace) => Cross(parents, random);

  public static IReadOnlyList<Permutation> Cross(IReadOnlyList<IParents<Permutation>> parents, IRandomNumberGenerator rng, Memory<int>? memory = null)
  {
    Span<int> lengths = stackalloc int[parents.Count];
    for (var i = 0; i < lengths.Length; i++) {
      lengths[i] = Math.Max(parents[i].Parent1.Count, parents[i].Parent2.Count);
    }

    // ToDo: use random spawn for parallelizing breakpoint generation for each individual?
    var breakpoints = new (int, int)[lengths.Length];
    GetRandomBreakPoints(lengths, breakpoints, rng);

    return Cross(parents, breakpoints, memory);
  }

  public static IReadOnlyList<Permutation> Cross(IReadOnlyList<IParents<Permutation>> parents, IReadOnlyList<(int, int)> breakpoints, Memory<int>? memory = null)
  {
    if (parents.Count != breakpoints.Count) {
      throw new ArgumentException("Number of parents must match the number of breakpoints.");
    }

    Span<int> lengths = stackalloc int[parents.Count];
    var totalLength = 0;
    for (var i = 0; i < lengths.Length; i++) {
      var length = Math.Max(parents[i].Item1.Count, parents[i].Item2.Count);
      lengths[i] = length;
      totalLength += length;
      var (start, end) = breakpoints[i];
      if (start < 0 || end < 0 || start >= length || end >= length || start > end) {
        throw new ArgumentException("Start and end indices must be within the bounds of the permutation.");
      }
    }

    if (memory.HasValue && memory.Value.Length < totalLength) {
      throw new ArgumentException("Provided memory length is less than total length of parent permutations.");
    }

    var allOffspringMemory = memory ?? new int[totalLength];
    var permutations = new Permutation[parents.Count];

    var currentOffset = 0;
    for (var i = 0; i < parents.Count; i++) {
      var offspring = allOffspringMemory.Slice(currentOffset, lengths[i]);
      currentOffset += lengths[i];

      var p = parents[i];
      var parent1 = p.Parent1;
      var parent2 = p.Parent2;
      var (start, end) = breakpoints[i];

      Cross(parent1.Span, parent2.Span, start, end, offspring.Span);

      permutations[i] = Permutation.FromMemory(offspring);
    }

    return permutations;
  }

  private static void Cross(ReadOnlySpan<int> parent1, ReadOnlySpan<int> parent2, int start, int end, Span<int> offspring)
  {
    Span<bool> contains = stackalloc bool[offspring.Length];
    contains.Clear();

    Span<int> mappings = stackalloc int[offspring.Length];
    mappings.Clear();

    // 1. copy segment from parent1
    for (var i = start; i <= end; i++) {
      var value = parent1[i];
      offspring[i] = value;
      contains[value] = true;
      mappings[value] = parent2[i];
    }

    // 2. copy left values from parent1
    for (var i = 0; i < start; i++) {
      // follow mapping
      var value = parent2[i];
      while (contains[value]) {
        value = mappings[value];
      }

      offspring[i] = value;
      contains[value] = true;
    }

    for (var i = end; i < parent1.Length; i++) {
      // follow mapping
      var value = parent2[i];
      while (contains[value]) {
        value = mappings[value];
      }

      offspring[i] = value;
      contains[value] = true;
    }
  }

  private static void GetRandomBreakPoints(ReadOnlySpan<int> lengths, Span<(int, int)> breakPoints, IRandomNumberGenerator rng)
  {
    if (lengths.Length != breakPoints.Length) {
      throw new ArgumentException("Length of lengths and breakpoints must match.");
    }

    // todo: batch create randoms
    for (var i = 0; i < lengths.Length; i++) {
      if (lengths[i] < 2) {
        throw new ArgumentException("Length must be at least 2 to have break points.");
      }
      var start = rng.Integer(0, lengths[i] - 1);
      var end = rng.Integer(start + 1, lengths[i]);
      breakPoints[i] = (start, end);
    }
  }
}
