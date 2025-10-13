namespace HEAL.HeuristicLib.Random;

public class SeedSequence {
  public static int GetSeed(int baseSeed, int index) {
    return Math.Abs((int)Hash(baseSeed, index));
  }

  private const uint FnvPrime = 16777619;
  private const uint OffsetBasis = 2166136261;

  private static uint Hash(params int[] values) {
    uint hash = OffsetBasis;

    foreach (int value in values) {
      unchecked {
        // Break each int into 4 bytes (little endian) and feed each byte
        hash ^= (byte)(value & 0xFF);
        hash *= FnvPrime;

        hash ^= (byte)((value >> 8) & 0xFF);
        hash *= FnvPrime;

        hash ^= (byte)((value >> 16) & 0xFF);
        hash *= FnvPrime;

        hash ^= (byte)((value >> 24) & 0xFF);
        hash *= FnvPrime;
      }
    }

    return hash;
  }
  //
  // private readonly int baseSeed;
  // public SeedSequence(int baseSeed) {
  //   this.baseSeed = baseSeed;
  // }
  //
  // public SeedSequence[] Spawn(int children) {
  //   return Enumerable.Range(0, children).Select(
  //     index => new SeedSequence(GetSeed(baseSeed, index))).ToArray();
  // }
}

// public interface IRandomSource {
//   IRandomNumberGenerator CreateRandomNumberGenerator();
// }
//
// public class RandomSource : IRandomSource {
//   private readonly System.Random random;
//   public int Seed { get; }
//   
//   public RandomSource(int seed) {
//     Seed = seed;
//     random = new System.Random(seed);
//   }
//   public RandomSource() : this(GetRandomSeed()) { }
//   
//   public IRandomNumberGenerator CreateRandomNumberGenerator() {
//     return new SystemRandomNumberGenerator(random);
//   }
//
//   private static int GetRandomSeed() => new System.Random().Next();
// }
//
// public class FixedRandomSource : IRandomSource {
//   public int Seed { get; }
//   public FixedRandomSource(int seed) {
//     Seed = seed;
//   }
//
//   public IRandomNumberGenerator CreateRandomNumberGenerator() {
//     return new SystemRandomNumberGenerator(new System.Random(Seed));
//   }
// }

//TODO remove after migration

// ToDo: and others
