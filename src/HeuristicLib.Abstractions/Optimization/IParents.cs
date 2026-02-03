namespace HEAL.HeuristicLib.Optimization;

public interface IParents<out T>
{
  T Parent1 { get; }
  T Parent2 { get; }

  T Item1 => Parent1;
  T Item2 => Parent2;
}

public readonly record struct Parents<T>(T Parent1, T Parent2) : IParents<T>;
