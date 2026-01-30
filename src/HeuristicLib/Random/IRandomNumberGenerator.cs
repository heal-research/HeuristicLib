using HEAL.HeuristicLib.Encodings.RealVector;

namespace HEAL.HeuristicLib.Random;

public interface IRandomNumberGenerator {
  int Integer() => Integer(0, int.MaxValue); //exclusive high is a concession to System.Random that can not generate int.MaxValue
  int Integer(int low, int high, bool inclusiveHigh = false);
  double Random();
  double Double() => Random();
  double Double(double min, double max) => NextDouble(min, max);
  double NextDouble() => Random();

  double NextDouble(double min, double max) {
    ArgumentOutOfRangeException.ThrowIfGreaterThan(min, max);
    return Random() * (max - min) + min;
  }

  bool Boolean(double probability = 0.5) => Random() < probability;

  byte[] RandomBytes(int length);

  public double[] Random(int length) {
    var values = new double[length];
    for (var i = 0; i < length; i++) {
      values[i] = Random();
    }

    return values;
  }

  public int[] Integers(int length, int high, bool inclusiveHigh = false) => Integers(length, 0, high, inclusiveHigh);

  public int[] Integers(int length, int low, int high, bool inclusiveHigh = false) {
    var values = new int[length];
    for (var i = 0; i < length; i++)
      values[i] = Integer(low, high, inclusiveHigh);
    return values;
  }

  public double NextGaussian(double mu = 0, double sigma = 1) {
    // we don't use spare numbers (efficiency loss but easier for multithreaded code)
    double u;
    double s;
    do {
      u = Random() * 2 - 1;
      var v = Random() * 2 - 1;
      s = u * u + v * v;
    } while (s is > 1 or 0);

    s = Math.Sqrt(-2.0 * Math.Log(s) / s);
    return mu + sigma * u * s;
  }

  public RealVector NextSphere(double[] mu, double[] sigma, int dim, bool surface = true) {
    var d = new RealVector(Enumerable.Range(0, dim).Select(_ => NextDouble()));
    if (surface)
      d /= d.Norm();
    d *= sigma;
    d += mu;
    return d;
  }

  int Integer(int high, bool inclusiveHigh = false) => Integer(0, high, inclusiveHigh);

  IReadOnlyList<IRandomNumberGenerator> Spawn(int count);

  IRandomNumberGenerator Spawn() => Spawn(1)[0];
}
