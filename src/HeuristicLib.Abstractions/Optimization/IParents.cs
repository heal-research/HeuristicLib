namespace HEAL.HeuristicLib.Operators.Crossover;

public interface IParents<out T> {
  T Parent1 { get; }
  T Parent2 { get; }

  T Item1 => Parent1;
  T Item2 => Parent2;
}
