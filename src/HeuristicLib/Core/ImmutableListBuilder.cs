namespace HEAL.HeuristicLib.Core;

public static class ImmutableListBuilder {
  public static ImmutableList<T> Create<T>(ReadOnlySpan<T> items) where T : IEquatable<T> {
    return new ImmutableList<T>(items);
  }
}
