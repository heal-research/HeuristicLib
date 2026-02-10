using System.Collections;
using Generator.Equals;

namespace HEAL.HeuristicLib.Algorithms.MultiStreamAlgorithms;

[Equatable]
public partial class Grid<T> : IEnumerable<T>
{
  public T Prototype { get; }

  [OrderedEquality]
  public ImmutableArray<IGridParameter<T>> Parameters { get; } = [];

  public Grid(T prototype)
  {
    Prototype = prototype;
  }

  public Grid(T prototype, ImmutableArray<IGridParameter<T>> parameters)
  {
    Prototype = prototype;
    Parameters = parameters;
  }

  public Grid<T> VaryBy<TP>(IReadOnlyList<TP> values, Func<T, TP, T> configurator)
  {
    return new Grid<T>(Prototype, Parameters.Add(new GridParameter<T, TP>(values, configurator)));
  }

  public IReadOnlyList<T> GetConfigurations()
  {
    IEnumerable<T> configurations = new List<T> { Prototype };

    return Parameters.Aggregate(configurations, (current, parameter) => parameter.GetConfigurations(current)).ToList();
  }

  public IEnumerator<T> GetEnumerator() => GetConfigurations().GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public interface IGridParameter<T>
{
  public int Count { get; }
  IEnumerable<T> GetConfigurations(IEnumerable<T> prototypes);
}

public record GridParameter<T, TParam>(IReadOnlyList<TParam> Values, Func<T, TParam, T> Configurator) : IGridParameter<T>
{
  public int Count => Values.Count;

  public IEnumerable<T> GetConfigurations(IEnumerable<T> prototypes)
  {
    return prototypes.SelectMany(prototype => Values.Select(value => Configurator(prototype, value)));
  }
}

public static class Grid
{
  public static Grid<T> Create<T>(T prototype)
    // where T : IAlgorithm
  {
    return new Grid<T>(prototype);
  }

  extension<T>(T prototype)
  {
    public Grid<T> AsGrid() => Create(prototype);
  }
}
