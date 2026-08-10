using System.Collections.Concurrent;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Execution;

public static class BatchExecution
{
    public static IReadOnlyList<TOut> Execute<TOut>(int count, Func<IRandomNumberGenerator, TOut> func, IRandomNumberGenerator random, ExecutionConcurrency concurrency)
        => concurrency.Kind == ExecutionConcurrencyKind.Sequential
            ? ExecuteSequentially(count, func, random)
            : ExecuteConcurrently(count, func, random, concurrency.MaximumConcurrency);

    public static IReadOnlyList<TOut> Execute<TIn, TOut>(IReadOnlyList<TIn> list, Func<TIn, IRandomNumberGenerator, TOut> func, IRandomNumberGenerator random, ExecutionConcurrency concurrency)
        => concurrency.Kind == ExecutionConcurrencyKind.Sequential
            ? ExecuteSequentially(list, func, random)
            : ExecuteConcurrently(list, func, random, concurrency.MaximumConcurrency);

    public static IReadOnlyList<TOut> Sequential<TOut>(int count, Func<IRandomNumberGenerator, TOut> func, IRandomNumberGenerator random)
        => Execute(count, func, random, ExecutionConcurrency.Sequential());

    public static IReadOnlyList<TOut> Sequential<TIn, TOut>(IReadOnlyList<TIn> list, Func<TIn, IRandomNumberGenerator, TOut> func, IRandomNumberGenerator random)
        => Execute(list, func, random, ExecutionConcurrency.Sequential());

    public static IReadOnlyList<TOut> Execute<TIn, TState, TOut>(IReadOnlyList<TIn> list, TState state, Func<TIn, IRandomNumberGenerator, TState, TOut> func, IRandomNumberGenerator random, ExecutionConcurrency concurrency)
        => concurrency.Kind == ExecutionConcurrencyKind.Sequential
            ? ExecuteSequentially(list, state, func, random)
            : ExecuteConcurrently(list, state, func, random, concurrency.MaximumConcurrency);

    public static IReadOnlyList<TOut> Sequential<TIn, TState, TOut>(IReadOnlyList<TIn> list, TState state, Func<TIn, IRandomNumberGenerator, TState, TOut> func, IRandomNumberGenerator random)
        => ExecuteSequentially(list, state, func, random);

    public static IReadOnlyList<TOut> Parallel<TOut>(int count, Func<IRandomNumberGenerator, TOut> func, IRandomNumberGenerator random)
        => Execute(count, func, random, ExecutionConcurrency.Concurrent());

    public static IReadOnlyList<TOut> Parallel<TOut>(int count, Func<IRandomNumberGenerator, TOut> func, IRandomNumberGenerator random, int maximumConcurrency)
        => Execute(count, func, random, ExecutionConcurrency.Concurrent(maximumConcurrency));

    public static IReadOnlyList<TOut> Parallel<TIn, TOut>(IReadOnlyList<TIn> list, Func<TIn, IRandomNumberGenerator, TOut> func, IRandomNumberGenerator random)
        => Execute(list, func, random, ExecutionConcurrency.Concurrent());

    public static IReadOnlyList<TOut> Parallel<TIn, TOut>(IReadOnlyList<TIn> list, Func<TIn, IRandomNumberGenerator, TOut> func, IRandomNumberGenerator random, int maximumConcurrency)
        => Execute(list, func, random, ExecutionConcurrency.Concurrent(maximumConcurrency));

    public static IReadOnlyList<TOut> Parallel<TIn, TState, TOut>(IReadOnlyList<TIn> list, TState state, Func<TIn, IRandomNumberGenerator, TState, TOut> func, IRandomNumberGenerator random)
        => Execute(list, state, func, random, ExecutionConcurrency.Concurrent());

    public static IReadOnlyList<TOut> Parallel<TIn, TState, TOut>(IReadOnlyList<TIn> list, TState state, Func<TIn, IRandomNumberGenerator, TState, TOut> func, IRandomNumberGenerator random, int maximumConcurrency)
        => Execute(list, state, func, random, ExecutionConcurrency.Concurrent(maximumConcurrency));

