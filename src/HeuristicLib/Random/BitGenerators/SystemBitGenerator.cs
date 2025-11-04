namespace HEAL.HeuristicLib.Random.BitGenerators;

public class SystemBitGenerator : IBitGenerator {

  private readonly SeedSequence seedSequence;
  private readonly System.Random random;
  public SystemBitGenerator(SeedSequence seedSequence) {
    this.seedSequence = seedSequence;
    random = new System.Random(seedSequence.GenerateSeed());
  }
  public SystemBitGenerator(int seed)
    : this(new SeedSequence(seed)) {
  }
  
  public byte[] RandomBytes(int length) {
    byte[] data = new byte[length];
    random.NextBytes(data);
    return data;
  }

  public int RandomInteger(int minValue, int maxValue) {
    return random.Next(minValue, maxValue);
  }

  public double RandomDouble() {
    return random.NextDouble();
  }

  public IReadOnlyList<SystemBitGenerator> Spawn(int count) => seedSequence.Spawn(count).Select(childSequence => new SystemBitGenerator(childSequence)).ToArray();
  IReadOnlyList<IBitGenerator> IBitGenerator.Spawn(int count) => Spawn(count);

}
