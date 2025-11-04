using HEAL.HeuristicLib.Random.BitGenerators;

namespace HEAL.HeuristicLib.Random;

public class Random {
  private readonly IBitGenerator bitGenerator;
  
  public Random(IBitGenerator bitGenerator) {
    this.bitGenerator = bitGenerator;
  }
  public Random(SeedSequence seedSequence) 
    : this(new SystemBitGenerator(seedSequence)) {
  }
  public Random(int seed) 
    : this(new SeedSequence(seed)) {
  }

  public int RandomInteger(int high) => bitGenerator.RandomInteger(0, high);
  public int RandomInteger(int low, int high) => bitGenerator.RandomInteger(low, high);

  public double RandomDouble() => bitGenerator.RandomDouble();

  public byte[] RandomBytes(int length) => bitGenerator.RandomBytes(length);

  public Random[] Spawn(int count) => bitGenerator.Spawn(count).Select(child => new Random(child)).ToArray();
}
