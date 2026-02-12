using System;
using System.Collections.Generic;
using System.Text;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Random.KeyCombiners;
using HEAL.HeuristicLib.Random.RandomEngines;

namespace HEAL.HeuristicLib.Tests;

public class TestRandoms
{
  public static RandomProfile SystemRandom { get; } = new(new SimpleKeyCombiner(), seed => new SystemRandomEngine(seed));

  public static RandomProfile NoRandomProfile { get; } = new(new SimpleKeyCombiner(), seed => new NoRandomEngine()); // forking must be possible

  public static readonly IRandomNumberGenerator NoRandom = RandomNumberGenerator.Create(0, NoRandomProfile);

  public static IRandomNumberGenerator SystemRandomGenerator(ulong seed) => RandomNumberGenerator.Create(seed, SystemRandom);
}
