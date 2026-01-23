namespace HEAL.HeuristicLib.Random;

public interface IRandomNumberGenerator {
  
    double NextDouble();
    
    int NextInt();
    
    //byte NextByte();
    //void NextBytes(Span<byte> buffer);
    
    IRandomNumberGenerator Fork(ulong forkKey);
}

public static class RandomNumberGeneratorExtensions 
{
  extension(IRandomNumberGenerator rng) 
  {
    public IRandomNumberGenerator Fork(int forkKey) => rng.Fork((ulong)forkKey);
    
    public IRandomNumberGenerator Fork(long forkKey) => rng.Fork((ulong)forkKey);
  }
}
