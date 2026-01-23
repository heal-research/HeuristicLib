namespace HEAL.HeuristicLib.Collections;

public static class ImmutableListBuilder
{
  public static ImmutableList<T> Create<T>(ReadOnlySpan<T> items) where T : IEquatable<T> => new(items);
}
