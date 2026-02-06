using System.Collections.Immutable;

namespace HEAL.HeuristicLib.Algorithms.MultiStreamAlgorithms;

public class Grid<T>
{
  public T Prototype { get; }

  public ImmutableList<IGridParameter<T>> Parameters { get; } = [];

  public Grid(T prototype)
  {
    Prototype = prototype;
  }
  
  public Grid(T prototype, ImmutableList<IGridParameter<T>> parameters)
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
