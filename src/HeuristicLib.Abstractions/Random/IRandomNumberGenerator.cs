namespace HEAL.HeuristicLib.Random;

public interface IRandomNumberGenerator {
    int Integer(int high, bool inclusiveHigh = false) => Integer(0, high, inclusiveHigh);
    int Integer(int low, int high, bool inclusiveHigh = false);

    double Random();
    double NextDouble() => Random();

    bool Boolean(double probability = 0.5) => Random() < probability;

    byte[] RandomBytes(int length);

    IReadOnlyList<IRandomNumberGenerator> Spawn(int count);
    IRandomNumberGenerator Spawn() => Spawn(1)[0];
}
