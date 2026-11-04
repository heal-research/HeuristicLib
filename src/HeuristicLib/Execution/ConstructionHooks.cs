using System.Collections.Concurrent;
using System.Reflection;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Execution;

// A resolution scope keeps one executor per configuration, whichever operation requests it first, so a configuration
// performing several operations must build that executor in one hook. Implementing the hook separately for each
// operation would make the executor depend on which operation was requested first.
internal static class ConstructionHooks
{
    private static readonly ConcurrentDictionary<Type, string?> Rejections = new();

    // Rejects a configuration type that implements CreateExecutor separately for several operations. The interface
    // mapping decides it per type, before anything is built, so the answer does not depend on the requested operation.
    public static void RequireOne(IOperator source)
    {
        if (Rejections.GetOrAdd(source.GetType(), Inspect) is { } reason)
            throw new InvalidOperationException(reason);
    }

    private static string? Inspect(Type type)
    {
        var operations = new List<Type>();
        // A method is identified with its constructed declaring type, so default implementations in two constructions of
        // one generic interface count as two hooks.
        var hooks = new HashSet<(Type?, Module, int)>();
        foreach (var contract in type.GetInterfaces())
        {
            if (!contract.IsGenericType || contract.GetGenericTypeDefinition() != typeof(IOperator<,>))
                continue;
            var mapping = type.GetInterfaceMap(contract);
            var hook = Array.FindIndex(mapping.InterfaceMethods, method => method.Name == nameof(IOperator<,>.CreateExecutor));
            var target = mapping.TargetMethods[hook];
            operations.Add(contract.GetGenericArguments()[0]);
            hooks.Add((target.DeclaringType, target.Module, target.MetadataToken));
        }
        if (hooks.Count <= 1)
            return null;
        return $"{ExecutionSignature.Name(type)} implements CreateExecutor separately for the operations " +
            $"{string.Join(", ", operations.Select(ExecutionSignature.Name))}. A configuration builds one executor for all its operations, so " +
            "implement CreateExecutor once as a public method, or configure operations that need separate executors as separate configurations.";
    }
}
