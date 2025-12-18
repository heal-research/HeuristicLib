namespace HEAL.HeuristicLib.Random;

public class SeedSequence {
  private readonly int rootSeed;
  //private readonly int[] spawnKey;
  private readonly int spawnKey;
  private int numberOfSpawnedChildren;

  //public SeedSequence(int rootSeed, params int[] spawnKey) {
  public SeedSequence(int rootSeed, int spawnKey = 0) {
    this.rootSeed = rootSeed;
    this.spawnKey = spawnKey;
    this.numberOfSpawnedChildren = 0;
  }

  public SeedSequence[] Spawn(int numberOfChildren) {
    var children = Enumerable
                   .Range(numberOfSpawnedChildren, numberOfChildren)
                   //.Select(index => new SeedSequence(rootSeed, Hash(spawnKey, index)))
                   .Select(index => new SeedSequence(rootSeed, HashCode.Combine(spawnKey, index)))
                   .ToArray();
    numberOfSpawnedChildren += numberOfChildren;
    return children;
  }

  [Obsolete("Not sure if this will clash with the Spawn method.")]
  public SeedSequence Fork(int forkKey) {
    return new SeedSequence(rootSeed, forkKey);
  }

  public int GenerateSeed() {
    return (int)Hash(rootSeed, spawnKey);
  }

  private const uint FnvPrime = 16777619;
  private const uint OffsetBasis = 2166136261;

  private static uint Hash(params IEnumerable<int> values) {
    var hash = OffsetBasis;

    foreach (var value in values) {
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
}