    private static IReadOnlyList<TOut> ExecuteSequentially<TOut>(int count, Func<IRandomNumberGenerator, TOut> func, IRandomNumberGenerator random)
    {
        var result = new TOut[count];
        for (int i = 0; i < count; i++)
        {
            var rng = random.Fork(i);
            result[i] = func(rng);
        }

        return result;
    }

    private static IReadOnlyList<TOut> ExecuteSequentially<TIn, TOut>(IReadOnlyList<TIn> list, Func<TIn, IRandomNumberGenerator, TOut> func, IRandomNumberGenerator random)
    {
        var result = new TOut[list.Count];
        for (int i = 0; i < list.Count; i++)
        {
            var rng = random.Fork(i);
            result[i] = func(list[i], rng);
        }

        return result;
    }

    private static IReadOnlyList<TOut> ExecuteSequentially<TIn, TState, TOut>(IReadOnlyList<TIn> list, TState state, Func<TIn, IRandomNumberGenerator, TState, TOut> func, IRandomNumberGenerator random)
    {
        var result = new TOut[list.Count];
        for (int i = 0; i < list.Count; i++)
        {
            var rng = random.Fork(i);
            result[i] = func(list[i], rng, state);
        }

        return result;
    }

    private static IReadOnlyList<TOut> ExecuteConcurrently<TOut>(int count, Func<IRandomNumberGenerator, TOut> func, IRandomNumberGenerator random, int maximumConcurrency)
    {
        if (count == 0)
            return [];

        var partitions = Partitioner.Create(0, count);
        var options = new ParallelOptions { MaxDegreeOfParallelism = ToMaximumDegreeOfParallelism(maximumConcurrency) };
        var result = new TOut[count];
        System.Threading.Tasks.Parallel.ForEach(partitions, options, range =>
        {
            var (start, end) = range;
            for (int i = start; i < end; i++)
            {
                var rng = random.Fork(i);
                result[i] = func(rng);
            }
        });

        return result;
    }

    private static IReadOnlyList<TOut> ExecuteConcurrently<TIn, TOut>(IReadOnlyList<TIn> list, Func<TIn, IRandomNumberGenerator, TOut> func, IRandomNumberGenerator random, int maximumConcurrency)
    {
        if (list.Count == 0)
            return [];

        var partitions = Partitioner.Create(0, list.Count);
        var options = new ParallelOptions { MaxDegreeOfParallelism = ToMaximumDegreeOfParallelism(maximumConcurrency) };
        var result = new TOut[list.Count];
        System.Threading.Tasks.Parallel.ForEach(partitions, options, range =>
        {
            var (start, end) = range;
            for (int i = start; i < end; i++)
            {
                var rng = random.Fork(i);
                result[i] = func(list[i], rng);
            }
        });

        return result;
    }

    private static IReadOnlyList<TOut> ExecuteConcurrently<TIn, TState, TOut>(IReadOnlyList<TIn> list, TState state, Func<TIn, IRandomNumberGenerator, TState, TOut> func, IRandomNumberGenerator random, int maximumConcurrency)
    {
        if (list.Count == 0)
            return [];

        var partitions = Partitioner.Create(0, list.Count);
        var options = new ParallelOptions { MaxDegreeOfParallelism = ToMaximumDegreeOfParallelism(maximumConcurrency) };
        var result = new TOut[list.Count];
        System.Threading.Tasks.Parallel.ForEach(partitions, options, range =>
        {
            var (start, end) = range;
            for (int i = start; i < end; i++)
            {
                var rng = random.Fork(i);
                result[i] = func(list[i], rng, state);
            }
        });

        return result;
    }

    private static int ToMaximumDegreeOfParallelism(int maximumConcurrency) => maximumConcurrency == int.MaxValue ? -1 : maximumConcurrency;
}
