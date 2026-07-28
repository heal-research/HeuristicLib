namespace HEAL.HeuristicLib.Optimization;

public interface IParents<out T>
{
    T Parent1 { get; }
    T Parent2 { get; }

    T Item1 => Parent1;
    T Item2 => Parent2;
}

public readonly record struct Parents<T>(T Parent1, T Parent2) : IParents<T>;

public static class Parents
{
    public static Parents<T> From<T>(T parent1, T parent2) => new(parent1, parent2);
}

public static class ParentsExtensions
{
    extension<T>((T Parent1, T Parent2) parents)
    {
        public Parents<T> ToParents() => Parents.From(parents.Parent1, parents.Parent2);
    }
}
